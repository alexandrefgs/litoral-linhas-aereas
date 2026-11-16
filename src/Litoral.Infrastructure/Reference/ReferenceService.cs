using Litoral.Application.Reference;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Reference;

public sealed class ReferenceService(LitoralDbContext db) : IReferenceService
{
    public async Task<IReadOnlyList<HubDto>> GetHubsAsync(CancellationToken ct) =>
        await db.Hubs
            .AsNoTracking()
            .OrderBy(h => h.Airport.Icao)
            .Select(h => new HubDto(
                h.Airport.Icao,
                h.Airport.Name,
                h.Airport.City,
                (h.Airport.Operations & OperationType.Passenger) == OperationType.Passenger,
                (h.Airport.Operations & OperationType.Cargo) == OperationType.Cargo))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RankDto>> GetRanksAsync(CancellationToken ct) =>
        await db.Ranks
            .AsNoTracking()
            .OrderBy(r => r.Level)
            .Select(r => new RankDto(r.Code, r.Name, r.Level, r.MinFlightHours, r.MinPoints))
            .ToListAsync(ct);
        public async Task<AirportDto?> GetAirportAsync(string icao, CancellationToken ct)
    {
        var code = icao.Trim().ToUpperInvariant();

        return await db.Airports
            .AsNoTracking()
            .Where(a => a.Icao == code)
            .Select(a => new AirportDto(
                a.Icao,
                a.Name,
                a.City,
                a.Country,
                a.Latitude,
                a.Longitude,
                db.Hubs.Any(h => h.AirportId == a.Id),
                db.Aircraft.Count(x => x.CurrentAirportId == a.Id && x.Status != AircraftStatus.InFlight && x.Status != AircraftStatus.Inactive),
                db.Routes.Count(r => r.DepartureAirportId == a.Id && r.IsActive)))
            .FirstOrDefaultAsync(ct);
    }
        public async Task<PublicStatsDto> GetPublicStatsAsync(CancellationToken ct) => new(
        await db.Pilots.CountAsync(p => p.IsActive, ct),
        await db.Bookings.CountAsync(b => b.Status == BookingStatus.Completed, ct),
        await db.AircraftTypes.CountAsync(ct),
        await db.Airports.CountAsync(ct),
        await db.Hubs.CountAsync(ct));
}