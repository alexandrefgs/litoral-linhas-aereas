using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class SystemSetting : Entity
{
    public const string RegistrationOpen = "Registration.Open";

    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private SystemSetting() { }

    public SystemSetting(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public void Set(string value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}