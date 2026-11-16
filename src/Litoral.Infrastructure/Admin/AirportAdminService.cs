using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Admin;

public sealed class AirportAdminService(LitoralDbContext db, IPhotoStorage photos) : IAirportAdminService
{
    private const string PhotoFolder = "airports";
    public async Task<IReadOnlyList<AdminAirportDto>> ListAsync(CancellationToken ct)
    {
        var rankCodes = await LoadRankCodesAsync(ct);
        var hubAirportIds = (await db.Hubs.AsNoTracking().Select(h => h.AirportId).ToListAsync(ct)).ToHashSet();
        var airports = await db.Airports.AsNoTracking().OrderBy(a => a.Icao).ToListAsync(ct);

        return airports
            .Select(a => ToDto(a, hubAirportIds.Contains(a.Id), rankCodes))
            .ToList();
    }

    public async Task<AdminAirportDto> GetAsync(string icao, CancellationToken ct)
    {
        var airport = await FindAsync(icao, ct);
        return await ToDtoAsync(airport, ct);
    }

    public async Task<AdminAirportDto> CreateAsync(CreateAirportRequest request, CancellationToken ct)
    {
        var icao = Normalize(request.Icao);

        if (await db.Airports.AnyAsync(a => a.Icao == icao, ct))
            throw new AppException(AppErrorKind.Conflict, $"Airport {icao} is already registered.");

        var level = await ResolveRankLevelAsync(request.MinimumRank, ct);
        var airport = new Airport(
            icao,
            request.Name,
            request.City,
            request.Country,
            request.Latitude,
            request.Longitude,
            ToOperations(request.Passenger, request.Cargo),
            level);
        airport.SetIataCode(request.Iata);
        db.Airports.Add(airport);

        if (request.IsHub)
            db.Hubs.Add(new Hub(airport));

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(airport, ct);
    }

    public async Task<AdminAirportDto> UpdateAsync(string icao, UpdateAirportRequest request, CancellationToken ct)
    {
        var airport = await FindAsync(icao, ct);
        var hub = await db.Hubs.FirstOrDefaultAsync(h => h.AirportId == airport.Id, ct);
        var level = await ResolveRankLevelAsync(request.MinimumRank, ct);
        var operations = ToOperations(request.Passenger, request.Cargo);

        if (request.IsHub && level > 1)
            throw new AppException(AppErrorKind.Validation, "Hubs must be accessible to all pilots (minimum rank FO).");

        var wasBase = hub is not null && airport.Supports(OperationType.Passenger);
        var staysBase = request.IsHub && request.Passenger;
        if (wasBase && !staysBase)
        {
            var basedPilots = await db.Pilots.CountAsync(p => p.BaseIcao == airport.Icao, ct);
            if (basedPilots > 0)
                throw new AppException(AppErrorKind.Conflict,
                    $"{basedPilots} pilot(s) have {airport.Icao} as their base. The airport must remain a hub with passenger operations.");
        }

        await EnsureNoRoutesUsingAsync(airport, OperationType.Passenger, request.Passenger, ct);
        await EnsureNoRoutesUsingAsync(airport, OperationType.Cargo, request.Cargo, ct);

        airport.Update(request.Name, request.City, request.Country, request.Latitude, request.Longitude);
        airport.SetIataCode(request.Iata);
        airport.SetOperations(operations);

        if (hub is not null && !request.IsHub)
            db.Hubs.Remove(hub);

        if (level > airport.MinimumRankLevel)
        {
            var pilots = await db.Pilots
                .Include(p => p.Rank)
                .Where(p => p.CurrentLocationIcao == airport.Icao && p.Rank.Level < level)
                .ToListAsync(ct);

            foreach (var pilot in pilots)
                pilot.MoveTo(pilot.BaseIcao);
        }

        airport.SetMinimumRankLevel(level);

        if (hub is null && request.IsHub)
            db.Hubs.Add(new Hub(airport));

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(airport, ct);
    }

