using Litoral.Application.Common;
using Litoral.Domain.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Operations;

public sealed class MaintenanceFerryPlanner(LitoralDbContext db, SpecialFlightSupport support)
{
    public static readonly TimeSpan FerryWindow = TimeSpan.FromDays(3);
    public static readonly TimeSpan MaintenanceDuration = TimeSpan.FromDays(2);
    public async Task<int> PlanDueAsync(CancellationToken ct)
    {
        var held = db.SpecialFlights
            .Where(f => f.Status == SpecialFlightStatus.Open || f.Status == SpecialFlightStatus.Taken)
            .Select(f => f.AircraftId);

        var due = await db.Aircraft
            .Include(a => a.AircraftType)
            .Where(a => a.Status == AircraftStatus.Available
                && a.MinutesSinceMaintenance >= a.AircraftType.MaintenanceIntervalHours * 60
                && !held.Contains(a.Id))
            .ToListAsync(ct);

        foreach (var aircraft in due)
        {
            await AfterFlightAsync(aircraft, null, ct);
            await db.SaveChangesAsync(ct);
        }

        return due.Count;
    }

    public async Task AfterFlightAsync(Aircraft aircraft, SpecialFlight? special, CancellationToken ct)
    {
        if (!aircraft.IsAvailable) return;

        var here = await LocationAsync(aircraft, ct);
        var atHub = here.Icao == Airline.MaintenanceHubIcao;

        if (special is { Type: SpecialFlightType.MaintenanceFerry, Status: SpecialFlightStatus.Completed } && atHub)
        {
            StartMaintenance(aircraft, $"Scheduled maintenance at {Airline.MaintenanceHubIcao}");
            return;
        }

        if (!aircraft.IsMaintenanceDue || special?.Status == SpecialFlightStatus.Taken) return;

        if (atHub)
        {
            StartMaintenance(aircraft, $"Scheduled maintenance at {Airline.MaintenanceHubIcao} ({aircraft.AircraftType.MaintenanceIntervalHours}h interval)");
            return;
        }

        if (await HasActiveSpecialFlightAsync(aircraft.Id, special?.Id, ct)) return;

        await PlanFerryAsync(aircraft, here, ct);
    }

    public async Task AfterSpecialClosedAsync(SpecialFlight flight, Aircraft aircraft, CancellationToken ct)
    {
        if (!aircraft.IsAvailable || !aircraft.IsMaintenanceDue) return;

        var here = await LocationAsync(aircraft, ct);

        if (flight.Type == SpecialFlightType.MaintenanceFerry || here.Icao == Airline.MaintenanceHubIcao)
        {
            StartMaintenance(aircraft, $"Scheduled maintenance at {here.Icao} ({aircraft.AircraftType.MaintenanceIntervalHours}h interval)");
            return;
        }

        if (await HasActiveSpecialFlightAsync(aircraft.Id, flight.Id, ct)) return;

        await PlanFerryAsync(aircraft, here, ct);
    }

    private async Task PlanFerryAsync(Aircraft aircraft, Airport here, CancellationToken ct)
    {
        var hub = await db.Airports.FirstOrDefaultAsync(a => a.Icao == Airline.MaintenanceHubIcao, ct);
        var rank = await db.Ranks.AsNoTracking().OrderByDescending(r => r.Level).FirstOrDefaultAsync(ct);
        var onSite = $"Scheduled maintenance at {here.Icao} ({aircraft.AircraftType.MaintenanceIntervalHours}h interval)";

        if (hub is null || rank is null)
        {
            StartMaintenance(aircraft, onSite);
            return;
        }

        List<Route>? routes = null;
        try
        {
            routes = await support.CreateRoutesAsync([here, hub], aircraft.AircraftType, [], ct);
            var now = DateTime.UtcNow;

            db.SpecialFlights.Add(new SpecialFlight(
                $"Maintenance ferry {aircraft.Registration}",
                $"{aircraft.Registration} reached its {aircraft.AircraftType.MaintenanceIntervalHours}h maintenance interval. Fly it to {hub.Icao} ({hub.City}) for scheduled maintenance.",
                SpecialFlightType.MaintenanceFerry,
                DifficultyFor(routes[0].DistanceNm),
                aircraft,
                rank.Level,
                now,
                now.Add(FerryWindow),
                routes));
        }
        catch (Exception ex) when (ex is DomainException or AppException)
        {
            if (routes is not null) db.Routes.RemoveRange(routes);
            StartMaintenance(aircraft, onSite);
        }
    }

    private Task<bool> HasActiveSpecialFlightAsync(Guid aircraftId, Guid? exceptId, CancellationToken ct) =>
        db.SpecialFlights.AnyAsync(f =>
            f.AircraftId == aircraftId
            && f.Id != exceptId
            && (f.Status == SpecialFlightStatus.Open || f.Status == SpecialFlightStatus.Taken), ct);

    private Task<Airport> LocationAsync(Aircraft aircraft, CancellationToken ct) =>
        db.Airports.FirstAsync(a => a.Id == aircraft.CurrentAirportId, ct);

    private static void StartMaintenance(Aircraft aircraft, string reason) =>
        aircraft.SendToMaintenance(reason, DateTime.UtcNow.Add(MaintenanceDuration));

    private static int DifficultyFor(int distanceNm) => distanceNm switch
    {
        <= 300 => 1,
        <= 600 => 2,
        <= 1000 => 3,
        <= 1500 => 4,
        _ => 5
    };
}