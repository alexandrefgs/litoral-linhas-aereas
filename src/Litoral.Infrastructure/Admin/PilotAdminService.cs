using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Staff;
using Litoral.Infrastructure.Pilots;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Admin;

public sealed class PilotAdminService(
    LitoralDbContext db,
    StaffRoles staffRoles,
    PilotEraser eraser) : IPilotAdminService
{
    public async Task<IReadOnlyList<AdminPilotDto>> ListAsync(CancellationToken ct)
    {
        var pilots = await db.Pilots.AsNoTracking().Include(p => p.Rank).OrderBy(p => p.PilotNumber).ToListAsync(ct);
        return await ToDtosAsync(pilots, ct);
    }

    public async Task<AdminPilotDto> DeactivateAsync(string actorUserId, Guid pilotId, CancellationToken ct)
    {
        var pilot = await FindAsync(pilotId, ct);
        await EnsureCanManageAsync(actorUserId, pilot, ct);

        if (!pilot.IsActive)
            throw new AppException(AppErrorKind.Conflict, $"{pilot.DisplayId} is already inactive.");

        pilot.Deactivate();
        await db.SaveChangesAsync(ct);
        return (await ToDtosAsync([pilot], ct))[0];
    }

    public async Task<AdminPilotDto> ActivateAsync(string actorUserId, Guid pilotId, CancellationToken ct)
    {
        var pilot = await FindAsync(pilotId, ct);

        if (pilot.UserId == actorUserId)
            throw new AppException(AppErrorKind.Validation, "You can't change your own account.");
        if (pilot.IsActive)
            throw new AppException(AppErrorKind.Conflict, $"{pilot.DisplayId} is already active.");

        pilot.Activate();
        await db.SaveChangesAsync(ct);
        return (await ToDtosAsync([pilot], ct))[0];
    }

    public async Task DeleteAsync(string actorUserId, Guid pilotId, CancellationToken ct)
    {
        var pilot = await FindAsync(pilotId, ct);

        if (pilot.UserId == actorUserId)
            throw new AppException(AppErrorKind.Validation, "Delete your own account in your profile settings.");
        if (await eraser.BlockReasonAsync(pilot, false, ct) is string reason)
            throw new AppException(AppErrorKind.Conflict, reason);

        await eraser.DeleteAsync(pilot, false, ct);
    }

    private async Task<Pilot> FindAsync(Guid pilotId, CancellationToken ct) =>
        await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.Id == pilotId, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

    private async Task EnsureCanManageAsync(string actorUserId, Pilot pilot, CancellationToken ct)
    {
        if (pilot.UserId == actorUserId)
            throw new AppException(AppErrorKind.Validation, "You can't change your own account.");

        var roles = await staffRoles.OfUserAsync(pilot.UserId, ct);
        if (roles.Contains(StaffPositions.CeoFounder))
            throw new AppException(AppErrorKind.Validation, "The CEO & Founder account can't be changed.");
        if (roles.Count > 0)
            throw new AppException(AppErrorKind.Conflict, $"{pilot.DisplayId} is part of the staff. Remove the staff positions first.");
    }

    private async Task<List<AdminPilotDto>> ToDtosAsync(IReadOnlyList<Pilot> pilots, CancellationToken ct)
    {
        var ids = pilots.Select(p => p.Id).ToList();
        var userIds = pilots.Select(p => p.UserId).ToList();

        var users = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.TwoFactorEnabled })
            .ToDictionaryAsync(u => u.Id, ct);

        var flights = await db.Pireps.AsNoTracking()
            .Where(p => ids.Contains(p.PilotId))
            .GroupBy(p => p.PilotId)
            .Select(g => new { PilotId = g.Key, Count = g.Count(), Last = g.Max(p => p.SubmittedAt) })
            .ToDictionaryAsync(g => g.PilotId, ct);

        var blocked = await eraser.BlockedIdsAsync(ids, ct);
        var roles = await staffRoles.ByUserAsync(ct);

        return pilots.Select(p =>
        {
            flights.TryGetValue(p.Id, out var stats);
            var user = users.GetValueOrDefault(p.UserId);var positions = roles.GetValueOrDefault(p.UserId) ?? [];
            return new AdminPilotDto(
                p.Id,
                p.PilotNumber,
                p.DisplayId,
                $"{p.FirstName} {p.LastName}",
                user?.Email ?? "",
                p.Rank.Code,
                p.PhotoUrl,
                p.BaseIcao,
                p.CurrentLocationIcao,
                p.TotalFlightMinutes,
                p.Points,
                stats?.Count ?? 0,
                stats?.Last,
                p.CreatedAt,
                p.IsActive,
                positions,
                positions.Count == 0 && !blocked.Contains(p.Id),
                user?.TwoFactorEnabled ?? false);
        }).ToList();
    }
}