using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Operations;
using Litoral.Domain.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Operations;

public sealed class SpecialFlightAdminService(LitoralDbContext db, SpecialFlightSupport support, MaintenanceFerryPlanner maintenance, IPhotoStorage photos) : ISpecialFlightAdminService
{
    private const string PhotoFolder = "special-flights";
    private const int FirstNumber = 9000;
    private const int LastNumber = 9999;

    private static readonly SpecialFlightStatus[] ActiveStatuses = [SpecialFlightStatus.Open, SpecialFlightStatus.Taken];

    public async Task<SpecialFlightOptionsDto> OptionsAsync(CancellationToken ct)
    {
        var busy = await db.SpecialFlights.AsNoTracking()
            .Where(f => ActiveStatuses.Contains(f.Status))
            .Select(f => f.AircraftId)
            .ToListAsync(ct);

        var aircraft = await db.Aircraft.AsNoTracking()
            .Where(a => a.Status != AircraftStatus.Inactive)
            .OrderBy(a => a.Registration)
            .Select(a => new OperationsAircraftDto(
                a.Id,
                a.Registration,
                a.AircraftType.IcaoCode,
                a.AircraftType.Name,
                a.AircraftType.IsCargo,
                a.CurrentAirport.Icao,
                a.Status.ToString(),
                a.AircraftType.DeliveryAirportIcao,
                busy.Contains(a.Id)))
            .ToListAsync(ct);

        var airports = await db.Airports.AsNoTracking()
            .OrderBy(a => a.Icao)
            .Select(a => new OperationsAirportDto(a.Icao, a.Name, a.City))
            .ToListAsync(ct);

        var ranks = await db.Ranks.AsNoTracking()
            .OrderBy(r => r.Level)
            .Select(r => new OperationsRankDto(r.Code, r.Name, r.Level))
            .ToListAsync(ct);

        return new SpecialFlightOptionsDto(Airline.MaintenanceHubIcao, aircraft, airports, ranks);
    }

    public async Task<IReadOnlyList<SpecialFlightDto>> ListAsync(CancellationToken ct)
    {
        var flights = await support.Query().AsNoTracking()
            .OrderByDescending(f => f.StartsAt)
            .ToListAsync(ct);

        var ranks = await support.RanksByLevelAsync(ct);
        var now = DateTime.UtcNow;
        return flights.Select(f => SpecialFlightSupport.ToDto(f, ranks, now)).ToList();
    }

    public async Task<SpecialFlightDto> CreateAsync(SpecialFlightRequest request, CancellationToken ct)
    {
        var input = await ResolveAsync(request, null, ct);
        var routes = await support.CreateRoutesAsync(input.Airports, input.Aircraft.AircraftType, [], ct);

        var flight = new SpecialFlight(
            request.Title, request.Description, input.Type, request.Difficulty, input.Aircraft,
            input.RankLevel, input.StartsAt, input.EndsAt, routes);

        db.SpecialFlights.Add(flight);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(flight.Id, ct);
    }

    public async Task<SpecialFlightDto> UpdateAsync(Guid id, SpecialFlightRequest request, CancellationToken ct)
    {
        var flight = await FindAsync(id, ct);
        if (flight.IsLocked)
            throw new AppException(AppErrorKind.Conflict, "This special flight was already taken and can't be edited.");

        var input = await ResolveAsync(request, id, ct);
        var oldRoutes = flight.Legs.Select(l => l.Route).ToList();
        var routes = await support.CreateRoutesAsync(input.Airports, input.Aircraft.AircraftType, oldRoutes.Select(r => r.FlightNumber), ct);

        flight.Update(
            request.Title, request.Description, input.Type, request.Difficulty, input.Aircraft,
            input.RankLevel, input.StartsAt, input.EndsAt, routes);

        db.Routes.RemoveRange(oldRoutes);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(flight.Id, ct);
    }

