using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Staff;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Admin;

public sealed class StaffAdminService(
    LitoralDbContext db,
    UserManager<ApplicationUser> userManager,
    StaffRoles staffRoles,
    IAuditLog auditLog) : IStaffAdminService
{
    private static readonly string[] CeoOnlyPositions = [StaffPositions.Chairman, StaffPositions.ViceChairman];

    public async Task<StaffOverviewDto> GetAsync(string actorUserId, CancellationToken ct)
    {
        var actorRoles = await staffRoles.OfUserAsync(actorUserId, ct);
        var actorIsCeo = actorRoles.Contains(StaffPositions.CeoFounder);

        var pilots = await db.Pilots.AsNoTracking().Include(p => p.Rank).OrderBy(p => p.PilotNumber).ToListAsync(ct);
        var roles = await staffRoles.ByUserAsync(ct);

        var positions = StaffPositions.All.Select(position => new StaffPositionDto(
            position.Code,
            position.Name,
            position.Team.ToString(),
            position.SingleHolder,
            CanEdit(position.Code, actorIsCeo),
            pilots
                .Where(p => roles.GetValueOrDefault(p.UserId)?.Contains(position.Code) == true)
                .Select(Member)
                .ToList()))
            .ToList();

        var candidates = pilots
            .Where(p => p.IsActive && p.UserId != actorUserId)
            .Select(Member)
            .ToList();

        return new StaffOverviewDto(positions, candidates);
    }

    public async Task<StaffOverviewDto> AssignAsync(string actorUserId, string positionCode, Guid pilotId, CancellationToken ct)
    {
        var position = await EnsureEditableAsync(actorUserId, positionCode, ct);
        var pilot = await FindPilotAsync(actorUserId, pilotId, ct);

        if (!pilot.IsActive)
            throw new AppException(AppErrorKind.Validation, $"{pilot.DisplayId} is inactive and can't join the staff.");

        var user = await FindUserAsync(pilot);
        if (await userManager.IsInRoleAsync(user, position.Code))
            throw new AppException(AppErrorKind.Conflict, $"{pilot.DisplayId} is already {position.Name}.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var replaced = new List<string>();
        if (position.SingleHolder)
        {
            foreach (var holder in await userManager.GetUsersInRoleAsync(position.Code))
            {
                Check(await userManager.RemoveFromRoleAsync(holder, position.Code));
                if (await db.Pilots.AsNoTracking().Include(p => p.Rank).FirstOrDefaultAsync(p => p.UserId == holder.Id, ct) is { } previous)
                {
                    replaced.Add(AuditContext.PilotLabel(previous));
                    await auditLog.WriteAsync(StaffEntry("Staff position removed", previous, $"Position: {position.Name}\nReplaced by {AuditContext.PilotLabel(pilot)}"), ct);
                }
            }
        }

        Check(await userManager.AddToRoleAsync(user, position.Code));
        await auditLog.WriteAsync(StaffEntry("Staff position assigned", pilot,
            $"Position: {position.Name}" + (replaced.Count > 0 ? $"\nReplaced {string.Join(", ", replaced)}" : "")), ct);
        await transaction.CommitAsync(ct);

        return await GetAsync(actorUserId, ct);
    }

    public async Task<StaffOverviewDto> RemoveAsync(string actorUserId, string positionCode, Guid pilotId, CancellationToken ct)
    {
        var position = await EnsureEditableAsync(actorUserId, positionCode, ct);
        var pilot = await FindPilotAsync(actorUserId, pilotId, ct);
        var user = await FindUserAsync(pilot);

        if (!await userManager.IsInRoleAsync(user, position.Code))
            throw new AppException(AppErrorKind.Conflict, $"{pilot.DisplayId} is not {position.Name}.");

        Check(await userManager.RemoveFromRoleAsync(user, position.Code));
        await auditLog.WriteAsync(StaffEntry("Staff position removed", pilot, $"Position: {position.Name}"), ct);
        return await GetAsync(actorUserId, ct);
    }

    private static bool CanEdit(string code, bool actorIsCeo) =>
        code != StaffPositions.CeoFounder && (actorIsCeo || !CeoOnlyPositions.Contains(code));

    private async Task<StaffPositionDefinition> EnsureEditableAsync(string actorUserId, string positionCode, CancellationToken ct)
    {
        var position = StaffPositions.Find(positionCode.ToUpperInvariant())
            ?? throw new AppException(AppErrorKind.NotFound, "Staff position not found.");

        if (position.Code == StaffPositions.CeoFounder)
            throw new AppException(AppErrorKind.Validation, "The CEO & Founder position can't be changed.");

        var actorIsCeo = (await staffRoles.OfUserAsync(actorUserId, ct)).Contains(StaffPositions.CeoFounder);
        if (!CanEdit(position.Code, actorIsCeo))
            throw new AppException(AppErrorKind.Validation, $"Only the CEO & Founder can change the {position.Name} position.");

        return position;
    }

    private async Task<Pilot> FindPilotAsync(string actorUserId, Guid pilotId, CancellationToken ct)
    {
        var pilot = await db.Pilots.AsNoTracking().Include(p => p.Rank).FirstOrDefaultAsync(p => p.Id == pilotId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

        if (pilot.UserId == actorUserId)
            throw new AppException(AppErrorKind.Validation, "You can't change your own staff positions.");

        return pilot;
    }

    private async Task<ApplicationUser> FindUserAsync(Pilot pilot) =>
        await userManager.FindByIdAsync(pilot.UserId)
        ?? throw new AppException(AppErrorKind.NotFound, "User not found.");

    private static AuditEntry StaffEntry(string action, Pilot pilot, string details) =>
        new(AuditCategory.Staff, action, "Pilot", pilot.Id.ToString(), AuditContext.PilotLabel(pilot), details);

    private static StaffMemberDto Member(Pilot pilot) =>
        new(pilot.Id, pilot.PilotNumber, pilot.DisplayId, pilot.PublicName, pilot.PhotoUrl);

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new AppException(AppErrorKind.Conflict, string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}