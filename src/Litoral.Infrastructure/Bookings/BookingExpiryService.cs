using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Litoral.Infrastructure.Bookings;

public sealed class BookingExpiryService(IServiceScopeFactory scopes, ILogger<BookingExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await ExpireAsync(stoppingToken);
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

        var specials = await ExpireSpecialFlightsAsync(db, now, ct);

        var expired = await db.Bookings
            .Include(b => b.Aircraft)
            .Include(b => b.Pilot)
            .Where(b => b.Status == BookingStatus.Booked && b.ExpiresAt <= now && b.Type != BookingType.Special)
            .ToListAsync(ct);

        foreach (var booking in expired)
            booking.Expire(now);

        if (expired.Count == 0 && specials == 0) return;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Expired {Bookings} bookings and {Specials} special flights.", expired.Count, specials);
    }

    private static async Task<int> ExpireSpecialFlightsAsync(LitoralDbContext db, DateTime now, CancellationToken ct)
    {
        var flights = await db.SpecialFlights
            .Include(f => f.Pilot)
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
                flight.Pilot.AddPoints(-BookingSupport.SpecialSchedulePenalty, PointTransactionType.Penalty, $"Special flight {flight.Title} expired");

            flight.Expire(now);
            count++;
        }

        return count;
    }
}