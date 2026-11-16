using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Admin;

public sealed class RouteAdminService(LitoralDbContext db) : IRouteAdminService
{
    public async Task<IReadOnlyList<AdminRouteDto>> ListAsync(CancellationToken ct)
    {
        var routes = await RoutesQuery().AsNoTracking().OrderBy(r => r.FlightNumber).ToListAsync(ct);
        return routes.Select(ToDto).ToList();
    }

    public async Task<AdminRouteDto> GetAsync(Guid id, CancellationToken ct) =>
        ToDto(await FindAsync(id, ct));

    public async Task<IReadOnlyList<AdminRouteDto>> CreateAsync(CreateRouteRequest request, CancellationToken ct)
    {
        var departure = await FindAirportAsync(request.DepartureIcao, ct);
        var arrival = await FindAirportAsync(request.ArrivalIcao, ct);
        var types = await LoadAircraftTypesAsync(request.AircraftTypeIds, ct);

        await EnsureFlightNumberIsFreeAsync(request.FlightNumber, null, ct);

        var route = new Route(request.FlightNumber, departure, arrival, types, request.EstimatedMinutes);
        db.Routes.Add(route);
        var created = new List<Route> { route };

        if (request.CreateReturn)
        {
            if (request.ReturnFlightNumber is not int returnNumber)
                throw new AppException(AppErrorKind.Validation, "Enter the return flight number.");
            if (returnNumber == request.FlightNumber)
                throw new AppException(AppErrorKind.Validation, "The return flight number must be different from the outbound one.");

            await EnsureFlightNumberIsFreeAsync(returnNumber, null, ct);

            var returnRoute = new Route(returnNumber, arrival, departure, types, request.EstimatedMinutes);
            db.Routes.Add(returnRoute);
            created.Add(returnRoute);
        }

        await db.SaveChangesAsync(ct);
        return created.Select(ToDto).ToList();
    }

