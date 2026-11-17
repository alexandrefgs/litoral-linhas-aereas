using Litoral.Application.Operations;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Litoral.Application.Common;

namespace Litoral.Infrastructure.Operations;

public sealed class SpecialFlightSupport(LitoralDbContext db)
{
    private const int FirstNumber = 9000;
    private const int LastNumber = 9999;
    public IQueryable<SpecialFlight> Query() =>
        db.SpecialFlights
            .Include(f => f.Aircraft).ThenInclude(a => a.AircraftType)
            .Include(f => f.Aircraft).ThenInclude(a => a.CurrentAirport)
            .Include(f => f.Pilot).ThenInclude(p => p!.Rank)
            .Include(f => f.Legs).ThenInclude(l => l.Route).ThenInclude(r => r.DepartureAirport)
            .Include(f => f.Legs).ThenInclude(l => l.Route).ThenInclude(r => r.ArrivalAirport)
            .Include(f => f.Legs).ThenInclude(l => l.Route).ThenInclude(r => r.AircraftTypes)
            .Include(f => f.Legs).ThenInclude(l => l.Booking)
            .AsSplitQuery();
    
    public async Task<List<Route>> CreateRoutesAsync(List<Airport> stops, AircraftType type, IEnumerable<int> releasedNumbers, CancellationToken ct)
    {
        var released = releasedNumbers.ToHashSet();
        var used = (await db.Routes
            .Where(r => r.FlightNumber >= FirstNumber && r.FlightNumber <= LastNumber)
            .Select(r => r.FlightNumber)
            .ToListAsync(ct))
            .Where(n => !released.Contains(n))
            .ToHashSet();

        var routes = new List<Route>();
        var number = LastNumber;

        for (var i = 0; i < stops.Count - 1; i++)
        {
            while (number >= FirstNumber && (used.Contains(number) || released.Contains(number))) number--;
            if (number < FirstNumber)
                throw new AppException(AppErrorKind.Conflict, "There are no free flight numbers in the 9XXX block.");

            routes.Add(Route.CreateSpecial(number, stops[i], stops[i + 1], type));
            used.Add(number);
        }

        db.Routes.AddRange(routes);
        return routes;
    }

    public async Task<Dictionary<int, Rank>> RanksByLevelAsync(CancellationToken ct) =>
        await db.Ranks.AsNoTracking().ToDictionaryAsync(r => r.Level, ct);

    public static string StateOf(SpecialFlight flight, DateTime now) => flight.Status switch
    {
        SpecialFlightStatus.Open when now < flight.StartsAt => "ComingSoon",
        SpecialFlightStatus.Open when now >= flight.EndsAt => "Expired",
        SpecialFlightStatus.Open => "Available",
        _ => flight.Status.ToString()
    };

    public static SpecialFlightDto ToDto(SpecialFlight flight, IReadOnlyDictionary<int, Rank> ranks, DateTime now)
    {
        var rank = ranks.GetValueOrDefault(flight.MinimumRankLevel);

        return new SpecialFlightDto(
            flight.Id,
            flight.Title,
            flight.Description,
            flight.Type.ToString(),
            flight.Difficulty,
            flight.AircraftId,
            flight.Aircraft.Registration,
            flight.Aircraft.AircraftType.IcaoCode,
            flight.Aircraft.AircraftType.Name,
            flight.Aircraft.PhotoUrl,
            rank?.Code ?? flight.MinimumRankLevel.ToString(),
            rank?.Name ?? $"Level {flight.MinimumRankLevel}",
            flight.StartsAt,
            flight.EndsAt,
            flight.Status.ToString(),
            StateOf(flight, now),
            flight.Pilot?.PilotNumber,
            flight.Pilot?.DisplayId,
            flight.Pilot?.PublicName,
            flight.PhotoUrl,
            flight.PhotoCredit,
            flight.OrderedLegs.Select(l => new SpecialFlightLegDto(
                l.Order,
                l.Route.Callsign,
                l.Route.DepartureAirport.Icao,
                l.Route.DepartureAirport.City,
                l.Route.ArrivalAirport.Icao,
                l.Route.ArrivalAirport.City,
                l.Route.DistanceNm,
                l.Route.EstimatedMinutes,
                l.Route.DepartureAirport.Country != l.Route.ArrivalAirport.Country,
                l.Booking?.Status.ToString(),
                l.CompletedAt)).ToList(),
            flight.CreatedAt);
    }
}