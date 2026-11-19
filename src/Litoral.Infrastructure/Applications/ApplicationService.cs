using System.Data;
using Litoral.Application.Admin;
using Litoral.Application.Applications;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Applications;

public sealed class ApplicationService(
    LitoralDbContext db,
    UserManager<ApplicationUser> userManager,
    PilotRegistrar registrar,
    RegistrationGate gate,
    Notifier notifier,
    IAuditLog auditLog) : IApplicationService
{
    public async Task<RegistrationStatusDto> GetRegistrationStatusAsync(CancellationToken ct) =>
        new(await gate.IsOpenAsync(ct), PilotApplication.Simulators, PilotApplication.AddOnOptions);

    public async Task<AdminApplicationsDto> ListAsync(CancellationToken ct)
    {
        var applications = await db.PilotApplications.AsNoTracking().OrderBy(a => a.CreatedAt).ToListAsync(ct);

        var userIds = applications.Select(a => a.UserId).ToList();
        var emails = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(ct);

        var vatsimIds = applications.Where(a => a.VatsimId != null).Select(a => a.VatsimId).ToList();
        var ivaoIds = applications.Where(a => a.IvaoId != null).Select(a => a.IvaoId).ToList();
        var usedVatsim = await db.Pilots.Where(p => p.VatsimId != null && vatsimIds.Contains(p.VatsimId)).Select(p => p.VatsimId!).ToListAsync(ct);
        var usedIvao = await db.Pilots.Where(p => p.IvaoId != null && ivaoIds.Contains(p.IvaoId)).Select(p => p.IvaoId!).ToListAsync(ct);

        var dtos = applications.Select(a => new AdminApplicationDto(
            a.Id,
            a.FirstName,
            a.LastName,
            emails.FirstOrDefault(e => e.Id == a.UserId)?.Email ?? "",
            a.HubIcao,
            a.VatsimId,
            a.IvaoId,
            a.Simulator,
            a.AddOnList,
            a.Motivation,
            a.CreatedAt,
            a.VatsimId is not null && usedVatsim.Contains(a.VatsimId),
            a.IvaoId is not null && usedIvao.Contains(a.IvaoId))).ToList();

        return new AdminApplicationsDto(await gate.IsOpenAsync(ct), dtos);
    }

    public async Task ApproveAsync(Guid applicationId, CancellationToken ct)
    {
        var application = await FindAsync(applicationId, ct);
        var user = await FindUserAsync(application.UserId);

        if (await registrar.NetworkConflictAsync(application.VatsimId, application.IvaoId, ct) is string conflict)
            throw new AppException(AppErrorKind.Conflict, conflict);

        var hubIcao = await registrar.IsPassengerHubAsync(application.HubIcao, ct) ? application.HubIcao : PilotRegistrar.DefaultHubIcao;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var pilot = await registrar.CreateAsync(user, application.FirstName, application.LastName, hubIcao, application.AcceptedTermsVersion, ct);
        pilot.SetNetworkIds(application.VatsimId, application.IvaoId, null);
        db.PilotApplications.Remove(application);

        notifier.Send(pilot.Id, NotificationType.Application, "Welcome to Litoral!",
            $"Your application was approved. You are {pilot.DisplayId}, First Officer based at {hubIcao}. Add your SimBrief username in Settings and book your first flight.",
            "/dashboard");

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Pilots, "Application approved", "Pilot", pilot.Id.ToString(),
            AuditContext.PilotLabel(pilot), $"Email: {user.Email}\nBase: {hubIcao}"), ct);
    }

    public async Task RejectAsync(Guid applicationId, RejectApplicationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new AppException(AppErrorKind.Validation, "Write the reason for the rejection.");

        var application = await FindAsync(applicationId, ct);
        var user = await FindUserAsync(application.UserId);
        var email = user.Email ?? "";
        var name = $"{application.FirstName} {application.LastName}";

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        gate.Block(email);
        db.PilotApplications.Remove(application);
        await db.SaveChangesAsync(ct);

        var deleted = await userManager.DeleteAsync(user);
        if (!deleted.Succeeded)
            throw new AppException(AppErrorKind.Conflict, string.Join(" ", deleted.Errors.Select(e => e.Description)));

        await transaction.CommitAsync(ct);

        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Pilots, "Application rejected", "Application", application.Id.ToString(), name,
            $"Email: {email}\nReason: {request.Reason.Trim()}\nCan apply again after {RegistrationBlock.BlockDays} days"), ct);
    }

    public async Task SetRegistrationOpenAsync(bool open, CancellationToken ct)
    {
        if (await gate.IsOpenAsync(ct) == open) return;

        await gate.SetOpenAsync(open, ct);
        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Pilots, open ? "Applications opened" : "Applications closed"), ct);
    }

    private async Task<PilotApplication> FindAsync(Guid id, CancellationToken ct) =>
        await db.PilotApplications.FirstOrDefaultAsync(a => a.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Application not found.");

    private async Task<ApplicationUser> FindUserAsync(string userId) =>
        await userManager.FindByIdAsync(userId)
        ?? throw new AppException(AppErrorKind.NotFound, "User not found.");
}