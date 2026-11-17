using Litoral.Application.Common;
using Litoral.Domain.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Fleet;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Operations;

public sealed class MaintenanceFerryPlanner(LitoralDbContext db, SpecialFlightSupport support)
{
    public static readonly TimeSpan FerryWindow = TimeSpan.FromDays(3);

    public async Task<int> PlanDueAsync(CancellationToken ct)
    {
        var held = db.SpecialFlights
            .Where(f => f.Status == SpecialFlightStatus.Open || f.Status == SpecialFlightStatus.Taken)
            .Select(f => f.AircraftId);

        var due = await db.Aircraft
            .Include(a => a.AircraftType)
            .Where(a => a.Status == AircraftStatus.Available
                && (a.MinutesSinceMaintenance >= a.AircraftType.MaintenanceIntervalHours * 60
                    || a.MinutesSinceCCheck >= a.AircraftType.CCheckIntervalHours * 60
                    || a.MinutesSinceDCheck >= a.AircraftType.DCheckIntervalHours * 60)
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

        if (special is { Type: SpecialFlightType.MaintenanceFerry, Status: SpecialFlightStatus.Completed })
        {
            var check = aircraft.DueCheck ?? MaintenanceCheck.A;
            if (await CanCheckAtAsync(check, here, aircraft.AircraftType, ct))
            {
                aircraft.StartCheck(check, here.Icao);
                return;
            }
        }

        if (aircraft.DueCheck is not { } due || special?.Status == SpecialFlightStatus.Taken) return;

        if (await CanCheckAtAsync(due, here, aircraft.AircraftType, ct))
        {
            aircraft.StartCheck(due, here.Icao);
            return;
        }

        if (await HasActiveSpecialFlightAsync(aircraft.Id, special?.Id, ct)) return;

        await PlanFerryAsync(aircraft, due, here, ct);
    }

        public async Task AfterMaintenanceAsync(Aircraft aircraft, MaintenanceCheck? completed, CancellationToken ct)
    {
        if (completed is not { } check || !MaintenancePolicy.IsHeavy(check) || !aircraft.IsAvailable) return;

        var here = await LocationAsync(aircraft, ct);
        var rank = await db.Ranks.AsNoTracking().OrderByDescending(r => r.Level).FirstOrDefaultAsync(ct);
        if (rank is null) return;

        Route? route = null;
        try
        {
            route = await support.CreateCheckRouteAsync(here, aircraft.AircraftType, ct);
            var now = DateTime.UtcNow;

            db.SpecialFlights.Add(new SpecialFlight(
                $"Check flight {aircraft.Registration}",
                $"{aircraft.Registration} just completed its {check}-check at {here.Icao}. Fly a functional check flight departing and landing at {here.Icao} before it returns to the line.",
                SpecialFlightType.CheckFlight,
                2,
                aircraft,
                rank.Level,
                now,
                now.Add(FerryWindow),
                [route]));
        }
        catch (Exception ex) when (ex is DomainException or AppException)
        {
            if (route is not null) db.Routes.Remove(route);
        }
    }

    public async Task AfterSpecialClosedAsync(SpecialFlight flight, Aircraft aircraft, CancellationToken ct)
    {
        if (!aircraft.IsAvailable || aircraft.DueCheck is not { } due) return;

        if (flight.Type == SpecialFlightType.MaintenanceFerry)
        {
            var destination = await FerryDestinationAsync(flight.Id, ct);
            aircraft.Relocate(destination);
            aircraft.StartCheck(due, destination.Icao);
            return;
        }

        var here = await LocationAsync(aircraft, ct);

        if (await CanCheckAtAsync(due, here, aircraft.AircraftType, ct))
        {
            aircraft.StartCheck(due, here.Icao);
            return;
        }

        if (await HasActiveSpecialFlightAsync(aircraft.Id, flight.Id, ct)) return;

        await PlanFerryAsync(aircraft, due, here, ct);
    }

    private async Task PlanFerryAsync(Aircraft aircraft, MaintenanceCheck check, Airport here, CancellationToken ct)
    {
        var type = aircraft.AircraftType;
        var destination = await DestinationAsync(check, here, type, ct);
        var ranks = await db.Ranks.AsNoTracking().OrderBy(r => r.Level).ToListAsync(ct);

        if (destination is null || ranks.Count == 0)
        {
            aircraft.StartCheck(check, here.Icao);
            return;
        }

        var rank = MaintenancePolicy.IsHeavy(check) ? ranks[^1] : ranks[0];

        List<Route>? routes = null;
        try
        {
            routes = await support.CreateRoutesAsync([here, destination], type, [], ct);
            var now = DateTime.UtcNow;

            db.SpecialFlights.Add(new SpecialFlight(
                $"{check}-check ferry {aircraft.Registration}",
                $"{aircraft.Registration} is due for its {check}-check ({type.IntervalHoursOf(check):N0}h interval). Fly it to {destination.Icao} ({destination.City}), where it will stay for {MaintenancePolicy.DurationLabel(check)}.",
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
            aircraft.StartCheck(check, here.Icao);
        }
    }

    private async Task<Airport?> DestinationAsync(MaintenanceCheck check, Airport here, AircraftType type, CancellationToken ct)
    {
        if (MaintenancePolicy.IsHeavy(check))
            return await db.Airports.FirstOrDefaultAsync(a => a.Icao == Airline.MaintenanceHubIcao, ct);

        var bases = await MaintenanceBasesAsync(type, ct);
        return bases
            .Where(a => a.Id != here.Id && here.DistanceNmTo(a) <= type.MaxRangeNm)
            .MinBy(a => here.DistanceNmTo(a));
    }

    private async Task<bool> CanCheckAtAsync(MaintenanceCheck check, Airport here, AircraftType type, CancellationToken ct)
    {
        if (here.Icao == Airline.MaintenanceHubIcao) return true;
        if (MaintenancePolicy.IsHeavy(check)) return false;

        var bases = await MaintenanceBasesAsync(type, ct);
        return bases.Any(a => a.Id == here.Id);
    }

    private async Task<List<Airport>> MaintenanceBasesAsync(AircraftType type, CancellationToken ct)
    {
        var airports = await db.Hubs.Select(h => h.Airport).ToListAsync(ct);
        var center = await db.Airports.FirstOrDefaultAsync(a => a.Icao == Airline.MaintenanceHubIcao, ct);
        if (center is not null && airports.All(a => a.Id != center.Id)) airports.Add(center);

        return airports.Where(a => a.Supports(type.Operation)).ToList();
    }

    private Task<bool> HasActiveSpecialFlightAsync(Guid aircraftId, Guid? exceptId, CancellationToken ct) =>
        db.SpecialFlights.AnyAsync(f =>
            f.AircraftId == aircraftId
            && f.Id != exceptId
            && (f.Status == SpecialFlightStatus.Open || f.Status == SpecialFlightStatus.Taken), ct);
    
    private Task<Airport> FerryDestinationAsync(Guid flightId, CancellationToken ct) =>
        db.SpecialFlightLegs
            .Where(l => l.SpecialFlightId == flightId)
            .OrderByDescending(l => l.Order)
            .Select(l => l.Route.ArrivalAirport)
            .FirstAsync(ct);

    private Task<Airport> LocationAsync(Aircraft aircraft, CancellationToken ct) =>
        db.Airports.FirstAsync(a => a.Id == aircraft.CurrentAirportId, ct);

    private static int DifficultyFor(int distanceNm) => distanceNm switch
    {
        <= 300 => 1,
        <= 600 => 2,
        <= 1000 => 3,
        <= 1500 => 4,
        _ => 5
    };
}