    public async Task<SpecialFlightDto> CancelAsync(Guid id, CancellationToken ct)
    {
        var flight = await FindAsync(id, ct);
        var booking = flight.NextLeg?.Booking;

        if (booking?.Status == BookingStatus.InProgress)
            throw new AppException(AppErrorKind.Conflict, "A leg of this special flight is in progress. Wait until it's finished.");

        if (booking?.Status == BookingStatus.Booked)
            booking.Cancel();

        flight.Cancel(DateTime.UtcNow);
        await maintenance.AfterSpecialClosedAsync(flight, flight.Aircraft, ct);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(flight.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var flight = await FindAsync(id, ct);
        var routeIds = flight.Legs.Select(l => l.RouteId).ToList();

        if (await db.Bookings.AnyAsync(b => routeIds.Contains(b.RouteId), ct))
            throw new AppException(AppErrorKind.Conflict, "This special flight was already flown or booked. Cancel it instead of deleting.");

        var routes = flight.Legs.Select(l => l.Route).ToList();
        var photo = flight.PhotoUrl;

        db.SpecialFlights.Remove(flight);
        db.Routes.RemoveRange(routes);
        await db.SaveChangesAsync(ct);
        photos.Delete(photo);
    }

    public async Task<SpecialFlightDto> UploadPhotoAsync(Guid id, PhotoUpload upload, CancellationToken ct)
    {
        var flight = await FindAsync(id, ct);
        var url = await photos.SaveAsync(PhotoFolder, upload, ct);
        var previous = flight.SetPhoto(url, upload.Credit);
        await db.SaveChangesAsync(ct);
        photos.Delete(previous);
        return await ToDtoAsync(flight.Id, ct);
    }

    public async Task<SpecialFlightDto> UpdatePhotoCreditAsync(Guid id, string? credit, CancellationToken ct)
    {
        var flight = await FindAsync(id, ct);
        flight.SetPhotoCredit(credit);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(flight.Id, ct);
    }

    public async Task<SpecialFlightDto> RemovePhotoAsync(Guid id, CancellationToken ct)
    {
        var flight = await FindAsync(id, ct);
        var previous = flight.SetPhoto(null, null);
        await db.SaveChangesAsync(ct);
        photos.Delete(previous);
        return await ToDtoAsync(flight.Id, ct);
    }

    private sealed record ResolvedInput(
        SpecialFlightType Type,
        Aircraft Aircraft,
        int RankLevel,
        DateTime StartsAt,
        DateTime EndsAt,
        List<Airport> Airports);

    private async Task<ResolvedInput> ResolveAsync(SpecialFlightRequest request, Guid? flightId, CancellationToken ct)
    {
        if (!Enum.TryParse<SpecialFlightType>(request.Type, out var type))
            throw new AppException(AppErrorKind.Validation, "Invalid special flight type.");
        if (type == SpecialFlightType.CheckFlight)
            throw new AppException(AppErrorKind.Validation, "Check flights are created automatically after C and D checks.");

        var stops = (request.Stops ?? []).Select(s => (s ?? "").Trim().ToUpperInvariant()).ToList();
        if (stops.Count is < 2 or > SpecialFlight.MaxLegs + 1 || stops.Any(s => s.Length != 4))
            throw new AppException(AppErrorKind.Validation, $"Enter the departure and up to {SpecialFlight.MaxLegs} destinations.");

        var aircraft = await db.Aircraft
            .Include(a => a.AircraftType)
            .Include(a => a.CurrentAirport)
            .FirstOrDefaultAsync(a => a.Id == request.AircraftId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Aircraft not found.");

        if (aircraft.Status == AircraftStatus.Inactive)
            throw new AppException(AppErrorKind.Validation, $"Aircraft {aircraft.Registration} is inactive.");

        var busy = await db.SpecialFlights.AnyAsync(
            f => f.AircraftId == aircraft.Id && f.Id != flightId && ActiveStatuses.Contains(f.Status), ct);
        if (busy)
            throw new AppException(AppErrorKind.Conflict, $"Aircraft {aircraft.Registration} is already in another active special flight.");

        if (type == SpecialFlightType.Redelivery && aircraft.AircraftType.DeliveryAirportIcao is { } delivery && stops[0] != delivery)
            throw new AppException(AppErrorKind.Validation, $"A {aircraft.AircraftType.IcaoCode} redelivery must depart from {delivery}.");

        var lastStop = stops[^1];
        if (type == SpecialFlightType.MaintenanceFerry
            && lastStop != Airline.MaintenanceHubIcao
            && !await db.Hubs.AnyAsync(h => h.Airport.Icao == lastStop, ct))
            throw new AppException(AppErrorKind.Validation, $"A maintenance ferry must end at a base or at {Airline.MaintenanceHubIcao}.");

        var airports = await db.Airports.Where(a => stops.Contains(a.Icao)).ToListAsync(ct);
        var missing = stops.Except(airports.Select(a => a.Icao)).ToList();
        if (missing.Count > 0)
            throw new AppException(AppErrorKind.Validation, $"Airport not registered: {string.Join(", ", missing.Distinct())}.");

        var rank = await db.Ranks.AsNoTracking().FirstOrDefaultAsync(r => r.Code == (request.MinimumRank ?? "").Trim().ToUpper(), ct)
            ?? throw new AppException(AppErrorKind.Validation, "Select the minimum rank.");

        var byIcao = airports.ToDictionary(a => a.Icao);
        return new ResolvedInput(
            type,
            aircraft,
            rank.Level,
            Utc(request.StartsAt),
            Utc(request.EndsAt),
            stops.Select(s => byIcao[s]).ToList());
    }

    private async Task<List<Route>> CreateRoutesAsync(List<Airport> stops, AircraftType type, IEnumerable<int> releasedNumbers, CancellationToken ct)
    {
        var released = releasedNumbers.ToHashSet();
        var used = (await db.Routes
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

    private async Task<SpecialFlight> FindAsync(Guid id, CancellationToken ct) =>
        await support.Query().FirstOrDefaultAsync(f => f.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Special flight not found.");

    private async Task<SpecialFlightDto> ToDtoAsync(Guid id, CancellationToken ct)
    {
        var flight = await support.Query().AsNoTracking().FirstAsync(f => f.Id == id, ct);
        return SpecialFlightSupport.ToDto(flight, await support.RanksByLevelAsync(ct), DateTime.UtcNow);
    }

    private static DateTime Utc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value, DateTimeKind.Utc);
}