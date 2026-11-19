using Litoral.Application.Admin;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Operations;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Litoral.Infrastructure.Monitoring;

namespace Litoral.Infrastructure.Bookings;

public sealed class BookingExpiryService(IServiceScopeFactory scopes, SystemMonitor monitor, ILogger<BookingExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await monitor.TrackJobAsync("Booking expiry and maintenance planning", Interval, () => ExpireAsync(stoppingToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to expire bookings.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LitoralDbContext>();
        var now = DateTime.UtcNow;

        var maintenance = scope.ServiceProvider.GetRequiredService<MaintenanceFerryPlanner>();
        var notifier = scope.ServiceProvider.GetRequiredService<Notifier>();
        var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLog>();
        var logs = new List<AuditEntry>();

        var specials = await ExpireSpecialFlightsAsync(db, maintenance, notifier, logs, now, ct);

        var expired = await db.Bookings
            .Include(b => b.Aircraft)
            .Include(b => b.Pilot)
            .Where(b => b.Status == BookingStatus.Booked && b.ExpiresAt <= now && b.Type != BookingType.Special)
            .ToListAsync(ct);

        foreach (var booking in expired)
        {
            booking.Expire(now);
            notifier.Send(booking.PilotId, NotificationType.BookingExpired,
                $"{booking.Callsign} expired",
                "The flight was not started within 24 hours and the booking was released. You can book a new flight.",
                "/book");
            logs.Add(new AuditEntry(AuditCategory.Bookings, "Booking expired", "Booking", booking.Id.ToString(), booking.Callsign,
                $"Pilot: {AuditContext.PilotLabel(booking.Pilot)}").AsSystem());
        }

        if (expired.Count > 0 || specials > 0)
        {
            await db.SaveChangesAsync(ct);
            foreach (var entry in logs)
                await auditLog.WriteAsync(entry, ct);
            logger.LogInformation("Expired {Bookings} bookings and {Specials} special flights.", expired.Count, specials);
        }

        var due = await maintenance.PlanDueAsync(ct);
        if (due > 0)
            logger.LogInformation("Handled {Count} aircraft due for maintenance.", due);
    }

    private static async Task<int> ExpireSpecialFlightsAsync(
        LitoralDbContext db, MaintenanceFerryPlanner maintenance, Notifier notifier, List<AuditEntry> logs, DateTime now, CancellationToken ct)
    {
        var flights = await db.SpecialFlights
            .Include(f => f.Pilot)
            .Include(f => f.Aircraft).ThenInclude(a => a.AircraftType)
            .Include(f => f.Legs).ThenInclude(l => l.Booking).ThenInclude(b => b!.Aircraft)
            .Where(f => (f.Status == SpecialFlightStatus.Open || f.Status == SpecialFlightStatus.Taken) && f.EndsAt <= now)
            .ToListAsync(ct);

        var count = 0;
        foreach (var flight in flights)
        {
            var booking = flight.NextLeg?.Booking;
            if (booking?.Status == BookingStatus.InProgress) continue;

            if (booking?.Status == BookingStatus.Booked)
                booking.Expire(now);

            if (flight.Status == SpecialFlightStatus.Taken && flight.Pilot is not null)
            {
                flight.Pilot.AddPoints(-BookingSupport.SpecialSchedulePenalty, PointTransactionType.Penalty, $"Special flight {flight.Title} expired");
                notifier.Send(flight.Pilot.Id, NotificationType.BookingExpired,
                    $"{flight.Title} expired",
                    $"The special flight window closed before it was finished. −{BookingSupport.SpecialSchedulePenalty} points.",
                    "/points");
                logs.Add(new AuditEntry(AuditCategory.Bookings, "Special flight expired", "Special flight", flight.Id.ToString(), flight.Title,
                    $"Pilot: {AuditContext.PilotLabel(flight.Pilot)}\nPenalty: −{BookingSupport.SpecialSchedulePenalty}").AsSystem());
            }

            flight.Expire(now);
            await maintenance.AfterSpecialClosedAsync(flight, flight.Aircraft, ct);
            count++;
        }

        return count;
    }
}