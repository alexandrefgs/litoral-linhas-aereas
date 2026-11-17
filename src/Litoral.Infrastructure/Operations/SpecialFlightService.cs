using Litoral.Application.Common;
using Litoral.Application.Operations;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Bookings;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Operations;

public sealed class SpecialFlightService(LitoralDbContext db, SpecialFlightSupport support, BookingSupport bookings) : ISpecialFlightService
{
    private static readonly TimeSpan HistoryWindow = TimeSpan.FromDays(30);

    private static readonly Dictionary<string, int> StateOrder = new()
    {
        ["Available"] = 0,
        ["ComingSoon"] = 1,
        ["Taken"] = 2,
        ["Completed"] = 3,
        ["Expired"] = 4
    };

    public async Task<IReadOnlyList<PilotSpecialFlightDto>> ListAsync(string userId, CancellationToken ct)
    {
        var pilot = await bookings.GetPilotAsync(userId, ct);
        var now = DateTime.UtcNow;
        var since = now - HistoryWindow;

        var flights = await support.Query().AsNoTracking()
            .Where(f => f.Status == SpecialFlightStatus.Open
                || f.Status == SpecialFlightStatus.Taken
                || ((f.Status == SpecialFlightStatus.Completed || f.Status == SpecialFlightStatus.Expired) && f.ClosedAt >= since))
            .ToListAsync(ct);

        var ranks = await support.RanksByLevelAsync(ct);
        var openBooking = await bookings.FindOpenBookingAsync(pilot.Id, ct);

        return flights
            .Select(f => ToDto(f, pilot, openBooking, ranks, now))
            .OrderByDescending(f => f.IsMine && f.Flight.State == "Taken")
            .ThenBy(f => StateOrder.GetValueOrDefault(f.Flight.State, 9))
            .ThenBy(f => f.Flight.State is "Completed" or "Expired" ? -f.Flight.EndsAt.Ticks : f.Flight.StartsAt.Ticks)
            .ToList();
    }

    public async Task<PilotSpecialFlightDto> TakeAsync(string userId, Guid id, CancellationToken ct)
    {
        var pilot = await bookings.GetPilotAsync(userId, ct);
        await bookings.EnsureNoOpenBookingAsync(pilot.Id, ct);

        var flight = await FindAsync(id, ct);
        var now = DateTime.UtcNow;

        if (flight.Aircraft.Status != AircraftStatus.Available)
            throw new AppException(AppErrorKind.Conflict, $"Aircraft {flight.Aircraft.Registration} is not available right now.");

        flight.Take(pilot, now);
        await BookLegAsync(flight, flight.NextLeg!, pilot, ct);

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(flight.Id, userId, ct);
    }

    public async Task<PilotSpecialFlightDto> BookNextLegAsync(string userId, Guid id, CancellationToken ct)
    {
        var pilot = await bookings.GetPilotAsync(userId, ct);
        await bookings.EnsureNoOpenBookingAsync(pilot.Id, ct);

        var flight = await FindAsync(id, ct);
        var now = DateTime.UtcNow;

        if (flight.Status != SpecialFlightStatus.Taken || flight.PilotId != pilot.Id)
            throw new AppException(AppErrorKind.Conflict, "This special flight is not yours.");
        if (now >= flight.EndsAt)
            throw new AppException(AppErrorKind.Conflict, "This special flight has ended.");

        var leg = flight.NextLeg
            ?? throw new AppException(AppErrorKind.Conflict, "All legs are already completed.");

        if (pilot.CurrentLocationIcao != leg.Route.DepartureAirport.Icao)
            throw new AppException(AppErrorKind.Conflict, $"You must be at {leg.Route.DepartureAirport.Icao} to book this leg.");

        await BookLegAsync(flight, leg, pilot, ct);

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(flight.Id, userId, ct);
    }

    private async Task BookLegAsync(SpecialFlight flight, SpecialFlightLeg leg, Pilot pilot, CancellationToken ct)
    {
        var booking = new Booking(pilot, leg.Route, flight.Aircraft, BookingType.Special, expiresAt: flight.EndsAt);
        db.Bookings.Add(booking);
        flight.AttachBooking(leg, booking);
        await bookings.AssignCrewAsync(booking, pilot, ct);
    }

    private async Task<SpecialFlight> FindAsync(Guid id, CancellationToken ct) =>
        await support.Query().FirstOrDefaultAsync(f => f.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Special flight not found.");

    private async Task<PilotSpecialFlightDto> ToDtoAsync(Guid id, string userId, CancellationToken ct)
    {
        var pilot = await bookings.GetPilotAsync(userId, ct);
        var flight = await support.Query().AsNoTracking().FirstAsync(f => f.Id == id, ct);
        var openBooking = await bookings.FindOpenBookingAsync(pilot.Id, ct);
        return ToDto(flight, pilot, openBooking, await support.RanksByLevelAsync(ct), DateTime.UtcNow);
    }

    private static PilotSpecialFlightDto ToDto(SpecialFlight flight, Pilot pilot, Booking? openBooking, IReadOnlyDictionary<int, Rank> ranks, DateTime now)
    {
        var dto = SpecialFlightSupport.ToDto(flight, ranks, now);
        var isMine = flight.PilotId == pilot.Id;
        var next = flight.NextLeg;

        string? blocked = null;
        var canTake = false;

        if (dto.State == "Available")
        {
            var first = flight.OrderedLegs[0].Route.DepartureAirport.Icao;
            blocked =
                !pilot.IsActive ? "Your account is inactive."
                : pilot.Rank.Level < flight.MinimumRankLevel ? $"Requires {dto.MinimumRankName}."
                : openBooking is not null ? $"You have an open flight ({openBooking.Callsign})."
                : pilot.CurrentLocationIcao != first ? $"You must be at {first}."
                : flight.Aircraft.Status != AircraftStatus.Available ? $"{flight.Aircraft.Registration} is not available right now."
                : null;
            canTake = blocked is null;
        }

        var canBookNext = isMine
            && flight.Status == SpecialFlightStatus.Taken
            && now < flight.EndsAt
            && next is not null
            && next.Booking?.IsOpen != true
            && openBooking is null
            && pilot.CurrentLocationIcao == next.Route.DepartureAirport.Icao;

        if (isMine && flight.Status == SpecialFlightStatus.Taken && next is not null && next.Booking?.IsOpen != true && !canBookNext)
            blocked = openBooking is not null ? $"You have an open flight ({openBooking.Callsign})."
                : now >= flight.EndsAt ? "This special flight has ended."
                : $"You must be at {next.Route.DepartureAirport.Icao} for leg {next.Order}.";

        return new PilotSpecialFlightDto(dto, isMine, canTake, blocked, next?.Order, canBookNext);
    }
}