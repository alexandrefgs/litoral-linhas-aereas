using Litoral.Domain.Entities;

namespace Litoral.Infrastructure.Events;

public static class EventSupport
{
    public static string State(AirlineEvent e, DateTime now) =>
        !e.IsPublished ? "Draft"
        : now >= e.EndsAt ? "Ended"
        : now >= e.StartsAt ? "Live"
        : "Upcoming";

    public static DateTime Utc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value
        : DateTime.SpecifyKind(value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value, DateTimeKind.Utc);
}