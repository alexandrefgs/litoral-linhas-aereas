using Litoral.Application.Bookings;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Bookings;

public sealed class BookingService(LitoralDbContext db, BookingSupport support) : IBookingService
{
    public async Task<BookingOverviewDto> GetOverviewAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var current = await support.FindOpenBookingAsync(pilot.Id, ct);
        var location = await db.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.Icao == pilot.CurrentLocationIcao, ct);

        return new BookingOverviewDto(
            pilot.CurrentLocationIcao,
            location?.City ?? pilot.CurrentLocationIcao,
            pilot.BaseIcao,
            pilot.Points,
            BookingSupport.CustomFlightCost,
            current is null ? null : BookingSupport.ToDto(current));
    }

    public async Task<IReadOnlyList<DestinationDto>> ListDestinationsAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        return await DestinationsAsync(pilot, ct);
    }

    public async Task<AircraftOptionsDto> ListAircraftAsync(string userId, Guid routeId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var route = await support.GetRouteAsync(routeId, ct);
        EnsureDepartsFromPilot(route, pilot);

        return support.ToOptions(route, await support.AvailableAircraftAsync(route, ct));
    }

    public async Task<RandomProposalDto> ProposeRandomAsync(string userId, FlightDuration duration, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        await support.EnsureNoOpenBookingAsync(pilot.Id, ct);

        var matching = (await DestinationsAsync(pilot, ct)).Where(d => Matches(d.EstimatedMinutes, duration)).ToList();

        if (matching.Count == 0)
            throw new AppException(AppErrorKind.NotFound, $"There are no routes from {pilot.CurrentLocationIcao} for this duration.");

        var withAircraft = matching.Where(d => d.AvailableAircraft > 0).ToList();
        if (withAircraft.Count == 0)
            throw new AppException(AppErrorKind.Conflict, $"There is no aircraft available at {pilot.CurrentLocationIcao}. Bring one from the Fleet page for free.");

        var destination = withAircraft[Random.Shared.Next(withAircraft.Count)];
        var route = await support.GetRouteAsync(destination.RouteId, ct);
        var aircraft = await support.PickAircraftAsync(route, null, ct);

        return new RandomProposalDto(destination with { Cost = 0 }, BookingSupport.ToDto(aircraft));
    }

    public Task<BookingDto> BookRandomAsync(string userId, BookFlightRequest request, CancellationToken ct) =>
        BookAsync(userId, request, BookingType.Random, ct);

    public Task<BookingDto> BookCustomAsync(string userId, BookFlightRequest request, CancellationToken ct) =>
        BookAsync(userId, request, BookingType.Custom, ct);

    public async Task CancelCurrentAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var booking = await support.FindOpenBookingAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "You don't have an open flight.");
        var session = await db.FlightSessions
            .FirstOrDefaultAsync(s => s.BookingId == booking.Id && s.Status == FlightSessionStatus.Active, ct);
        
        session?.Abandon();
        booking.Cancel();

        if (booking.Type == BookingType.Special)
        {
            pilot.AddPoints(-BookingSupport.SpecialSchedulePenalty, PointTransactionType.Penalty, $"Special flight {booking.Callsign} cancelled");

            var leg = await db.SpecialFlightLegs
                .Include(l => l.SpecialFlight).ThenInclude(f => f.Legs)
                .FirstOrDefaultAsync(l => l.BookingId == booking.Id, ct);
            leg?.SpecialFlight.ReleaseLeg(booking.Id, DateTime.UtcNow);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<BookingDto> BookAsync(string userId, BookFlightRequest request, BookingType type, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        await support.EnsureNoOpenBookingAsync(pilot.Id, ct);

        var route = await support.GetRouteAsync(request.RouteId, ct);
        EnsureDepartsFromPilot(route, pilot);

        var cost = 0;
        if (type == BookingType.Custom)
        {
            var bases = await support.GetBaseIcaosAsync(ct);
            cost = bases.Contains(route.ArrivalAirport.Icao) ? 0 : BookingSupport.CustomFlightCost;
        }

        var aircraft = await support.PickAircraftAsync(route, request.AircraftId, ct);
        var booking = new Booking(pilot, route, aircraft, type, cost);

        if (cost > 0)
            pilot.Spend(cost, PointTransactionType.BookingCost, $"Custom flight {route.Callsign} {route.DepartureAirport.Icao}-{route.ArrivalAirport.Icao}");

        db.Bookings.Add(booking);
        await support.AssignCrewAsync(booking, pilot, ct);
        await db.SaveChangesAsync(ct);

        return BookingSupport.ToDto(booking);
    }

    private async Task<IReadOnlyList<DestinationDto>> DestinationsAsync(Pilot pilot, CancellationToken ct)
    {
        var routes = await support.RouteQuery()
            .AsNoTracking()
            .Where(r => r.IsActive
                && r.DepartureAirport.Icao == pilot.CurrentLocationIcao
                && r.ArrivalAirport.MinimumRankLevel <= pilot.Rank.Level)
            .OrderBy(r => r.FlightNumber)
            .ToListAsync(ct);

        var available = await db.Aircraft
            .AsNoTracking()
            .Where(a => a.CurrentAirport.Icao == pilot.CurrentLocationIcao && a.Status == AircraftStatus.Available)
            .Select(a => a.AircraftTypeId)
            .ToListAsync(ct);

        var bases = await support.GetBaseIcaosAsync(ct);

        return routes
            .Select(r =>
            {
                var typeIds = r.AircraftTypes.Select(t => t.Id).ToHashSet();
                return new DestinationDto(
                    r.Id,
                    r.FlightNumber,
                    r.Callsign,
                    BookingSupport.ToDto(r.ArrivalAirport),
                    r.DistanceNm,
                    r.EstimatedMinutes,
                    r.IsCargo,
                    r.AircraftTypes.Select(t => t.IcaoCode).Distinct().OrderBy(c => c).ToList(),
                    available.Count(typeIds.Contains),
                    bases.Contains(r.ArrivalAirport.Icao) ? 0 : BookingSupport.CustomFlightCost);
            })
            .ToList();
    }

    private static void EnsureDepartsFromPilot(Route route, Pilot pilot)
    {
        if (route.DepartureAirport.Icao != pilot.CurrentLocationIcao)
            throw new AppException(AppErrorKind.Validation, $"This flight departs from {route.DepartureAirport.Icao}, but you are at {pilot.CurrentLocationIcao}.");
    }

    private static bool Matches(int minutes, FlightDuration duration) => duration switch
    {
        FlightDuration.Short => minutes <= 60,
        FlightDuration.Medium => minutes is > 60 and <= 120,
        FlightDuration.Long => minutes > 120,
        _ => true
    };
}