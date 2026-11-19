using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Tours;

public sealed class TourProgressTracker(LitoralDbContext db, Notifier notifier)
{
    public const string SystemGrantor = "System · Tour completed";

    public async Task OnApprovedAsync(Pirep pirep, CancellationToken ct)
    {
        if (pirep.Booking.TourLegId is not Guid legId) return;

        var leg = await db.TourLegs
            .Include(l => l.Route).ThenInclude(r => r.ArrivalAirport)
            .Include(l => l.Tour).ThenInclude(t => t.Award)
            .Include(l => l.Tour).ThenInclude(t => t.Legs)
            .FirstOrDefaultAsync(l => l.Id == legId, ct);
        if (leg is null) return;

        var tour = leg.Tour;
        var pilot = pirep.Pilot;

        if (pirep.ArrivalIcao != leg.Route.ArrivalAirport.Icao)
        {
            notifier.Send(pilot.Id, NotificationType.Tour, $"{tour.Title}: leg {leg.Order} not counted",
                $"You landed at {pirep.ArrivalIcao} instead of {leg.Route.ArrivalAirport.Icao}. Fly the leg again to continue the tour.", "/tours");
            return;
        }

        if (tour.EndsAt is { } end && pirep.BlockOffAt >= end) return;

        var progress = await db.TourProgresses.FirstOrDefaultAsync(p => p.TourId == tour.Id && p.PilotId == pilot.Id, ct);
        if (progress is null)
        {
            progress = new TourProgress(tour, pilot);
            db.TourProgresses.Add(progress);
        }

        var now = DateTime.UtcNow;
        if (!progress.CompleteLeg(leg.Order, tour.Legs.Count, now)) return;

        if (!progress.IsCompleted)
        {
            notifier.Send(pilot.Id, NotificationType.Tour, $"{tour.Title}: leg {leg.Order} of {tour.Legs.Count} completed",
                "Keep going! The next leg is waiting for you.", "/tours");
            return;
        }

        if (tour.BonusPoints > 0)
            pilot.AddPoints(tour.BonusPoints, PointTransactionType.TourBonus, $"Tour {tour.Title} completed");

        if (!await db.PilotAwards.AnyAsync(a => a.AwardId == tour.AwardId && a.PilotId == pilot.Id, ct))
            db.PilotAwards.Add(new PilotAward(pilot, tour.Award, null, SystemGrantor));

        notifier.Send(pilot.Id, NotificationType.Award, $"Tour completed: {tour.Title}",
            tour.BonusPoints > 0 ? $"+{tour.BonusPoints:N0} points and the {tour.Award.Name} award." : $"You earned the {tour.Award.Name} award.",
            $"/profile/{pilot.PilotNumber}?tab=awards");
    }
}