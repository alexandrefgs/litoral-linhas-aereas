namespace Litoral.Infrastructure.Privacy;

public sealed class PrivacyOptions
{
    public const string SectionName = "Privacy";

    public int AuditLogRetentionDays { get; set; } = 730;
}