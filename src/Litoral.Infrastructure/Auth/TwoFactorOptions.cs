namespace Litoral.Infrastructure.Auth;

public sealed class TwoFactorOptions
{
    public const string SectionName = "TwoFactor";

    public string Issuer { get; set; } = "Litoral";
    public bool RequiredForStaff { get; set; } = true;
    public int TrustedDeviceDays { get; set; } = 30;
}