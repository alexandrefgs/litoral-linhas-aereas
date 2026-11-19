using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Flights;
using Litoral.Domain.Scoring;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Mel;

public sealed class MelTracker(LitoralDbContext db)
{
    public const string SystemActor = "System";

    private const double BaseChance = 0.03;
    private const double MaxChance = 0.08;
    private const double WarningShare = 0.9;
    private const double CategoryCShare = 0.7;
    private const int MaxSystemItems = 2;
    private const int AltitudeToleranceFt = 300;

    public async Task AfterFlightAsync(Booking booking, Pirep pirep, FlightEvaluationData data, Airport landedAt, CancellationToken ct)
    {
        var aircraft = booking.Aircraft;
        var now = DateTime.UtcNow;
        var startedAt = booking.StartedAt ?? now;

        var open = await db.AircraftMelItems
            .Include(a => a.MelItem)
            .Where(a => a.AircraftId == aircraft.Id && a.ClosedAt == null)
            .ToListAsync(ct);

        await CheckFlightAsync(pirep, data, open.Where(a => a.OpenedAt <= startedAt).ToList(), ct);

        if (await db.Hubs.AnyAsync(h => h.AirportId == landedAt.Id, ct))
        {
            foreach (var item in open)
                item.Close(SystemActor, $"Fixed at base {landedAt.Icao}", now);
            open.Clear();
        }

        if (booking.Type is BookingType.Tour or BookingType.Special || !aircraft.IsAvailable) return;
        if (open.Count(a => a.Source == MelSource.System) >= MaxSystemItems) return;
        if (Random.Shared.NextDouble() >= Chance(aircraft)) return;

        await OpenRandomAsync(aircraft, open.Select(a => a.MelItemId).ToHashSet(), ct);
    }

    public async Task CloseAllAsync(Aircraft aircraft, string reason, CancellationToken ct)
    {
        var open = await db.AircraftMelItems.Where(a => a.AircraftId == aircraft.Id && a.ClosedAt == null).ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var item in open)
            item.Close(SystemActor, reason, now);
    }

    private async Task CheckFlightAsync(Pirep pirep, FlightEvaluationData data, List<AircraftMelItem> items, CancellationToken ct)
    {
        var checkable = items.Where(a => a.MelItem.Kind == MelKind.Mel && a.MelItem.Verification != MelVerification.Info).ToList();
        if (checkable.Count == 0) return;

        var rules = await db.ScoringRules.AsNoTracking()
            .Where(r => r.IsActive && (r.Check == ScoringCheck.MelCompliance || r.Check == ScoringCheck.MelViolation))
            .ToListAsync(ct);
        var complied = rules.FirstOrDefault(r => r.Check == ScoringCheck.MelCompliance);
        var violated = rules.FirstOrDefault(r => r.Check == ScoringCheck.MelViolation);

        var maxAltitude = data.Samples.Count > 0 ? data.Samples.Max(s => s.AltitudeFt) : pirep.CruiseAltitudeFt;

        foreach (var item in checkable)
        {
            var definition = item.MelItem;
            var (result, detail) = Evaluate(definition, maxAltitude);

            db.FlightMelChecks.Add(new FlightMelCheck(pirep.Id, item, result, detail, result == MelCheckResult.Pending ? null : SystemActor));

            var rule = result switch
            {
                MelCheckResult.Complied => complied,
                MelCheckResult.Violated => violated,
                _ => null
            };
            if (rule is not null)
                pirep.AddMelItem(rule, $"{MelSupport.Label(definition)}: {(result == MelCheckResult.Complied ? "procedure followed" : "procedure not followed")}");
        }
    }

    private static (MelCheckResult Result, string? Detail) Evaluate(MelItem item, int? maxAltitude)
    {
        if (item.Verification != MelVerification.Acars)
            return (MelCheckResult.Pending, "Safety review");

        if (item.AcarsCheck == MelAcarsCheck.MaxAltitude && item.CheckValue is int limit && maxAltitude is int altitude)
        {
            var ok = altitude <= limit + AltitudeToleranceFt;
            return (ok ? MelCheckResult.Complied : MelCheckResult.Violated, $"Highest altitude {altitude:N0} ft, limit {limit:N0} ft");
        }

        return (MelCheckResult.Pending, "ACARS data not available, Safety review");
    }

    private static double Chance(Aircraft aircraft)
    {
        var type = aircraft.AircraftType;
        var progress = new[]
        {
            Share(aircraft.MinutesSinceMaintenance, type.MaintenanceIntervalHours),
            Share(aircraft.MinutesSinceCCheck, type.CCheckIntervalHours),
            Share(aircraft.MinutesSinceDCheck, type.DCheckIntervalHours)
        }.Max();

        if (progress < WarningShare) return BaseChance;
        var ramp = Math.Min(1, (progress - WarningShare) / (1 - WarningShare));
        return BaseChance + (MaxChance - BaseChance) * ramp;
    }

    private static double Share(int minutes, int intervalHours) =>
        intervalHours <= 0 ? 0 : minutes / (intervalHours * 60.0);

    private async Task OpenRandomAsync(Aircraft aircraft, HashSet<Guid> alreadyOpen, CancellationToken ct)
    {
        var candidates = (await db.MelItems
                .Where(m => m.IsActive && m.AutoEligible && m.Kind == MelKind.Mel
                    && (m.Category == MelCategory.C || m.Category == MelCategory.D))
                .ToListAsync(ct))
            .Where(m => !alreadyOpen.Contains(m.Id) && m.AppliesTo(aircraft.AircraftType.IcaoCode))
            .ToList();
        if (candidates.Count == 0) return;

        var preferred = Random.Shared.NextDouble() < CategoryCShare ? MelCategory.C : MelCategory.D;
        var pool = candidates.Where(m => m.Category == preferred).ToList();
        if (pool.Count == 0) pool = candidates;

        var item = pool[Random.Shared.Next(pool.Count)];
        db.AircraftMelItems.Add(new AircraftMelItem(aircraft, item, MelSource.System, SystemActor, "Found after the last flight"));
    }
}