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

        var expired = await db.Bookings
            .Include(b => b.Aircraft)
            .Include(b => b.Pilot)
            .Where(b => b.Status == BookingStatus.Booked && b.ExpiresAt <= now)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        foreach (var booking in expired)
        {
            if (!booking.Expire(now)) continue;

            if (booking.Type == BookingType.Special)
                booking.Pilot.AddPoints(-BookingSupport.SpecialSchedulePenalty, PointTransactionType.Penalty, $"Special schedule {booking.Callsign} expired");
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Expired {Count} bookings.", expired.Count);
    }
}