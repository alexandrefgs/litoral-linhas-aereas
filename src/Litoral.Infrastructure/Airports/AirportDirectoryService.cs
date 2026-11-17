using Litoral.Application.Airports;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Bookings;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Airports;

public sealed class AirportDirectoryService(LitoralDbContext db, BookingSupport support) : IAirportDirectoryService
{
    public const int JumpseatPointsPerNm = 3;
    private const int PilotsPreview = 4;

    public async Task<AirportDirectoryDto> ListAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var bases = await support.GetBaseIcaosAsync(ct);
        var level = pilot.Rank.Level;

        var ranks = await db.Ranks.AsNoTracking().OrderBy(r => r.Level).ToListAsync(ct);
        var here = await db.Airports.AsNoTracking().FirstAsync(a => a.Icao == pilot.CurrentLocationIcao, ct);

        var airports = await db.Airports.AsNoTracking()
            .Where(a => a.MinimumRankLevel <= level)
            .Select(a => new
            {
                a.Icao,
                a.IataCode,
                a.Name,
                a.City,
                a.Country,
                a.Latitude,
                a.Longitude,
                a.PhotoUrl,
                a.PhotoCredit,
                a.Operations,
                a.MinimumRankLevel,
                OnGround = db.Aircraft.Count(x => x.CurrentAirportId == a.Id && x.Status != AircraftStatus.InFlight && x.Status != AircraftStatus.Inactive),
                Departures = db.Routes.Count(r => r.DepartureAirportId == a.Id && r.IsActive && !r.IsSpecial && r.ArrivalAirport.MinimumRankLevel <= level)
            })
            .ToListAsync(ct);

        var pilots = (await db.Pilots.AsNoTracking().Include(p => p.Rank).Where(p => p.IsActive).ToListAsync(ct))
            .GroupBy(p => p.CurrentLocationIcao)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Rank.Level).ThenBy(p => p.PilotNumber).ToList());

        var items = airports
            .OrderByDescending(a => a.Icao == pilot.CurrentLocationIcao)
            .ThenByDescending(a => bases.Contains(a.Icao))
            .ThenBy(a => a.Icao)
            .Select(a =>
            {
                var pilotsHere = pilots.GetValueOrDefault(a.Icao) ?? [];
                return new AirportListItemDto(
                    a.Icao,
                    a.IataCode,
                    a.Name,
                    a.City,
                    a.Country,
                    a.Latitude,
                    a.Longitude,
                    a.PhotoUrl,
                    a.PhotoCredit,
                    bases.Contains(a.Icao),
                    (a.Operations & OperationType.Passenger) == OperationType.Passenger,
                    (a.Operations & OperationType.Cargo) == OperationType.Cargo,
                    a.MinimumRankLevel,
                    ranks.FirstOrDefault(r => r.Level == a.MinimumRankLevel)?.Name ?? $"Level {a.MinimumRankLevel}",
                    ranks.FirstOrDefault(r => r.Level == a.MinimumRankLevel)?.Code ?? ranks[0].Code,
                    a.OnGround,
                    a.Departures,
                    bases.Contains(a.Icao) ? 0 : CostTo(here.Latitude, here.Longitude, a.Latitude, a.Longitude),
                    pilotsHere.Count,
                    pilotsHere.Take(PilotsPreview)
                        .Select(p => new AirportPilotDto(p.PilotNumber, p.DisplayId, p.PublicName, p.FirstName, p.LastName, p.PhotoUrl))
                        .ToList());
            })
            .ToList();

        return new AirportDirectoryDto(
            pilot.CurrentLocationIcao,
            pilot.Points,
            pilot.Rank.Name,
            await BlockedReasonAsync(pilot, ct),
            items);
    }

    public async Task<IReadOnlyList<AirportAircraftDto>> AircraftAsync(string icao, CancellationToken ct)
    {
        var code = icao.Trim().ToUpperInvariant();

        return await db.Aircraft.AsNoTracking()
            .Where(a => a.CurrentAirport.Icao == code && a.Status != AircraftStatus.InFlight && a.Status != AircraftStatus.Inactive)
            .OrderBy(a => a.AircraftType.Name).ThenBy(a => a.Registration)
            .Select(a => new AirportAircraftDto(
                a.Registration,
                a.AircraftType.IcaoCode,
                a.AircraftType.Name,
                a.PhotoUrl,
                a.PhotoCredit,
                a.Status.ToString(),
                db.Pireps.Where(p => p.Registration == a.Registration).OrderByDescending(p => p.BlockOnAt).Select(p => p.Callsign).FirstOrDefault(),
                a.LastFlightAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AirportDepartureDto>> DeparturesAsync(string userId, string icao, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var code = icao.Trim().ToUpperInvariant();
        var level = pilot.Rank.Level;

        var routes = await support.RouteQuery().AsNoTracking()
            .Where(r => r.DepartureAirport.Icao == code && r.IsActive && r.ArrivalAirport.MinimumRankLevel <= level)
            .OrderBy(r => r.FlightNumber)
            .ToListAsync(ct);

        var availableTypes = (await db.Aircraft.AsNoTracking()
            .Where(a => a.CurrentAirport.Icao == code && a.Status == AircraftStatus.Available && !support.HeldAircraftIds().Contains(a.Id))
            .Select(a => a.AircraftTypeId)
            .ToListAsync(ct))
            .ToHashSet();

        return routes
            .Select(r => new AirportDepartureDto(
                r.Id,
                r.Callsign,
                r.ArrivalAirport.Icao,
                r.ArrivalAirport.IataCode,
                r.ArrivalAirport.Name,
                r.ArrivalAirport.City,
                r.ArrivalAirport.Country,
                r.IsCargo,
                r.DistanceNm,
                r.EstimatedMinutes,
                r.AircraftTypes.Select(t => t.IcaoCode).Distinct().OrderBy(t => t).ToList(),
                r.AircraftTypes.Any(t => availableTypes.Contains(t.Id))))
            .ToList();
    }

    public async Task<JumpseatResultDto> JumpseatAsync(string userId, string icao, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        await support.EnsureNoOpenBookingAsync(pilot.Id, ct);

        var code = icao.Trim().ToUpperInvariant();
        var destination = await db.Airports.FirstOrDefaultAsync(a => a.Icao == code, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Airport not found.");

        var origin = await db.Airports.AsNoTracking().FirstAsync(a => a.Icao == pilot.CurrentLocationIcao, ct);
        var bases = await support.GetBaseIcaosAsync(ct);
        var cost = bases.Contains(destination.Icao) ? 0 : CostTo(origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude);
        var from = pilot.CurrentLocationIcao;

        pilot.JumpseatTo(destination);
        if (cost > 0)
            pilot.Spend(cost, PointTransactionType.Jumpseat, $"Jumpseat {from} → {destination.Icao}");

        await db.SaveChangesAsync(ct);
        return new JumpseatResultDto(from, destination.Icao, cost, pilot.Points);
    }

    private static int CostTo(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude) =>
        Airport.DistanceNm(fromLatitude, fromLongitude, toLatitude, toLongitude) * JumpseatPointsPerNm;
    private async Task<string?> BlockedReasonAsync(Pilot pilot, CancellationToken ct)
    {
        if (!pilot.IsActive)
            return "Your account is inactive. Contact the Head of HR.";
        if (!pilot.CanJumpseat)
            return Pilot.JumpseatUsedMessage;

        var open = await support.FindOpenBookingAsync(pilot.Id, ct);
        return open is null ? null : $"You have an open flight ({open.Callsign}). Cancel it or complete it first.";
    }
}