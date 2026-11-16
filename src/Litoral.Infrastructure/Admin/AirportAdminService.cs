using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Admin;

public sealed class AirportAdminService(LitoralDbContext db) : IAirportAdminService
{
    public async Task<IReadOnlyList<AdminAirportDto>> ListAsync(CancellationToken ct)
    {
        var rankCodes = await LoadRankCodesAsync(ct);
        var hubs = await db.Hubs.AsNoTracking().ToDictionaryAsync(h => h.AirportId, h => h.Operations, ct);
        var airports = await db.Airports.AsNoTracking().OrderBy(a => a.Icao).ToListAsync(ct);

        return airports
            .Select(a => ToDto(a, hubs.GetValueOrDefault(a.Id), rankCodes))
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
            throw new AppException(AppErrorKind.Conflict, $"O aeroporto {icao} já está cadastrado.");

        var level = await ResolveRankLevelAsync(request.MinimumRank, ct);
        var airport = new Airport(icao, request.Name, request.City, request.Country, request.Latitude, request.Longitude, level);

        db.Airports.Add(airport);
        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(airport, ct);
    }

    public async Task<AdminAirportDto> UpdateAsync(string icao, UpdateAirportRequest request, CancellationToken ct)
    {
        var airport = await FindAsync(icao, ct);
        var level = await ResolveRankLevelAsync(request.MinimumRank, ct);

        if (level > 1 && await db.Hubs.AnyAsync(h => h.AirportId == airport.Id, ct))
            throw new AppException(AppErrorKind.Validation, "Hubs precisam ser acessíveis a todos os pilotos (patente mínima FO).");

        airport.Update(request.Name, request.City, request.Country, request.Latitude, request.Longitude);

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
        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(airport, ct);
    }

    public async Task<AdminAirportDto> SetHubAsync(string icao, SetHubRequest request, CancellationToken ct)
    {
        var airport = await FindAsync(icao, ct);
        var hub = await db.Hubs.FirstOrDefaultAsync(h => h.AirportId == airport.Id, ct);

        OperationType operations = 0;
        if (request.Passenger) operations |= OperationType.Passenger;
        if (request.Cargo) operations |= OperationType.Cargo;

        var losesPassenger = hub is not null && hub.Supports(OperationType.Passenger) && !request.Passenger;
        if (losesPassenger)
        {
            var basedPilots = await db.Pilots.CountAsync(p => p.BaseIcao == airport.Icao, ct);
            if (basedPilots > 0)
                throw new AppException(AppErrorKind.Conflict,
                    $"{basedPilots} piloto(s) têm {airport.Icao} como base. Não é possível remover a operação de passageiros.");
        }

        if (operations == 0)
        {
            if (hub is not null)
                db.Hubs.Remove(hub);
        }
        else if (hub is null)
        {
            db.Hubs.Add(new Hub(airport, operations));
        }
        else
        {
            hub.SetOperations(operations);
        }

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(airport, ct);
    }

    public async Task DeleteAsync(string icao, CancellationToken ct)
    {
        var airport = await FindAsync(icao, ct);

        if (await db.Hubs.AnyAsync(h => h.AirportId == airport.Id, ct))
            throw new AppException(AppErrorKind.Conflict, "Remova o hub antes de excluir o aeroporto.");
        if (await db.Routes.AnyAsync(r => r.DepartureAirportId == airport.Id || r.ArrivalAirportId == airport.Id, ct))
            throw new AppException(AppErrorKind.Conflict, "Existem rotas usando este aeroporto.");
        if (await db.Aircraft.AnyAsync(a => a.CurrentAirportId == airport.Id, ct))
            throw new AppException(AppErrorKind.Conflict, "Existem aeronaves neste aeroporto.");
        if (await db.Pilots.AnyAsync(p => p.CurrentLocationIcao == airport.Icao || p.BaseIcao == airport.Icao, ct))
            throw new AppException(AppErrorKind.Conflict, "Existem pilotos neste aeroporto.");

        db.Airports.Remove(airport);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Airport> FindAsync(string icao, CancellationToken ct)
    {
        var code = Normalize(icao);
        return await db.Airports.FirstOrDefaultAsync(a => a.Icao == code, ct)
            ?? throw new AppException(AppErrorKind.NotFound, $"Aeroporto {code} não encontrado.");
    }

    private async Task<int> ResolveRankLevelAsync(string code, CancellationToken ct)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        var rank = await db.Ranks.AsNoTracking().FirstOrDefaultAsync(r => r.Code == normalized, ct)
            ?? throw new AppException(AppErrorKind.Validation, $"Patente '{code}' não existe.");

        return rank.Level;
    }

    private async Task<Dictionary<int, string>> LoadRankCodesAsync(CancellationToken ct) =>
        await db.Ranks.AsNoTracking().ToDictionaryAsync(r => r.Level, r => r.Code, ct);

    private async Task<AdminAirportDto> ToDtoAsync(Airport airport, CancellationToken ct)
    {
        var rankCodes = await LoadRankCodesAsync(ct);
        var operations = await db.Hubs
            .Where(h => h.AirportId == airport.Id)
            .Select(h => (OperationType?)h.Operations)
            .FirstOrDefaultAsync(ct);

        return ToDto(airport, operations ?? default, rankCodes);
    }

    private static AdminAirportDto ToDto(Airport airport, OperationType operations, Dictionary<int, string> rankCodes) => new(
        airport.Icao,
        airport.Name,
        airport.City,
        airport.Country,
        airport.Latitude,
        airport.Longitude,
        rankCodes.GetValueOrDefault(airport.MinimumRankLevel, airport.MinimumRankLevel.ToString()),
        operations.HasFlag(OperationType.Passenger),
        operations.HasFlag(OperationType.Cargo));

    private static string Normalize(string icao) => (icao ?? string.Empty).Trim().ToUpperInvariant();
}