using Litoral.Application.Admin;
using Litoral.Application.Auth;
using Litoral.Application.Common;
using Litoral.Domain.Enums;
using Litoral.Domain.Staff;
using Litoral.Infrastructure.Admin;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Litoral.Infrastructure.Auth;

public sealed class TwoFactorService(
    LitoralDbContext db,
    UserManager<ApplicationUser> userManager,
    TwoFactorTokens tokens,
    StaffRoles staffRoles,
    Notifier notifier,
    IAuditLog auditLog,
    IOptions<TwoFactorOptions> options) : ITwoFactorService
{
    private const int RecoveryCodeCount = 10;
    public const string LockedOutMessage = "Too many failed attempts. Try again in a few minutes.";

    public async Task<TwoFactorStatus> GetStatusAsync(string userId, CancellationToken ct)
    {
        var user = await FindUserAsync(userId);
        var roles = await staffRoles.OfUserAsync(userId, ct);
        var left = user.TwoFactorEnabled ? await userManager.CountRecoveryCodesAsync(user) : 0;

        return new TwoFactorStatus(user.TwoFactorEnabled, options.Value.RequiredForStaff && roles.Count > 0, left);
    }

    public async Task<TwoFactorSetup> BeginSetupAsync(string userId, CancellationToken ct)
    {
        var user = await FindUserAsync(userId);
        if (user.TwoFactorEnabled)
            throw new AppException(AppErrorKind.Conflict, "Two-factor authentication is already enabled.");

        await userManager.ResetAuthenticatorKeyAsync(user);
        var key = await userManager.GetAuthenticatorKeyAsync(user)
            ?? throw new InvalidOperationException("Could not create the authenticator key.");

        var issuer = Uri.EscapeDataString(options.Value.Issuer);
        var account = Uri.EscapeDataString(user.Email!);
        var uri = $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6";

        return new TwoFactorSetup(string.Join(' ', key.ToLowerInvariant().Chunk(4).Select(c => new string(c))), uri);
    }

    public async Task<RecoveryCodesResponse> EnableAsync(string userId, TwoFactorEnableRequest request, CancellationToken ct)
    {
        var user = await FindUserAsync(userId);
        if (user.TwoFactorEnabled)
            throw new AppException(AppErrorKind.Conflict, "Two-factor authentication is already enabled.");
        if (await userManager.GetAuthenticatorKeyAsync(user) is null)
            throw new AppException(AppErrorKind.Validation, "Start the setup again.");

        var code = TwoFactorTokens.AuthenticatorCode(request.Code);
        if (code is null || !await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code))
            throw new AppException(AppErrorKind.Validation, "Invalid code. Check that the time on your phone is correct and try again.");

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var codes = await NewRecoveryCodesAsync(user);

        await WriteAuditAsync(userId, "Two-factor enabled", ct);
        return codes;
    }

    public async Task<RecoveryCodesResponse> RegenerateRecoveryCodesAsync(string userId, TwoFactorConfirmRequest request, CancellationToken ct)
    {
        var user = await FindEnabledUserAsync(userId);
        await ConfirmAsync(user, request);

        var codes = await NewRecoveryCodesAsync(user);
        await WriteAuditAsync(userId, "Recovery codes regenerated", ct);
        return codes;
    }

    public async Task DisableAsync(string userId, TwoFactorConfirmRequest request, CancellationToken ct)
    {
        var user = await FindEnabledUserAsync(userId);
        await ConfirmAsync(user, request);

        await TurnOffAsync(user);
        await WriteAuditAsync(userId, "Two-factor disabled", ct);
    }

    public async Task ResetAsync(string actorUserId, Guid pilotId, CancellationToken ct)
    {
        var pilot = await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.Id == pilotId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

        if (pilot.UserId == actorUserId)
            throw new AppException(AppErrorKind.Validation, "Manage your own two-factor authentication in your settings.");
        if ((await staffRoles.OfUserAsync(pilot.UserId, ct)).Contains(StaffPositions.CeoFounder))
            throw new AppException(AppErrorKind.Validation, "The CEO & Founder account can't be changed.");

        var user = await FindUserAsync(pilot.UserId);
        if (!user.TwoFactorEnabled)
            throw new AppException(AppErrorKind.Conflict, $"{pilot.DisplayId} doesn't have two-factor authentication enabled.");

        await TurnOffAsync(user);

        notifier.Send(pilot.Id, NotificationType.Security, "Two-factor authentication reset",
            "The staff reset your two-factor authentication. Set it up again in Settings to keep your account protected.",
            "/profile?tab=settings");

        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Pilots, "Two-factor reset", "Pilot", pilot.Id.ToString(),
            AuditContext.PilotLabel(pilot)), ct);
    }

    private async Task ConfirmAsync(ApplicationUser user, TwoFactorConfirmRequest request)
    {
        if (await userManager.IsLockedOutAsync(user))
            throw new AppException(AppErrorKind.Validation, LockedOutMessage);

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            throw new AppException(AppErrorKind.Validation, "The password is incorrect.");
        }

        if (await tokens.VerifyCodeAsync(user, request.Code) == TwoFactorCodeResult.Invalid)
        {
            await userManager.AccessFailedAsync(user);
            throw new AppException(AppErrorKind.Validation, "Invalid code.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
    }

    private async Task TurnOffAsync(ApplicationUser user)
    {
        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);
    }

    private async Task<RecoveryCodesResponse> NewRecoveryCodesAsync(ApplicationUser user)
    {
        var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount)
            ?? throw new InvalidOperationException("Could not create the recovery codes.");
        return new RecoveryCodesResponse(codes.ToList());
    }

    private async Task WriteAuditAsync(string userId, string action, CancellationToken ct)
    {
        var pilot = await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");
        var label = AuditContext.PilotLabel(pilot);

        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Account, action, "Pilot", pilot.Id.ToString(), label, null, userId, label), ct);
    }

    private async Task<ApplicationUser> FindEnabledUserAsync(string userId)
    {
        var user = await FindUserAsync(userId);
        if (!user.TwoFactorEnabled)
            throw new AppException(AppErrorKind.Conflict, "Two-factor authentication is not enabled.");
        return user;
    }

    private async Task<ApplicationUser> FindUserAsync(string userId) =>
        await userManager.FindByIdAsync(userId)
        ?? throw new AppException(AppErrorKind.NotFound, "User not found.");
}