using System.Data;
using Litoral.Application.Admin;
using Litoral.Application.Auth;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Legal;
using Litoral.Domain.Staff;
using Litoral.Infrastructure.Admin;
using Litoral.Infrastructure.Applications;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Pilots;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Litoral.Infrastructure.Auth;

public sealed class AuthService(
    LitoralDbContext db,
    UserManager<ApplicationUser> userManager,
    JwtTokenGenerator tokenGenerator,
    TwoFactorTokens twoFactorTokens,
    PilotRegistrar registrar,
    RegistrationGate gate,
    StaffRoles staffRoles,
    Notifier notifier,
    IAuditLog auditLog,
    IOptions<TwoFactorOptions> twoFactorOptions) : IAuthService
{
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (!request.AcceptedTerms)
            throw new AppException(AppErrorKind.Validation, "You must accept the Regulations and the Privacy Policy.");

        var email = request.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
            throw new AppException(AppErrorKind.Conflict, "This email is already registered.");

        var hubIcao = string.IsNullOrWhiteSpace(request.HubIcao)
            ? PilotRegistrar.DefaultHubIcao
            : request.HubIcao.Trim().ToUpperInvariant();

        if (!await registrar.IsPassengerHubAsync(hubIcao, ct))
            throw new AppException(AppErrorKind.Validation, $"{hubIcao} is not a Litoral passenger hub.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var isFirstPilot = !await db.Pilots.AnyAsync(ct);

        if (!isFirstPilot)
        {
            if (!await gate.IsOpenAsync(ct))
                throw new AppException(AppErrorKind.Conflict, "Litoral is not accepting new pilots right now.");
            if (await gate.BlockedUntilAsync(email, ct) is DateTime until)
                throw new AppException(AppErrorKind.Conflict, $"Your last application was not accepted. You can apply again on {until:d MMM yyyy}.");
            if (await registrar.NetworkConflictAsync(Clean(request.VatsimId), Clean(request.IvaoId), ct) is string conflict)
                throw new AppException(AppErrorKind.Conflict, conflict);
        }

        var user = new ApplicationUser { UserName = email, Email = email };
        var created = await userManager.CreateAsync(user, request.Password);

        if (!created.Succeeded)
            throw new AppException(AppErrorKind.Validation, string.Join(" ", created.Errors.Select(e => e.Description)));

        if (isFirstPilot)
        {
            var pilot = await registrar.CreateAsync(user, request.FirstName, request.LastName, hubIcao, LegalTerms.CurrentVersion, ct);
            pilot.SetNetworkIds(request.VatsimId, request.IvaoId, null);
            await db.SaveChangesAsync(ct);

            var roleResult = await userManager.AddToRoleAsync(user, StaffPositions.CeoFounder);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Could not assign the CEO & Founder position.");

            await transaction.CommitAsync(ct);
            return new RegisterResponse(await BuildResponseAsync(user, pilot), null);
        }

        var application = new PilotApplication(user.Id, request.FirstName, request.LastName, hubIcao, request.VatsimId, request.IvaoId,
            request.Simulator, request.AddOns, request.Motivation, LegalTerms.CurrentVersion);
        db.PilotApplications.Add(application);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var name = $"{application.FirstName} {application.LastName}";
        var reviewers = await ReviewerPilotIdsAsync(ct);
        notifier.SendMany(reviewers, NotificationType.Application, "New application",
            $"{name} applied to join Litoral. Check the profile and approve or reject the application.", "/admin/applications");

        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Pilots, "Application submitted", "Application", application.Id.ToString(), name,
            $"Email: {email}\nVATSIM: {application.VatsimId ?? "-"}\nIVAO: {application.IvaoId ?? "-"}\nSimulator: {application.Simulator}",
            ActorName: name), ct);

        return new RegisterResponse(null, new ApplicationStatusDto(application.FirstName, application.CreatedAt));
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            if (await gate.BlockedUntilAsync(email, ct) is DateTime until)
                throw new AppException(AppErrorKind.Unauthorized, $"Your application was not accepted. You can apply again on {until:d MMM yyyy}.");
            throw new AppException(AppErrorKind.Unauthorized, "Invalid email or password.");
        }

        var pilot = await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (pilot is null)
            return await ApplicationLoginAsync(user, request.Password, ct);

        var label = AuditContext.PilotLabel(pilot);

        if (await userManager.IsLockedOutAsync(user))
            throw new AppException(AppErrorKind.Unauthorized, TwoFactorService.LockedOutMessage);

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            await auditLog.WriteAsync(new AuditEntry(AuditCategory.Logins, "Login failed", "Pilot", pilot.Id.ToString(), label,
                "Wrong password", ActorName: "Visitor"), ct);
            throw new AppException(AppErrorKind.Unauthorized, "Invalid email or password.");
        }

        var trustedDevice = user.TwoFactorEnabled && twoFactorTokens.IsTrustedDevice(user, request.TrustedDevice);

        if (user.TwoFactorEnabled && !trustedDevice)
            return new LoginResponse(null, twoFactorTokens.CreateChallenge(user));

        await userManager.ResetAccessFailedCountAsync(user);
        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Logins, "Login", "Pilot", pilot.Id.ToString(), label,
            LoginDetails(pilot, trustedDevice ? "Trusted device" : null), user.Id, label), ct);

        return new LoginResponse(await BuildResponseAsync(user, pilot), null);
    }

    public async Task<AuthResponse> LoginWithTwoFactorAsync(TwoFactorLoginRequest request, CancellationToken ct)
    {
        var user = await twoFactorTokens.ReadChallengeAsync(request.ChallengeToken)
            ?? throw new AppException(AppErrorKind.Unauthorized, "Your sign-in expired. Sign in again.");

        var pilot = await LoadPilotAsync(user.Id, ct);
        var label = AuditContext.PilotLabel(pilot);

        if (await userManager.IsLockedOutAsync(user))
            throw new AppException(AppErrorKind.Unauthorized, TwoFactorService.LockedOutMessage);

        var result = await twoFactorTokens.VerifyCodeAsync(user, request.Code);

        if (result == TwoFactorCodeResult.Invalid)
        {
            await userManager.AccessFailedAsync(user);
            await auditLog.WriteAsync(new AuditEntry(AuditCategory.Logins, "Login failed", "Pilot", pilot.Id.ToString(), label,
                "Wrong two-factor code", ActorName: "Visitor"), ct);
            throw new AppException(AppErrorKind.Validation, "Invalid code.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Logins, "Login", "Pilot", pilot.Id.ToString(), label,
            LoginDetails(pilot, result == TwoFactorCodeResult.RecoveryCode ? "Recovery code used" : "Two-factor code"), user.Id, label), ct);

        var response = await BuildResponseAsync(user, pilot);
        return request.RememberDevice ? response with { TrustedDevice = twoFactorTokens.CreateTrustedDevice(user) } : response;
    }

    public async Task<PilotSummary> GetCurrentAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new AppException(AppErrorKind.NotFound, "User not found.");

        var pilot = await LoadPilotAsync(userId, ct);
        var roles = await userManager.GetRolesAsync(user);

        return PilotSummaryMapper.ToSummary(user, pilot, roles.ToList(), twoFactorOptions.Value.RequiredForStaff);
    }

    private async Task<LoginResponse> ApplicationLoginAsync(ApplicationUser user, string password, CancellationToken ct)
    {
        var application = await db.PilotApplications.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == user.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

        if (await userManager.IsLockedOutAsync(user))
            throw new AppException(AppErrorKind.Unauthorized, TwoFactorService.LockedOutMessage);

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            throw new AppException(AppErrorKind.Unauthorized, "Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return new LoginResponse(null, null, new ApplicationStatusDto(application.FirstName, application.CreatedAt));
    }

    private async Task<List<Guid>> ReviewerPilotIdsAsync(CancellationToken ct)
    {
        var reviewers = (await staffRoles.ByUserAsync(ct))
            .Where(r => r.Value.Contains(StaffPositions.HeadOfHr) || r.Value.Contains(StaffPositions.CeoFounder))
            .Select(r => r.Key)
            .ToList();

        return await db.Pilots.Where(p => reviewers.Contains(p.UserId)).Select(p => p.Id).ToListAsync(ct);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? LoginDetails(Pilot pilot, string? method)
    {
        var parts = new[] { method, pilot.IsActive ? null : "Account is inactive" }.OfType<string>().ToList();
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private async Task<Pilot> LoadPilotAsync(string userId, CancellationToken ct) =>
        await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.UserId == userId, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

    private async Task<AuthResponse> BuildResponseAsync(ApplicationUser user, Pilot pilot)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var (token, expiresAt) = tokenGenerator.Generate(user, pilot, roles);

        return new AuthResponse(token, expiresAt, PilotSummaryMapper.ToSummary(user, pilot, roles, twoFactorOptions.Value.RequiredForStaff));
    }
}