using System.Security.Cryptography;
using Litoral.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Litoral.Infrastructure.Auth;

public enum TwoFactorCodeResult
{
    Invalid,
    Authenticator,
    RecoveryCode
}

public sealed class TwoFactorTokens(
    IDataProtectionProvider dataProtection,
    UserManager<ApplicationUser> userManager,
    IOptions<TwoFactorOptions> options)
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly ITimeLimitedDataProtector _challenge =
        dataProtection.CreateProtector("Litoral.TwoFactor.Challenge").ToTimeLimitedDataProtector();

    private readonly ITimeLimitedDataProtector _trustedDevice =
        dataProtection.CreateProtector("Litoral.TwoFactor.TrustedDevice").ToTimeLimitedDataProtector();

    public string CreateChallenge(ApplicationUser user) =>
        _challenge.Protect(Payload(user), ChallengeLifetime);

    public string CreateTrustedDevice(ApplicationUser user) =>
        _trustedDevice.Protect(Payload(user), TimeSpan.FromDays(options.Value.TrustedDeviceDays));

    public async Task<ApplicationUser?> ReadChallengeAsync(string token)
    {
        if (Read(_challenge, token) is not [var userId, var stamp]) return null;

        var user = await userManager.FindByIdAsync(userId);
        return user is not null && user.TwoFactorEnabled && user.SecurityStamp == stamp ? user : null;
    }

    public bool IsTrustedDevice(ApplicationUser user, string? token) =>
        !string.IsNullOrWhiteSpace(token)
        && Read(_trustedDevice, token) is [var userId, var stamp]
        && userId == user.Id
        && stamp == user.SecurityStamp;

    public async Task<TwoFactorCodeResult> VerifyCodeAsync(ApplicationUser user, string code)
    {
        if (AuthenticatorCode(code) is string totp)
            return await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, totp)
                ? TwoFactorCodeResult.Authenticator
                : TwoFactorCodeResult.Invalid;

        var recovery = RecoveryCode(code);
        if (recovery is null) return TwoFactorCodeResult.Invalid;

        var redeemed = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, recovery);
        return redeemed.Succeeded ? TwoFactorCodeResult.RecoveryCode : TwoFactorCodeResult.Invalid;
    }

    public static string? AuthenticatorCode(string code)
    {
        var digits = code.Replace(" ", "").Replace("-", "");
        return digits.Length == 6 && digits.All(char.IsAsciiDigit) ? digits : null;
    }

    private static string? RecoveryCode(string code)
    {
        var clean = code.Replace(" ", "").Replace("-", "").ToUpperInvariant();
        return clean.Length == 10 && clean.All(char.IsAsciiLetterOrDigit) ? $"{clean[..5]}-{clean[5..]}" : null;
    }

    private static string Payload(ApplicationUser user) => $"{user.Id}|{user.SecurityStamp}";

    private static string[]? Read(ITimeLimitedDataProtector protector, string token)
    {
        try
        {
            return protector.Unprotect(token).Split('|');
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}