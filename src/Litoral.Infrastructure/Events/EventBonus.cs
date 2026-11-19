using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Events;

public sealed class EventBonus(LitoralDbContext db, Notifier notifier)
{
    public async Task OnApprovedAsync(Pirep pirep, CancellationToken ct)
    {
        var at = pirep.BlockOffAt;

        var candidates = await db.AirlineEvents
            .Where(e => e.IsPublished && e.StartsAt <= at && e.EndsAt > at)
            .ToListAsync(ct);

        var best = candidates
            .Where(e => e.Covers(pirep.DepartureIcao, pirep.ArrivalIcao, at))
            .OrderByDescending(e => e.BonusPoints)
            .FirstOrDefault();
        if (best is null) return;

        if (await db.AirlineEventFlights.AnyAsync(f => f.PirepId == pirep.Id, ct)) return;

        db.AirlineEventFlights.Add(new AirlineEventFlight(best, pirep.Pilot, pirep.Id, best.BonusPoints));
        if (best.BonusPoints <= 0) return;

        pirep.Pilot.AddPoints(best.BonusPoints, PointTransactionType.EventBonus, $"Event {best.Title} · {pirep.Callsign}");
        notifier.Send(pirep.Pilot.Id, NotificationType.Bonus, $"+{best.BonusPoints:N0} event points",
            $"{pirep.Callsign} counted for the {best.Title} event.", "/points");
    }
}