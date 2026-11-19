using System.Data;
using Litoral.Application.Admin;
using Litoral.Application.Auth;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Legal;
using Litoral.Domain.Staff;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Identity;
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
    IAuditLog auditLog,
    IOptions<TwoFactorOptions> twoFactorOptions) : IAuthService
{
    private const string DefaultHubIcao = "SBGR";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (!request.AcceptedTerms)
            throw new AppException(AppErrorKind.Validation, "You must accept the Regulations and the Privacy Policy.");
        var email = request.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
            throw new AppException(AppErrorKind.Conflict, "This email is already registered.");

        var hubIcao = string.IsNullOrWhiteSpace(request.HubIcao)
            ? DefaultHubIcao
            : request.HubIcao.Trim().ToUpperInvariant();

        var isPassengerHub = await db.Hubs.AnyAsync(
            h => h.Airport.Icao == hubIcao && (h.Airport.Operations & OperationType.Passenger) == OperationType.Passenger, ct);

        if (!isPassengerHub)
            throw new AppException(AppErrorKind.Validation, $"{hubIcao} is not a Litoral passenger hub.");

        var initialRank = await db.Ranks.OrderBy(r => r.Level).FirstAsync(ct);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var isFirstPilot = !await db.Pilots.AnyAsync(ct);
        var pilotNumber = await NextPilotNumberAsync(ct);

        var user = new ApplicationUser { UserName = email, Email = email };
        var created = await userManager.CreateAsync(user, request.Password);

        if (!created.Succeeded)
            throw new AppException(AppErrorKind.Validation, string.Join(" ", created.Errors.Select(e => e.Description)));

        var pilot = new Pilot(user.Id, pilotNumber, request.FirstName.Trim(), request.LastName.Trim(), initialRank, hubIcao);
        pilot.AcceptTerms(LegalTerms.CurrentVersion);
        db.Pilots.Add(pilot);
        await db.SaveChangesAsync(ct);

        if (isFirstPilot)
        {
            var roleResult = await userManager.AddToRoleAsync(user, StaffPositions.CeoFounder);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Could not assign the CEO & Founder position.");
        }

        await transaction.CommitAsync(ct);

        return await BuildResponseAsync(user, pilot);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());

        if (user is null)
            throw new AppException(AppErrorKind.Unauthorized, "Invalid email or password.");

        var pilot = await LoadPilotAsync(user.Id, ct);
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

    private async Task<int> NextPilotNumberAsync(CancellationToken ct)
    {
        var numbers = await db.Pilots.OrderBy(p => p.PilotNumber).Select(p => p.PilotNumber).ToListAsync(ct);

        var next = Pilot.FirstPilotNumber;
        foreach (var number in numbers)
        {
            if (number != next) break;
            next++;
        }

        return next;
    }

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