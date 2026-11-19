using Litoral.Application.Admin;
using Litoral.Application.Bookings;
using Litoral.Application.Common;
using Litoral.Application.Tours;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Bookings;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Tours;

public sealed class TourService(LitoralDbContext db, TourSupport support, BookingSupport bookings, IAuditLog auditLog) : ITourService
{
    private sealed record LegState(Guid TourId, int Order, string State);

    public async Task<IReadOnlyList<PilotTourDto>> ListAsync(string userId, CancellationToken ct)
    {
        var pilot = await bookings.GetPilotAsync(userId, ct);
        var now = DateTime.UtcNow;

        var tours = await support.Query().AsNoTracking().Where(t => t.IsPublished).ToListAsync(ct);
        var progress = await db.TourProgresses.AsNoTracking().Where(p => p.PilotId == pilot.Id).ToDictionaryAsync(p => p.TourId, ct);
        var completed = await db.TourProgresses.AsNoTracking()
            .Where(p => p.CompletedAt != null)
            .GroupBy(p => p.TourId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        var states = await LegStatesAsync(pilot.Id, ct);
        var openBooking = await bookings.FindOpenBookingAsync(pilot.Id, ct);
        var ranks = await support.RanksByLevelAsync(ct);

        return tours
            .Where(t => !t.HasEnded(now) || progress.ContainsKey(t.Id))
            .Select(t => ToDto(t, pilot, progress.GetValueOrDefault(t.Id), states.GetValueOrDefault(t.Id), openBooking, completed.GetValueOrDefault(t.Id), ranks, now))
            .OrderBy(t => t.State switch { "Open" => 0, "ComingSoon" => 1, _ => 2 })
            .ThenByDescending(t => t.Progress is { CompletedAt: null })
            .ThenBy(t => t.StartsAt)
            .ThenBy(t => t.Title)
            .ToList();
    }

    public async Task<AircraftOptionsDto> AircraftAsync(string userId, Guid tourId, CancellationToken ct)
    {
        var (_, leg) = await EnsureCanBookAsync(userId, tourId, ct);
        return bookings.ToOptions(leg.Route, await bookings.AvailableAircraftAsync(leg.Route, ct));
    }

    public async Task<BookingDto> BookNextLegAsync(string userId, Guid tourId, BookTourLegRequest request, CancellationToken ct)
    {
        var (pilot, leg) = await EnsureCanBookAsync(userId, tourId, ct);
        var tour = leg.Tour;
        var now = DateTime.UtcNow;

        var aircraft = await bookings.PickAircraftAsync(leg.Route, request.AircraftId, ct);
        var expiresAt = now + Booking.DefaultValidity;
        if (tour.EndsAt is { } end && end < expiresAt) expiresAt = end;

        var booking = new Booking(pilot, leg.Route, aircraft, BookingType.Tour, expiresAt: expiresAt);
        booking.LinkTourLeg(leg);
        db.Bookings.Add(booking);

        if (!await db.TourProgresses.AnyAsync(p => p.TourId == tour.Id && p.PilotId == pilot.Id, ct))
            db.TourProgresses.Add(new TourProgress(tour, pilot));

        await bookings.AssignCrewAsync(booking, pilot, ct);
        await db.SaveChangesAsync(ct);
        await auditLog.WriteAsync(BookingSupport.AuditEntryFor(booking, "Flight booked", $"Tour: {tour.Title} (leg {leg.Order} of {tour.Legs.Count})"), ct);

        return BookingSupport.ToDto(booking);
    }

    private async Task<(Pilot Pilot, TourLeg Leg)> EnsureCanBookAsync(string userId, Guid tourId, CancellationToken ct)
    {
        var pilot = await bookings.GetPilotAsync(userId, ct);
        var tour = await support.Query().FirstOrDefaultAsync(t => t.Id == tourId && t.IsPublished, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Tour not found.");

        var progress = await db.TourProgresses.AsNoTracking().FirstOrDefaultAsync(p => p.TourId == tour.Id && p.PilotId == pilot.Id, ct);
        var states = await LegStatesAsync(pilot.Id, ct);
        var openBooking = await bookings.FindOpenBookingAsync(pilot.Id, ct);
        var ranks = await support.RanksByLevelAsync(ct);

        var dto = ToDto(tour, pilot, progress, states.GetValueOrDefault(tour.Id), openBooking, 0, ranks, DateTime.UtcNow);
        if (!dto.CanBook)
            throw new AppException(AppErrorKind.Conflict, dto.BlockedReason ?? "You can't book this tour leg right now.");

        return (pilot, tour.OrderedLegs[dto.NextLegOrder!.Value - 1]);
    }

    private async Task<Dictionary<Guid, LegState>> LegStatesAsync(Guid pilotId, CancellationToken ct)
    {
        var rows = await db.Bookings.AsNoTracking()
            .Where(b => b.PilotId == pilotId && b.TourLegId != null
                && (b.Status == BookingStatus.Booked
                    || b.Status == BookingStatus.InProgress
                    || (b.Status == BookingStatus.Completed && db.Pireps.Any(p => p.BookingId == b.Id && p.Status == PirepStatus.Pending))))
            .Select(b => new { b.TourLeg!.TourId, b.TourLeg.Order, b.Status, b.CreatedAt })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.TourId)
            .Select(g => g.OrderByDescending(r => r.CreatedAt).First())
            .ToDictionary(r => r.TourId, r => new LegState(r.TourId, r.Order, r.Status switch
            {
                BookingStatus.Booked => "Booked",
                BookingStatus.InProgress => "InProgress",
                _ => "PendingReview"
            }));
    }

    private static PilotTourDto ToDto(
        Tour tour,
        Pilot pilot,
        TourProgress? progress,
        LegState? current,
        Booking? openBooking,
        int pilotsCompleted,
        IReadOnlyDictionary<int, Rank> ranks,
        DateTime now)
    {
        var state = TourSupport.State(tour, now);
        var legs = tour.OrderedLegs;
        var done = progress?.CompletedLegs ?? 0;
        var finished = progress?.IsCompleted == true;
        int? nextOrder = finished ? null : done + 1;
        var next = nextOrder is { } o && o <= legs.Count ? legs[o - 1] : null;
        var rankName = ranks.GetValueOrDefault(tour.MinimumRankLevel)?.Name ?? $"Level {tour.MinimumRankLevel}";

        string? blocked =
            finished ? null
            : !pilot.IsActive ? "Your account is inactive."
            : state == "ComingSoon" ? "This tour has not started yet."
            : state == "Ended" ? "This tour has ended."
            : pilot.Rank.Level < tour.MinimumRankLevel ? $"Requires {rankName}."
            : current is { State: "PendingReview" } ? $"Leg {current.Order} is waiting for the PIREP review."
            : current is { State: "Booked" or "InProgress" } ? $"Leg {current.Order} is already booked. Fly it from the briefing."
            : openBooking is not null ? $"You have an open flight ({openBooking.Callsign})."
            : next is null ? null
            : pilot.CurrentLocationIcao != next.Route.DepartureAirport.Icao ? $"You must be at {next.Route.DepartureAirport.Icao} for leg {next.Order}. Use a jumpseat if needed."
            : null;

        var canBook = !finished && next is not null && blocked is null;

        return new PilotTourDto(
            tour.Id,
            tour.Title,
            tour.Description,
            tour.ImageUrl,
            tour.StartsAt,
            tour.EndsAt,
            state,
            rankName,
            TourSupport.IsCargo(tour),
            TourSupport.AircraftTypes(tour),
            tour.BonusPoints,
            TourSupport.Award(tour),
            TourSupport.Legs(tour),
            legs.Sum(l => l.Route.DistanceNm),
            pilotsCompleted,
            progress is null ? null : new TourProgressDto(progress.CompletedLegs, progress.StartedAt, progress.CompletedAt, current?.State),
            nextOrder,
            canBook,
            blocked);
    }
}