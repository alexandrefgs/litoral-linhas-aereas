using Litoral.Application.Events;
using Litoral.Infrastructure.Bookings;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Events;

public sealed class EventService(LitoralDbContext db, BookingSupport bookings) : IEventService
{
    private static readonly TimeSpan HistoryWindow = TimeSpan.FromDays(30);

    public async Task<IReadOnlyList<PilotEventDto>> ListAsync(string userId, CancellationToken ct)
    {
        var pilot = await bookings.GetPilotAsync(userId, ct);
        var now = DateTime.UtcNow;
        var since = now - HistoryWindow;

        var events = await db.AirlineEvents.AsNoTracking()
            .Where(e => e.IsPublished && e.EndsAt >= since)
            .ToListAsync(ct);
        var ids = events.Select(e => e.Id).ToList();

        var stats = await db.AirlineEventFlights.AsNoTracking()
            .Where(f => ids.Contains(f.EventId))
            .GroupBy(f => f.EventId)
            .Select(g => new
            {
                g.Key,
                Flights = g.Count(),
                Pilots = g.Select(f => f.PilotId).Distinct().Count(),
                Mine = g.Count(f => f.PilotId == pilot.Id)
            })
            .ToDictionaryAsync(g => g.Key, ct);

        return events
            .Select(e =>
            {
                var s = stats.GetValueOrDefault(e.Id);
                return new PilotEventDto(e.Id, e.Title, e.Description, e.ImageUrl, e.StartsAt, e.EndsAt, EventSupport.State(e, now),
                    e.AirportList, e.BonusPoints, s?.Flights ?? 0, s?.Pilots ?? 0, s?.Mine ?? 0);
            })
            .OrderBy(e => e.State switch { "Live" => 0, "Upcoming" => 1, _ => 2 })
            .ThenBy(e => e.State == "Ended" ? -e.EndsAt.Ticks : e.StartsAt.Ticks)
            .ToList();
    }
}