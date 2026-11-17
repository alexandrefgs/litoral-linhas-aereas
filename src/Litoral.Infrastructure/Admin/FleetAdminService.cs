using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Domain.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Litoral.Domain.Fleet;

namespace Litoral.Infrastructure.Admin;

public sealed class FleetAdminService(LitoralDbContext db, IPhotoStorage photos) : IFleetAdminService
{
    private const string PhotoFolder = "aircraft";

    public async Task<IReadOnlyList<AdminAircraftTypeDto>> ListTypesAsync(CancellationToken ct)
    {
        var types = await db.AircraftTypes.AsNoTracking().OrderBy(t => t.IsCargo).ThenBy(t => t.IcaoCode).ToListAsync(ct);
        var aircraftCounts = await db.Aircraft.GroupBy(a => a.AircraftTypeId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var routeCounts = await RouteCountsAsync(ct);

        return types.Select(t => ToDto(t, aircraftCounts.GetValueOrDefault(t.Id), routeCounts.GetValueOrDefault(t.Id))).ToList();
    }

    public async Task<AdminAircraftTypeDto> CreateTypeAsync(AircraftTypeRequest request, CancellationToken ct)
    {
        await EnsureTypeIsUniqueAsync(request.IcaoCode, request.IsCargo, null, ct);
        await EnsurePrefixIsFreeAsync(request.RegistrationPrefix, null, ct);

        var type = new AircraftType(request.IcaoCode, request.Name, request.IsCargo, request.MaxRangeNm, request.RegistrationPrefix, request.MaintenanceIntervalHours);
        type.SetDeliveryAirport(request.DeliveryAirportIcao);
        db.AircraftTypes.Add(type);
        await db.SaveChangesAsync(ct);

        return ToDto(type, 0, 0);
    }

    public async Task<AdminAircraftTypeDto> UpdateTypeAsync(Guid id, AircraftTypeRequest request, CancellationToken ct)
    {
        var type = await db.AircraftTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Aircraft type not found.");

        await EnsureTypeIsUniqueAsync(request.IcaoCode, request.IsCargo, id, ct);
        await EnsurePrefixIsFreeAsync(request.RegistrationPrefix, id, ct);

        var aircraftCount = await db.Aircraft.CountAsync(a => a.AircraftTypeId == id, ct);
        var routes = await db.Routes.AsNoTracking().Where(r => r.AircraftTypes.Any(t => t.Id == id)).Select(r => new { r.FlightNumber, r.DistanceNm }).ToListAsync(ct);

        if (request.IsCargo != type.IsCargo && (aircraftCount > 0 || routes.Count > 0))
            throw new AppException(AppErrorKind.Conflict, "This type has aircraft or routes. Its operation cannot be changed.");

        if (aircraftCount > 0 && !string.Equals(Normalize(request.RegistrationPrefix), type.RegistrationPrefix, StringComparison.Ordinal))
            throw new AppException(AppErrorKind.Conflict, "This type has aircraft in the fleet. Its registration prefix cannot be changed.");

        var longest = routes.MaxBy(r => r.DistanceNm);
        if (longest is not null && request.MaxRangeNm < longest.DistanceNm)
            throw new AppException(AppErrorKind.Conflict, $"Route {Airline.Icao}{longest.FlightNumber} is {longest.DistanceNm} NM long. The range cannot be lower than that.");

        type.Update(request.IcaoCode, request.Name, request.IsCargo, request.MaxRangeNm, request.RegistrationPrefix, request.MaintenanceIntervalHours);
        type.SetDeliveryAirport(request.DeliveryAirportIcao);
        await db.SaveChangesAsync(ct);

        return ToDto(type, aircraftCount, routes.Count);
    }

    public async Task DeleteTypeAsync(Guid id, CancellationToken ct)
    {
        var type = await db.AircraftTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Aircraft type not found.");

        if (await db.Aircraft.AnyAsync(a => a.AircraftTypeId == id, ct))
            throw new AppException(AppErrorKind.Conflict, "This type has aircraft in the fleet and cannot be deleted.");
        if (await db.Routes.AnyAsync(r => r.AircraftTypes.Any(t => t.Id == id), ct))
            throw new AppException(AppErrorKind.Conflict, "This type is used by routes and cannot be deleted.");

        db.AircraftTypes.Remove(type);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AdminAircraftDto>> ListAircraftAsync(CancellationToken ct)
    {
        var aircraft = await AircraftQuery().AsNoTracking().OrderBy(a => a.Registration).ToListAsync(ct);
        return aircraft.Select(ToDto).ToList();
    }

    public async Task<RegistrationSuggestionDto> SuggestRegistrationAsync(Guid aircraftTypeId, CancellationToken ct)
    {
        var type = await FindTypeAsync(aircraftTypeId, ct);

        if (type.RegistrationStart is not { } start)
            throw new AppException(AppErrorKind.Validation, $"Set the registration prefix of the {type.IcaoCode} type first.");

        var free = await FreeBlockRegistrationsAsync(start, ct);
        return new RegistrationSuggestionDto($"{start}_", free.FirstOrDefault(), free.Count);
    }

    public async Task<AdminAircraftDto> GetAircraftAsync(Guid id, CancellationToken ct) =>
        ToDto(await FindAircraftAsync(id, ct));

    public async Task<AdminAircraftDto> CreateAircraftAsync(AircraftRequest request, CancellationToken ct)
    {
        await EnsureRegistrationIsFreeAsync(request.Registration, null, ct);

        var type = await FindTypeAsync(request.AircraftTypeId, ct);
        var airport = await FindAirportAsync(request.CurrentAirportIcao, ct);
        await EnsureRegistrationFitsBlockAsync(request.Registration, type, null, ct);

        var aircraft = new Aircraft(request.Registration, type, airport, request.Variant);
        db.Aircraft.Add(aircraft);
        await db.SaveChangesAsync(ct);

        return ToDto(aircraft);
    }

    public async Task<AdminAircraftDto> UpdateAircraftAsync(Guid id, AircraftRequest request, CancellationToken ct)
    {
        var aircraft = await FindAircraftAsync(id, ct);
        await EnsureRegistrationIsFreeAsync(request.Registration, id, ct);

        var type = await FindTypeAsync(request.AircraftTypeId, ct);
        var airport = await FindAirportAsync(request.CurrentAirportIcao, ct);

        if (Normalize(request.Registration) != aircraft.Registration || type.Id != aircraft.AircraftTypeId)
            await EnsureRegistrationFitsBlockAsync(request.Registration, type, id, ct);

        aircraft.Update(request.Registration, type, airport, request.Variant);

        await db.SaveChangesAsync(ct);
        return ToDto(aircraft);
    }

    public async Task DeleteAircraftAsync(Guid id, CancellationToken ct)
    {
        var aircraft = await FindAircraftAsync(id, ct);

        if (await db.Bookings.AnyAsync(b => b.AircraftId == id, ct))
            throw new AppException(AppErrorKind.Conflict, "This aircraft has flight history. Deactivate it instead of deleting.");

        var photoUrl = aircraft.PhotoUrl;
        db.Aircraft.Remove(aircraft);
        await db.SaveChangesAsync(ct);
        photos.Delete(photoUrl);
    }

    public async Task<AdminAircraftDto> UploadPhotoAsync(Guid id, PhotoUpload upload, CancellationToken ct)
    {
        var aircraft = await FindAircraftAsync(id, ct);
        var previous = aircraft.PhotoUrl;

        var url = await photos.SaveAsync(PhotoFolder, upload, ct);
        aircraft.SetPhoto(url, upload.Credit);
        await db.SaveChangesAsync(ct);

        photos.Delete(previous);
        return ToDto(aircraft);
    }

    public async Task<AdminAircraftDto> UpdatePhotoCreditAsync(Guid id, string? credit, CancellationToken ct)
    {
        var aircraft = await FindAircraftAsync(id, ct);
        aircraft.SetPhoto(aircraft.PhotoUrl, credit);
        await db.SaveChangesAsync(ct);
        return ToDto(aircraft);
    }

    public async Task<AdminAircraftDto> RemovePhotoAsync(Guid id, CancellationToken ct)
    {
        var aircraft = await FindAircraftAsync(id, ct);
        var previous = aircraft.PhotoUrl;

        aircraft.SetPhoto(null, null);
        await db.SaveChangesAsync(ct);

        photos.Delete(previous);
        return ToDto(aircraft);
    }

    public async Task<AdminAircraftDto> SendToMaintenanceAsync(Guid id, MaintenanceRequest request, CancellationToken ct)
    {
        var aircraft = await FindAircraftAsync(id, ct);
        var until = request.Until is { } date ? DateTime.SpecifyKind(date.ToUniversalTime(), DateTimeKind.Utc) : (DateTime?)null;

        aircraft.SendToMaintenance(request.Reason, until);
        await db.SaveChangesAsync(ct);
        return ToDto(aircraft);
    }

    public async Task<AdminAircraftDto> ReleaseFromMaintenanceAsync(Guid id, CancellationToken ct)
    {
        var aircraft = await FindAircraftAsync(id, ct);
        aircraft.ReturnFromMaintenance();
        await db.SaveChangesAsync(ct);
        return ToDto(aircraft);
    }

    public async Task<AdminAircraftDto> SetActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var aircraft = await FindAircraftAsync(id, ct);
        if (active) aircraft.Activate();
        else aircraft.Deactivate();
        await db.SaveChangesAsync(ct);
        return ToDto(aircraft);
    }

    public async Task<IReadOnlyList<FleetAirportDto>> ListAirportsAsync(CancellationToken ct)
    {
        var airports = await db.Airports.AsNoTracking().OrderBy(a => a.Icao).ToListAsync(ct);
        return airports
            .Select(a => new FleetAirportDto(a.Icao, a.Name, a.City, a.Supports(OperationType.Passenger), a.Supports(OperationType.Cargo)))
            .ToList();
    }

    private IQueryable<Aircraft> AircraftQuery() =>
        db.Aircraft.Include(a => a.AircraftType).Include(a => a.CurrentAirport);

    private async Task<Aircraft> FindAircraftAsync(Guid id, CancellationToken ct) =>
        await AircraftQuery().FirstOrDefaultAsync(a => a.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Aircraft not found.");

    private async Task<AircraftType> FindTypeAsync(Guid id, CancellationToken ct) =>
        await db.AircraftTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Aircraft type not found.");

    private async Task<Airport> FindAirportAsync(string icao, CancellationToken ct)
    {
        var code = (icao ?? string.Empty).Trim().ToUpperInvariant();
        return await db.Airports.FirstOrDefaultAsync(a => a.Icao == code, ct)
            ?? throw new AppException(AppErrorKind.NotFound, $"Airport {code} is not registered.");
    }

    private async Task EnsureRegistrationIsFreeAsync(string registration, Guid? ignoreId, CancellationToken ct)
    {
        var normalized = (registration ?? string.Empty).Trim().ToUpperInvariant();
        if (await db.Aircraft.AnyAsync(a => a.Registration == normalized && a.Id != ignoreId, ct))
            throw new AppException(AppErrorKind.Conflict, $"Registration {normalized} is already in the fleet.");
    }

    private async Task EnsureTypeIsUniqueAsync(string icaoCode, bool isCargo, Guid? ignoreId, CancellationToken ct)
    {
        var code = (icaoCode ?? string.Empty).Trim().ToUpperInvariant();
        if (await db.AircraftTypes.AnyAsync(t => t.IcaoCode == code && t.IsCargo == isCargo && t.Id != ignoreId, ct))
            throw new AppException(AppErrorKind.Conflict, $"There is already a {(isCargo ? "cargo" : "passenger")} {code} type.");
    }

    private async Task EnsureRegistrationFitsBlockAsync(string registration, AircraftType type, Guid? ignoreId, CancellationToken ct)
    {
        var normalized = Normalize(registration);

        var owner = await db.AircraftTypes
            .AsNoTracking()
            .Where(t => t.Id != type.Id && t.RegistrationPrefix != null)
            .ToListAsync(ct);

        var reserved = owner.FirstOrDefault(t => normalized.StartsWith(t.RegistrationStart!, StringComparison.Ordinal));
        if (reserved is not null)
            throw new AppException(AppErrorKind.Conflict, $"{reserved.RegistrationStart}_ is reserved for the {reserved.Name}.");

        if (type.RegistrationStart is not { } start || normalized.StartsWith(start, StringComparison.Ordinal))
            return;

        var free = await FreeBlockRegistrationsAsync(start, ct, ignoreId);
        if (free.Count > 0)
            throw new AppException(AppErrorKind.Validation, $"The {start}_ block still has free registrations. Use {free[0]}.");
    }

    private async Task<List<string>> FreeBlockRegistrationsAsync(string start, CancellationToken ct, Guid? ignoreId = null)
    {
        var used = (await db.Aircraft
                .Where(a => a.Registration.StartsWith(start) && a.Id != ignoreId)
                .Select(a => a.Registration)
                .ToListAsync(ct))
            .ToHashSet();

        return Enumerable.Range('A', 26)
            .Select(c => $"{start}{(char)c}")
            .Where(r => RegistrationMarks.IsAllowed(r[3..]) && !used.Contains(r))
            .ToList();
    }

    private async Task EnsurePrefixIsFreeAsync(string prefix, Guid? ignoreId, CancellationToken ct)
    {
        var code = Normalize(prefix);
        if (await db.AircraftTypes.AnyAsync(t => t.RegistrationPrefix == code && t.Id != ignoreId, ct))
            throw new AppException(AppErrorKind.Conflict, $"Prefix {AircraftType.RegistrationNationality}{code} is already used by another type.");
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    private async Task<Dictionary<Guid, int>> RouteCountsAsync(CancellationToken ct)
    {
        var pairs = await db.Routes.SelectMany(r => r.AircraftTypes.Select(t => t.Id)).ToListAsync(ct);
        return pairs.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
    }

    private static AdminAircraftTypeDto ToDto(AircraftType type, int aircraftCount, int routeCount) =>
        new(type.Id, type.IcaoCode, type.Name, type.IsCargo, type.MaxRangeNm, type.MaintenanceIntervalHours, type.RegistrationPrefix, type.DeliveryAirportIcao, aircraftCount, routeCount);

    private static AdminAircraftDto ToDto(Aircraft aircraft) =>
        new(
            aircraft.Id,
            aircraft.Registration,
            aircraft.AircraftTypeId,
            aircraft.AircraftType.IcaoCode,
            aircraft.AircraftType.Name,
            aircraft.AircraftType.IsCargo,
            aircraft.Variant,
            aircraft.PhotoUrl,
            aircraft.PhotoCredit,
            aircraft.CurrentAirport.Icao,
            aircraft.CurrentAirport.City,
            aircraft.Status.ToString(),
            aircraft.TotalMinutes,
            aircraft.Cycles,
            aircraft.MinutesSinceMaintenance,
            aircraft.AircraftType.MaintenanceIntervalHours,
            aircraft.IsMaintenanceDue,
            aircraft.MaintenanceReason,
            aircraft.MaintenanceStartedAt,
            aircraft.MaintenanceUntil,
            aircraft.LastMaintenanceAt,
            aircraft.LastFlightAt);
}