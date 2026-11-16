using Litoral.Application.Bookings;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Bookings;

public sealed class PilotFleetService(LitoralDbContext db, BookingSupport support) : IPilotFleetService
{
    public const int TransferBaseCost = 1000;
    public const int TransferCostPer100Nm = 100;

    public async Task<IReadOnlyList<FleetAircraftDto>> ListAsync(CancellationToken ct)
    {
        var aircraft = await db.Aircraft
            .AsNoTracking()
            .Include(a => a.AircraftType)
            .Include(a => a.CurrentAirport)
            .Where(a => a.Status != AircraftStatus.Inactive)
            .OrderBy(a => a.Registration)
            .ToListAsync(ct);

        return aircraft.Select(ToDto).ToList();
    }

    public async Task<TransferQuoteDto> QuoteTransferAsync(string userId, Guid aircraftId, CancellationToken ct)
    {
        var (pilot, aircraft, destination) = await LoadAsync(userId, aircraftId, ct);
        return await QuoteAsync(pilot, aircraft, destination, ct);
    }

    public async Task<FleetAircraftDto> TransferAsync(string userId, Guid aircraftId, CancellationToken ct)
    {
        var (pilot, aircraft, destination) = await LoadAsync(userId, aircraftId, ct);
        var quote = await QuoteAsync(pilot, aircraft, destination, ct);

        if (quote.BlockedReason is not null)
            throw new AppException(AppErrorKind.Conflict, quote.BlockedReason);

        var origin = aircraft.CurrentAirport.Icao;
        aircraft.Relocate(destination);

        if (quote.Cost > 0)
            pilot.Spend(quote.Cost, PointTransactionType.AircraftTransfer, $"Transfer of {aircraft.Registration} from {origin} to {destination.Icao}");

        await db.SaveChangesAsync(ct);
        return ToDto(aircraft);
    }

    private async Task<(Pilot Pilot, Aircraft Aircraft, Airport Destination)> LoadAsync(string userId, Guid aircraftId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);

        var aircraft = await db.Aircraft
            .Include(a => a.AircraftType)
            .Include(a => a.CurrentAirport)
            .FirstOrDefaultAsync(a => a.Id == aircraftId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Aircraft not found.");

        var destination = await db.Airports.FirstOrDefaultAsync(a => a.Icao == pilot.CurrentLocationIcao, ct)
            ?? throw new AppException(AppErrorKind.NotFound, $"Airport {pilot.CurrentLocationIcao} not found.");

        return (pilot, aircraft, destination);
    }

    private async Task<TransferQuoteDto> QuoteAsync(Pilot pilot, Aircraft aircraft, Airport destination, CancellationToken ct)
    {
        var distance = aircraft.CurrentAirport.DistanceNmTo(destination);

        var hasAircraftHere = await db.Aircraft.AnyAsync(
            a => a.CurrentAirportId == destination.Id && a.Status == AircraftStatus.Available, ct);

        var cost = hasAircraftHere ? TransferBaseCost + distance / 100 * TransferCostPer100Nm : 0;

        var blocked =
            aircraft.CurrentAirportId == destination.Id ? $"{aircraft.Registration} is already at {destination.Icao}."
            : aircraft.Status != AircraftStatus.Available ? $"{aircraft.Registration} is not available ({aircraft.Status})."
            : !destination.Supports(aircraft.AircraftType.Operation) ? $"{destination.Icao} doesn't have {(aircraft.AircraftType.IsCargo ? "cargo" : "passenger")} operations."
            : pilot.Points < cost ? $"You need {cost} points and you have {pilot.Points}."
            : null;

        return new TransferQuoteDto(
            aircraft.Id,
            aircraft.Registration,
            aircraft.CurrentAirport.Icao,
            destination.Icao,
            distance,
            cost,
            cost == 0,
            pilot.Points,
            blocked);
    }

    private static FleetAircraftDto ToDto(Aircraft aircraft) =>
        new(
            aircraft.Id,
            aircraft.Registration,
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
            aircraft.LastFlightAt);
}