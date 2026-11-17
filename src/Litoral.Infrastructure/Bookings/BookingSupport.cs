using Litoral.Application.Bookings;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Bookings;

public sealed class BookingSupport(LitoralDbContext db)
{
    public const int CustomFlightCost = 50;
    public const int SpecialSchedulePenalty = 500;
    public const string FallbackCrewName = "Severino";

    public async Task<Pilot> GetPilotAsync(string userId, CancellationToken ct) =>
        await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.UserId == userId, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

    public IQueryable<Booking> BookingQuery() =>
        db.Bookings
            .Include(b => b.Pilot).ThenInclude(p => p.Rank)
            .Include(b => b.Route).ThenInclude(r => r.DepartureAirport)
            .Include(b => b.Route).ThenInclude(r => r.ArrivalAirport)
            .Include(b => b.Aircraft).ThenInclude(a => a.AircraftType)
            .Include(b => b.CrewPilot).ThenInclude(c => c!.Rank);

    public Task<Booking?> FindOpenBookingAsync(Guid pilotId, CancellationToken ct) =>
        BookingQuery().FirstOrDefaultAsync(
            b => b.PilotId == pilotId && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.InProgress), ct);

    public async Task EnsureNoOpenBookingAsync(Guid pilotId, CancellationToken ct)
    {
        if (!await db.Pilots.AnyAsync(p => p.Id == pilotId && p.IsActive, ct))
            throw new AppException(AppErrorKind.Conflict, "Your account is inactive. Contact the Head of HR.");
        var open = await FindOpenBookingAsync(pilotId, ct);
        if (open is not null)
            throw new AppException(AppErrorKind.Conflict, $"You already have an open flight ({open.Callsign}). Cancel it or complete it first.");
    }

    public IQueryable<Route> RouteQuery(bool includeSpecial = false) =>
        db.Routes
            .Where(r => includeSpecial || !r.IsSpecial)
            .Include(r => r.DepartureAirport)
            .Include(r => r.ArrivalAirport)
            .Include(r => r.AircraftTypes);

    public async Task<Route> GetRouteAsync(Guid routeId, CancellationToken ct) =>
        await RouteQuery(includeSpecial: true).FirstOrDefaultAsync(r => r.Id == routeId, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Route not found.");

    public async Task<HashSet<string>> GetBaseIcaosAsync(CancellationToken ct)
    {
        var hubs = await db.Hubs.AsNoTracking().Include(h => h.Airport).ToListAsync(ct);
        return hubs.Where(h => h.Airport.Supports(OperationType.Passenger)).Select(h => h.Airport.Icao).ToHashSet();
    }

    public async Task<List<Aircraft>> AvailableAircraftAsync(Route route, CancellationToken ct)
    {
        var typeIds = route.AircraftTypes.Select(t => t.Id).ToList();

        return await db.Aircraft
            .Include(a => a.AircraftType)
            .Where(a => a.CurrentAirportId == route.DepartureAirportId
                && a.Status == AircraftStatus.Available
                && typeIds.Contains(a.AircraftTypeId))
            .OrderBy(a => a.Registration)
            .ToListAsync(ct);
    }

    public async Task<Aircraft> PickAircraftAsync(Route route, Guid? aircraftId, CancellationToken ct)
    {
        var options = await AvailableAircraftAsync(route, ct);

        if (options.Count == 0)
            throw new AppException(AppErrorKind.Conflict, $"There is no aircraft available at {route.DepartureAirport.Icao} for this flight. Bring one from the Fleet page.");

        if (aircraftId is null)
            return options[Random.Shared.Next(options.Count)];

        return options.FirstOrDefault(a => a.Id == aircraftId)
            ?? throw new AppException(AppErrorKind.Conflict, "This aircraft is no longer available. Choose another one.");
    }

    public async Task AssignCrewAsync(Booking booking, Pilot pilot, CancellationToken ct)
    {
        var levels = await db.Ranks.AsNoTracking().Select(r => r.Level).OrderBy(l => l).ToListAsync(ct);
        if (levels.Count < 2) return;

        var targetLevel = pilot.Rank.Level == levels[0] ? levels[^1] : levels[0];
        var busy = db.Bookings.Where(b => b.Status == BookingStatus.Booked || b.Status == BookingStatus.InProgress);

        var crew = await db.Pilots
            .Include(p => p.Rank)
            .Where(p => p.IsActive && p.Id != pilot.Id && p.Rank.Level == targetLevel)
            .Where(p => !busy.Any(b => b.PilotId == p.Id || b.CrewPilotId == p.Id))
            .OrderBy(p => p.LastCrewAssignedAt.HasValue)
            .ThenBy(p => p.LastCrewAssignedAt)
            .ThenBy(p => p.PilotNumber)
            .FirstOrDefaultAsync(ct);

        if (crew is not null)
            booking.AssignCrew(crew);
    }

    public AircraftOptionsDto ToOptions(Route route, IEnumerable<Aircraft> aircraft) =>
        new(route.Id, route.Callsign, (int)Booking.DefaultValidity.TotalHours, aircraft.Select(ToDto).ToList());

    public static BookingAirportDto ToDto(Airport airport) =>
        new(airport.Icao, airport.Name, airport.City, airport.Country, airport.Latitude, airport.Longitude);

    public static BookingAircraftDto ToDto(Aircraft aircraft) =>
        new(
            aircraft.Id,
            aircraft.Registration,
            aircraft.AircraftTypeId,
            aircraft.AircraftType.IcaoCode,
            aircraft.AircraftType.Name,
            aircraft.Variant,
            aircraft.PhotoUrl,
            aircraft.PhotoCredit,
            aircraft.LastFlightAt);

    public static BookingDto ToDto(Booking booking) =>
        new(
            booking.Id,
            booking.Callsign,
            booking.Pilot.NetworkCallsign,
            booking.Route.FlightNumber,
            booking.Type.ToString(),
            booking.Status.ToString(),
            ToDto(booking.Route.DepartureAirport),
            ToDto(booking.Route.ArrivalAirport),
            booking.Route.DistanceNm,
            booking.Route.EstimatedMinutes,
            booking.Route.IsCargo,
            ToDto(booking.Aircraft),
            booking.CrewPilot is { } crew
                ? new CrewDto(crew.DisplayId, $"{crew.FirstName} {crew.LastName}", crew.Rank.Code)
                : FallbackCrew(booking.Pilot),
            booking.PointsCost,
            booking.CreatedAt,
            booking.ExpiresAt);
    
    private static CrewDto FallbackCrew(Pilot pilot)
    {
        var rank = pilot.Rank.Level == 1 ? "CMD" : "FO";
        return new CrewDto($"{rank}0000", FallbackCrewName, rank);
    }
}