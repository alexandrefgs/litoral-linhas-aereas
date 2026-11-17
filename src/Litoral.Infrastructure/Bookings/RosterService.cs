using Litoral.Application.Bookings;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Planning;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Bookings;

public sealed class RosterService(LitoralDbContext db, BookingSupport support) : IRosterService
{
    public async Task<RosterStatusDto> GetAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        return await StatusAsync(pilot, ct);
    }

    public async Task<RosterStatusDto> CreateAsync(string userId, CreateRosterRequest request, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var today = DateTime.UtcNow;

        if (await db.MonthlyRosters.AnyAsync(r => r.PilotId == pilot.Id && r.Year == today.Year && r.Month == today.Month, ct))
            throw new AppException(AppErrorKind.Conflict, "You already have a roster for this month.");

        if (request.FlightsPerDay is < 1 or > MonthlyRoster.MaxFlightsPerDay)
            throw new AppException(AppErrorKind.Validation, $"Choose between 1 and {MonthlyRoster.MaxFlightsPerDay} flights per day.");

        var routes = await support.RouteQuery()
            .Where(r => r.IsActive
                && r.DepartureAirport.MinimumRankLevel <= pilot.Rank.Level
                && r.ArrivalAirport.MinimumRankLevel <= pilot.Rank.Level)
            .ToListAsync(ct);

        var edges = routes.Select(r => new RosterEdge(r.Id, r.DepartureAirport.Icao, r.ArrivalAirport.Icao)).ToList();
        var total = RemainingDays(today) * request.FlightsPerDay;
        var plan = RosterPlanner.Plan(pilot.BaseIcao, edges, total, Random.Shared);

        var byId = routes.ToDictionary(r => r.Id);
        var sequences = plan.Select(s => (IReadOnlyList<Route>)s.Select(e => byId[e.RouteId]).ToList()).ToList();

        db.MonthlyRosters.Add(new MonthlyRoster(pilot, today.Year, today.Month, request.FlightsPerDay, sequences));
        await db.SaveChangesAsync(ct);

        return await StatusAsync(pilot, ct);
    }

    public async Task<AircraftOptionsDto> ListLegAircraftAsync(string userId, Guid legId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var (_, leg) = await GetNextLegAsync(pilot, legId, ct);
        var route = await support.GetRouteAsync(leg.RouteId, ct);

        return support.ToOptions(route, await support.AvailableAircraftAsync(route, ct));
    }

    public async Task<BookingDto> BookLegAsync(string userId, Guid legId, BookLegRequest request, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        await support.EnsureNoOpenBookingAsync(pilot.Id, ct);

        var (_, leg) = await GetNextLegAsync(pilot, legId, ct);
        var route = await support.GetRouteAsync(leg.RouteId, ct);

        if (pilot.CurrentLocationIcao != route.DepartureAirport.Icao)
        {
            if (!route.DepartureAirport.IsAccessibleTo(pilot.Rank))
                throw new AppException(AppErrorKind.Validation, $"Your rank does not allow operations at {route.DepartureAirport.Icao}.");

            pilot.MoveTo(route.DepartureAirport.Icao);
        }

        var aircraft = await support.PickAircraftAsync(route, request.AircraftId, ct);
        var booking = new Booking(pilot, route, aircraft, BookingType.Monthly);
        leg.AttachBooking(booking);

        db.Bookings.Add(booking);
        await support.AssignCrewAsync(booking, pilot, ct);
        await db.SaveChangesAsync(ct);

        return BookingSupport.ToDto(booking);
    }

    private async Task<(MonthlyRoster Roster, RosterLeg Leg)> GetNextLegAsync(Pilot pilot, Guid legId, CancellationToken ct)
    {
        var roster = await CurrentRosterQuery(pilot.Id).FirstOrDefaultAsync(ct)
            ?? throw new AppException(AppErrorKind.NotFound, "You don't have a roster for this month.");

        var next = roster.NextLeg
            ?? throw new AppException(AppErrorKind.Conflict, "Your roster is already completed.");

        if (next.Id != legId)
            throw new AppException(AppErrorKind.Validation, "Roster flights must be flown in order. Book the next one.");

        return (roster, next);
    }

    private IQueryable<MonthlyRoster> CurrentRosterQuery(Guid pilotId)
    {
        var today = DateTime.UtcNow;

        return db.MonthlyRosters
            .Include(r => r.Legs).ThenInclude(l => l.Route).ThenInclude(r => r.DepartureAirport)
            .Include(r => r.Legs).ThenInclude(l => l.Route).ThenInclude(r => r.ArrivalAirport)
            .Include(r => r.Legs).ThenInclude(l => l.Booking)
            .Where(r => r.PilotId == pilotId && r.Year == today.Year && r.Month == today.Month);
    }

    private async Task<RosterStatusDto> StatusAsync(Pilot pilot, CancellationToken ct)
    {
        var roster = await CurrentRosterQuery(pilot.Id).AsNoTracking().FirstOrDefaultAsync(ct);
        var open = await support.FindOpenBookingAsync(pilot.Id, ct);
        var next = roster?.NextLeg;
        var otherOpen = open is not null && open.Id != next?.BookingId;

        return new RosterStatusDto(
            roster is null ? null : ToDto(roster, next),
            RemainingDays(DateTime.UtcNow),
            roster is null,
            pilot.CurrentLocationIcao,
            otherOpen);
    }

    private static RosterDto ToDto(MonthlyRoster roster, RosterLeg? next)
    {
        var legs = roster.Legs.OrderBy(l => l.Order).Select(l => new RosterLegDto(
            l.Id,
            l.Order,
            l.Sequence,
            l.RouteId,
            l.Route.Callsign,
            l.Route.DepartureAirport.Icao,
            l.Route.DepartureAirport.City,
            l.Route.ArrivalAirport.Icao,
            l.Route.ArrivalAirport.City,
            l.Route.IsCargo,
            l.Route.DistanceNm,
            l.Route.EstimatedMinutes,
            l.CompletedAt is not null ? "Completed"
                : l.Id != next?.Id ? "Locked"
                : l.Booking is { IsOpen: true } ? "Booked"
                : "Next",
            l.Route.ArrivalAirport.PhotoUrl)).ToList();

        return new RosterDto(
            roster.Id,
            roster.Year,
            roster.Month,
            roster.FlightsPerDay,
            roster.BaseIcao,
            legs.Count,
            legs.Count(l => l.State == "Completed"),
            roster.Bonus,
            roster.CompletedAt,
            legs);
    }

    private static int RemainingDays(DateTime today) =>
        DateTime.DaysInMonth(today.Year, today.Month) - today.Day + 1;
}