    public async Task<AdminRouteDto> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken ct)
    {
        var route = await FindAsync(id, ct);

        if (request.FlightNumber != route.FlightNumber)
            await EnsureFlightNumberIsFreeAsync(request.FlightNumber, id, ct);

        var departure = await FindAirportAsync(request.DepartureIcao, ct);
        var arrival = await FindAirportAsync(request.ArrivalIcao, ct);
        var types = await LoadAircraftTypesAsync(request.AircraftTypeIds, ct);

        route.Update(request.FlightNumber, departure, arrival, types, request.EstimatedMinutes);

        if (request.IsActive) route.Activate();
        else route.Deactivate();

        await db.SaveChangesAsync(ct);
        return ToDto(route);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var route = await FindAsync(id, ct);

        if (await db.Bookings.AnyAsync(b => b.RouteId == id, ct))
            throw new AppException(AppErrorKind.Conflict, "This route has bookings. Deactivate it instead of deleting.");
        if (await db.RosterLegs.AnyAsync(l => l.RouteId == id, ct))
            throw new AppException(AppErrorKind.Conflict, "This route is part of a monthly roster. Deactivate it instead of deleting.");

        db.Routes.Remove(route);
        await db.SaveChangesAsync(ct);
    }

    public async Task<FlightNumberSuggestionDto> SuggestFlightNumberAsync(string? departureIcao, string? arrivalIcao, bool cargo, CancellationToken ct)
    {
        var block = cargo ? FlightBlock.FerryAndCargo
            : string.IsNullOrWhiteSpace(departureIcao) || string.IsNullOrWhiteSpace(arrivalIcao) ? FlightBlock.Domestic
            : await ResolveBlockAsync(Normalize(departureIcao), Normalize(arrivalIcao), ct);

        var (min, max) = FlightBlockRules.Range(block);
        var used = (await db.Routes
                .Where(r => r.FlightNumber >= min && r.FlightNumber <= max)
                .Select(r => r.FlightNumber)
                .ToListAsync(ct))
            .ToHashSet();

        for (var number = min + 1; number < max; number += 2)
        {
            if (!used.Contains(number) && !used.Contains(number + 1))
                return new FlightNumberSuggestionDto(block.ToString(), number, number + 1);
        }

        for (var number = min; number <= max; number++)
        {
            if (!used.Contains(number))
                return new FlightNumberSuggestionDto(block.ToString(), number, null);
        }

        throw new AppException(AppErrorKind.Conflict, $"There are no free flight numbers in the {block} block.");
    }

    public async Task<IReadOnlyList<AircraftTypeDto>> ListAircraftTypesAsync(CancellationToken ct) =>
        await db.AircraftTypes
            .AsNoTracking()
            .OrderBy(t => t.IsCargo)
            .ThenBy(t => t.IcaoCode)
            .Select(t => new AircraftTypeDto(t.Id, t.IcaoCode, t.Name, t.IsCargo, t.MaxRangeNm))
            .ToListAsync(ct);

    private async Task<FlightBlock> ResolveBlockAsync(string departureIcao, string arrivalIcao, CancellationToken ct)
    {
        string[] codes = [departureIcao, arrivalIcao];

        var catalog = await db.AirportCatalog
            .AsNoTracking()
            .Where(a => codes.Contains(a.Icao))
            .ToDictionaryAsync(a => a.Icao, ct);

        var registered = await db.Airports
            .AsNoTracking()
            .Where(a => codes.Contains(a.Icao))
            .ToDictionaryAsync(a => a.Icao, a => a.Country, ct);

        string CountryCode(string icao) =>
            catalog.TryGetValue(icao, out var entry) ? entry.CountryCode
            : registered.GetValueOrDefault(icao) == "Brazil" ? "BR"
            : string.Empty;

        if (CountryCode(departureIcao) == "BR" && CountryCode(arrivalIcao) == "BR")
            return FlightBlock.Domestic;

        var foreign = CountryCode(departureIcao) == "BR" ? arrivalIcao : departureIcao;
        var continent = catalog.TryGetValue(foreign, out var foreignEntry) ? foreignEntry.Continent : string.Empty;

        return continent switch
        {
            "SA" => FlightBlock.SouthAmerica,
            "NA" => FlightBlock.CentralNorthAmerica,
            _ => FlightBlock.EuropeAndOthers
        };
    }

    private IQueryable<Route> RoutesQuery() =>
        db.Routes
            .Include(r => r.DepartureAirport)
            .Include(r => r.ArrivalAirport)
            .Include(r => r.AircraftTypes);

    private async Task<Route> FindAsync(Guid id, CancellationToken ct) =>
        await RoutesQuery().FirstOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Route not found.");

    private async Task<Airport> FindAirportAsync(string icao, CancellationToken ct)
    {
        var code = Normalize(icao);
        return await db.Airports.FirstOrDefaultAsync(a => a.Icao == code, ct)
            ?? throw new AppException(AppErrorKind.NotFound, $"Airport {code} is not registered.");
    }

    private async Task<List<AircraftType>> LoadAircraftTypesAsync(IReadOnlyList<Guid>? ids, CancellationToken ct)
    {
        var distinctIds = (ids ?? []).Distinct().ToList();
        var types = await db.AircraftTypes.Where(t => distinctIds.Contains(t.Id)).ToListAsync(ct);

        if (types.Count != distinctIds.Count)
            throw new AppException(AppErrorKind.Validation, "One or more aircraft types were not found.");

        return types;
    }

    private async Task EnsureFlightNumberIsFreeAsync(int flightNumber, Guid? ignoreRouteId, CancellationToken ct)
    {
        if (await db.Routes.AnyAsync(r => r.FlightNumber == flightNumber && r.Id != ignoreRouteId, ct))
            throw new AppException(AppErrorKind.Conflict, $"Flight number {flightNumber} is already in use.");
    }

    private static AdminRouteDto ToDto(Route route)
    {
        var types = route.AircraftTypes.OrderBy(t => t.IcaoCode).ToList();

        return new AdminRouteDto(
            route.Id,
            route.FlightNumber,
            route.Callsign,
            route.Block.ToString(),
            route.DepartureAirport.Icao,
            route.DepartureAirport.City,
            route.ArrivalAirport.Icao,
            route.ArrivalAirport.City,
            route.IsCargo,
            types.Select(t => t.Id).ToList(),
            types.Select(t => t.IcaoCode).ToList(),
            route.DistanceNm,
            route.EstimatedMinutes,
            route.IsActive);
    }

    private static string Normalize(string? icao) => (icao ?? string.Empty).Trim().ToUpperInvariant();
}