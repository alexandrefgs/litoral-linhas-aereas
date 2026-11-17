using Litoral.Domain.Enums;

namespace Litoral.Domain.Fleet;

public static class MaintenancePolicy
{
    public static TimeSpan DurationOf(MaintenanceCheck check) => check switch
    {
        MaintenanceCheck.A => TimeSpan.FromHours(12),
        MaintenanceCheck.C => TimeSpan.FromDays(14),
        _ => TimeSpan.FromDays(30)
    };

    public static string DurationLabel(MaintenanceCheck check) => check switch
    {
        MaintenanceCheck.A => "12 hours",
        MaintenanceCheck.C => "14 days",
        _ => "30 days"
    };

    public static bool IsHeavy(MaintenanceCheck check) => check != MaintenanceCheck.A;
}