    public async Task DeleteAsync(string icao, CancellationToken ct)
    {
        var airport = await FindAsync(icao, ct);

        if (await db.Hubs.AnyAsync(h => h.AirportId == airport.Id, ct))
            throw new AppException(AppErrorKind.Conflict, "Remove the hub before deleting the airport.");
        if (await db.Routes.AnyAsync(r => r.DepartureAirportId == airport.Id || r.ArrivalAirportId == airport.Id, ct))
            throw new AppException(AppErrorKind.Conflict, "There are routes using this airport.");
        if (await db.Aircraft.AnyAsync(a => a.CurrentAirportId == airport.Id, ct))
            throw new AppException(AppErrorKind.Conflict, "There are aircraft at this airport.");
        if (await db.Pilots.AnyAsync(p => p.CurrentLocationIcao == airport.Icao || p.BaseIcao == airport.Icao, ct))
            throw new AppException(AppErrorKind.Conflict, "There are pilots at this airport.");

                var photo = airport.PhotoUrl;
        db.Airports.Remove(airport);
        await db.SaveChangesAsync(ct);
        photos.Delete(photo);
    }

    public async Task<AdminAirportDto> UploadPhotoAsync(string icao, PhotoUpload upload, CancellationToken ct)
    {
        var airport = await FindAsync(icao, ct);
        var url = await photos.SaveAsync(PhotoFolder, upload, ct);
        var previous = airport.SetPhoto(url, upload.Credit);
        await db.SaveChangesAsync(ct);
        photos.Delete(previous);
        return await ToDtoAsync(airport, ct);
    }

    public async Task<AdminAirportDto> UpdatePhotoCreditAsync(string icao, string? credit, CancellationToken ct)
    {
        var airport = await FindAsync(icao, ct);
        airport.SetPhotoCredit(credit);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(airport, ct);
    }

    public async Task<AdminAirportDto> RemovePhotoAsync(string icao, CancellationToken ct)
    {
        var airport = await FindAsync(icao, ct);
        var previous = airport.SetPhoto(null, null);
        await db.SaveChangesAsync(ct);
        photos.Delete(previous);
        return await ToDtoAsync(airport, ct);
    }

    private async Task EnsureNoRoutesUsingAsync(Airport airport, OperationType operation, bool keeps, CancellationToken ct)
    {
        if (keeps || !airport.Supports(operation)) return;

        var isCargo = operation == OperationType.Cargo;
        var inUse = await db.Routes.AnyAsync(
            r => (r.DepartureAirportId == airport.Id || r.ArrivalAirportId == airport.Id) && r.IsCargo == isCargo, ct);

        if (inUse)
            throw new AppException(AppErrorKind.Conflict,
                $"There are {(isCargo ? "cargo" : "passenger")} routes using {airport.Icao}. Remove them before removing this operation.");
    }

    private async Task<Airport> FindAsync(string icao, CancellationToken ct)
    {
        var code = Normalize(icao);
        return await db.Airports.FirstOrDefaultAsync(a => a.Icao == code, ct)
            ?? throw new AppException(AppErrorKind.NotFound, $"Airport {code} not found.");
    }

    private async Task<int> ResolveRankLevelAsync(string code, CancellationToken ct)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        var rank = await db.Ranks.AsNoTracking().FirstOrDefaultAsync(r => r.Code == normalized, ct)
            ?? throw new AppException(AppErrorKind.Validation, $"Rank '{code}' does not exist.");

        return rank.Level;
    }

    private async Task<Dictionary<int, string>> LoadRankCodesAsync(CancellationToken ct) =>
        await db.Ranks.AsNoTracking().ToDictionaryAsync(r => r.Level, r => r.Code, ct);

    private async Task<AdminAirportDto> ToDtoAsync(Airport airport, CancellationToken ct)
    {
        var rankCodes = await LoadRankCodesAsync(ct);
        var isHub = await db.Hubs.AnyAsync(h => h.AirportId == airport.Id, ct);

        return ToDto(airport, isHub, rankCodes);
    }

    private static AdminAirportDto ToDto(Airport airport, bool isHub, Dictionary<int, string> rankCodes) => new(
        airport.Icao,
        airport.IataCode,
        airport.Name,
        airport.City,
        airport.Country,
        airport.Latitude,
        airport.Longitude,
        rankCodes.GetValueOrDefault(airport.MinimumRankLevel, airport.MinimumRankLevel.ToString()),
        airport.Supports(OperationType.Passenger),
        airport.Supports(OperationType.Cargo),
        isHub,
        airport.PhotoUrl,
        airport.PhotoCredit);

    private static OperationType ToOperations(bool passenger, bool cargo)
    {
        OperationType operations = 0;
        if (passenger) operations |= OperationType.Passenger;
        if (cargo) operations |= OperationType.Cargo;

        if (operations == 0)
            throw new AppException(AppErrorKind.Validation, "Select at least one operation (passenger or cargo).");

        return operations;
    }

    private static string Normalize(string icao) => (icao ?? string.Empty).Trim().ToUpperInvariant();
}