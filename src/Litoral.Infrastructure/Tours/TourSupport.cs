using Litoral.Application.Common;
using Litoral.Application.Tours;
using Litoral.Domain.Entities;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Tours;

public sealed class TourSupport(LitoralDbContext db)
{
    private static (int First, int Last) Block(bool cargo) => cargo ? (9000, 9999) : (7000, 7999);

    public IQueryable<Tour> Query() =>
        db.Tours
            .Include(t => t.Award)
            .Include(t => t.Legs).ThenInclude(l => l.Route).ThenInclude(r => r.DepartureAirport)
            .Include(t => t.Legs).ThenInclude(l => l.Route).ThenInclude(r => r.ArrivalAirport)
            .Include(t => t.Legs).ThenInclude(l => l.Route).ThenInclude(r => r.AircraftTypes)
            .AsSplitQuery();

    public async Task<IReadOnlyDictionary<int, Rank>> RanksByLevelAsync(CancellationToken ct) =>
        await db.Ranks.AsNoTracking().ToDictionaryAsync(r => r.Level, ct);

    public async Task<List<Route>> CreateRoutesAsync(IReadOnlyList<Airport> stops, IReadOnlyList<AircraftType> types, IEnumerable<int> releasedNumbers, CancellationToken ct)
    {
        var cargo = types[0].IsCargo;
        var (first, last) = Block(cargo);
        var released = releasedNumbers.ToHashSet();
        var used = (await db.Routes
            .Where(r => r.FlightNumber >= first && r.FlightNumber <= last)
            .Select(r => r.FlightNumber)
            .ToListAsync(ct))
            .ToHashSet();

        var routes = new List<Route>();
        var number = last;

        for (var i = 0; i < stops.Count - 1; i++)
        {
            while (number >= first && (used.Contains(number) || released.Contains(number))) number--;
            if (number < first)
                throw new AppException(AppErrorKind.Conflict, $"There are no free flight numbers in the {(cargo ? "9XXX" : "7XXX")} block.");

            routes.Add(Route.CreateTour(number, stops[i], stops[i + 1], types));
            used.Add(number);
        }

        db.Routes.AddRange(routes);
        return routes;
    }

    public static string State(Tour tour, DateTime now) =>
        !tour.IsPublished ? "Draft"
        : tour.HasEnded(now) ? "Ended"
        : !tour.HasStarted(now) ? "ComingSoon"
        : "Open";

    public static bool IsCargo(Tour tour) => tour.OrderedLegs.FirstOrDefault()?.Route.IsCargo ?? false;

    public static IReadOnlySet<Guid> AircraftTypeIds(Tour tour) =>
        tour.OrderedLegs.FirstOrDefault()?.Route.AircraftTypes.Select(t => t.Id).ToHashSet() ?? [];

    public static IReadOnlyList<string> AircraftTypes(Tour tour) =>
        tour.OrderedLegs.FirstOrDefault()?.Route.AircraftTypes.Select(t => t.IcaoCode).Distinct().OrderBy(c => c).ToList() ?? [];

    public static IReadOnlyList<TourLegDto> Legs(Tour tour) =>
        tour.OrderedLegs.Select(l => new TourLegDto(
                l.Order,
                l.Route.Callsign,
                Airport(l.Route.DepartureAirport),
                Airport(l.Route.ArrivalAirport),
                l.Route.DistanceNm,
                l.Route.EstimatedMinutes))
            .ToList();

    public static TourAwardDto Award(Tour tour) => new(tour.Award.Id, tour.Award.Name, tour.Award.ImageUrl);

    public static string AwardDescription(string title) => $"Completed every leg of the {title} tour.";

    private static TourAirportDto Airport(Airport a) => new(a.Icao, a.Name, a.City, a.Country, a.Latitude, a.Longitude);
}