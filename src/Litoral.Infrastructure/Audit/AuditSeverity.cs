using System.Linq.Expressions;
using Litoral.Domain.Entities;

namespace Litoral.Infrastructure.Audit;

public static class AuditSeverity
{
    public const string Critical = "Critical";
    public const string Warning = "Warning";
    public const string Info = "Info";

    private static readonly string[] CriticalActions = ["Account deleted", "Pilot deactivated", "PIREP rejected", "Two-factor reset"];
    private static readonly string[] WarningActions = ["Pilot reactivated", "Password changed", "Login failed", "Inactivity penalty started", "Special flight expired", "Two-factor disabled", "Recovery codes regenerated", "Privacy request created", "Privacy request rejected", "Optional data removed"];

    public static readonly Expression<Func<AuditLog, bool>> IsCritical = l =>
        CriticalActions.Contains(l.Action)
        || l.Action.EndsWith(" deleted")
        || l.Action.StartsWith("Staff position");

    public static string Of(string action)
    {
        if (CriticalActions.Contains(action)
            || action.EndsWith(" deleted", StringComparison.Ordinal)
            || action.StartsWith("Staff position", StringComparison.Ordinal))
            return Critical;

        if (WarningActions.Contains(action) || action.EndsWith(" updated", StringComparison.Ordinal))
            return Warning;

        return Info;
    }
}