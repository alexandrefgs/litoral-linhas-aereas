using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Flights;

namespace Litoral.Domain.Scoring;

public sealed record ScoreResultItem(
    Guid RuleId,
    ScoringCategory Category,
    ScoringCheck Check,
    string Description,
    ScoringOutcome Outcome,
    int Points,
    int Quantity);

public sealed record EvaluationResult(IReadOnlyList<ScoreResultItem> Items, int Points, bool Rejected, int Conformity)
{
    public static int ConformityOf(IEnumerable<ScoreResultItem> items)
    {
        var list = items.ToList();
        if (list.Any(i => i.Outcome == ScoringOutcome.Reject)) return 0;

        var penalty = list.Where(i => i.Points < 0).Sum(i => -i.Points);
        return Math.Max(0, 100 - penalty / 10);
    }

    public static EvaluationResult From(IEnumerable<ScoreResultItem> items)
    {
        var list = items.ToList();
        return new EvaluationResult(
            list,
            list.Sum(i => i.Points),
            list.Any(i => i.Outcome == ScoringOutcome.Reject),
            ConformityOf(list));
    }
}

public static class PirepEvaluator
{
    private const int TakeoffRollMinKt = 60;

    public static EvaluationResult Evaluate(FlightEvaluationData data, IEnumerable<ScoringRule> rules)
    {
        var applicable = rules
            .Where(r => r.IsActive && r.AppliesTo(data.AircraftTypeIcao))
            .Where(r => ScoringCatalog.Get(r.Check).Source == ScoringSource.Acars)
            .ToList();

        var samples = data.Samples.OrderBy(s => s.At).ToList();
        var items = new List<ScoreResultItem>();

        foreach (var rule in applicable.Where(r => r.Check != ScoringCheck.OverspeedTotal))
        {
            var (count, seconds) = Measure(rule, data, samples);

            var quantity = rule.Mode switch
            {
                ScoringMode.Once => count > 0 ? 1 : 0,
                ScoringMode.PerOccurrence => count,
                ScoringMode.PerSecond => seconds,
                _ => 0
            };

            if (quantity > 0) items.Add(ToItem(rule, quantity));
        }

        var overspeedPenalty = items.Where(i => i.Check == ScoringCheck.Overspeed && i.Points < 0).Sum(i => -i.Points);

        foreach (var rule in applicable.Where(r => r.Check == ScoringCheck.OverspeedTotal))
        {
            if (rule.Min is decimal limit && overspeedPenalty > 0 && overspeedPenalty >= limit)
                items.Add(ToItem(rule, 1));
        }

        return EvaluationResult.From(items);
    }

    private static ScoreResultItem ToItem(ScoringRule rule, int quantity) => new(
        rule.Id,
        rule.Category,
        rule.Check,
        rule.Description,
        rule.Outcome,
        rule.Outcome == ScoringOutcome.Reject ? 0 : rule.Points * quantity,
        quantity);

    private static (int Count, int Seconds) Measure(ScoringRule rule, FlightEvaluationData d, List<TelemetrySample> samples)
    {
        static (int, int) Hit(bool condition) => condition ? (1, 0) : (0, 0);

        return rule.Check switch
        {
            ScoringCheck.BlockTime => Hit(d.BlockMinutes is int m && InRange(m, rule.Min, rule.Max)),
            ScoringCheck.TouchdownRate => Hit(d.LandingRateFpm is int rate && InRange(-rate, rule.Min, rule.Max)),
            ScoringCheck.FlapsOverspeed => FlapsOverspeed(rule, samples),
            ScoringCheck.TakeoffFlapsSet => Hit(d.TakeoffFlaps is int f && Positions(rule).Contains(f)),
            ScoringCheck.TakeoffFlapsNotSet => Hit(d.TakeoffFlaps is int f && !Positions(rule).Contains(f)),
            ScoringCheck.LandingFlapsSet => Hit(d.LandingFlaps is int f && Positions(rule).Contains(f)),
            ScoringCheck.LandingFlapsNotSet => Hit(d.LandingFlaps is int f && !Positions(rule).Contains(f)),
            ScoringCheck.TailStrike => Hit(d.LiftoffPitch is decimal p && rule.Min is decimal min && p >= min),
            ScoringCheck.RotationSpeedAbove => Hit(d.RotationSpeedKt is int vr && rule.Max is decimal max && vr > max),
            ScoringCheck.RotationSpeedWithin => Hit(d.RotationSpeedKt is int vr && rule.Max is decimal max && vr <= max),
            ScoringCheck.GearUpOverspeed => GearOverspeed(rule, samples, down: false),
            ScoringCheck.GearDownOverspeed => GearOverspeed(rule, samples, down: true),
            ScoringCheck.Overspeed => Events(rule, d, FlightEventType.Overspeed),
            ScoringCheck.Slew => Events(rule, d, FlightEventType.Slew),
            ScoringCheck.Stall => Events(rule, d, FlightEventType.Stall),
            ScoringCheck.Pause => Events(rule, d, FlightEventType.Pause),
            ScoringCheck.SimulationRate => Events(rule, d, FlightEventType.SimulationRate),
            ScoringCheck.Refuel => Events(rule, d, FlightEventType.Refuel),
            ScoringCheck.Crash => Events(rule, d, FlightEventType.Crash),
            ScoringCheck.IncompleteData => Hit(d.IncompleteData),
            ScoringCheck.EngineStartOrder => Hit(d.FirstEngineStarted is int e && rule.Min is decimal n && e == n),
            ScoringCheck.TaxiInAbove => Hit(!Excluded(rule, d.ArrivalIcao) && d.TaxiInSeconds is int t && rule.Min is decimal l && t > l),
            ScoringCheck.TaxiInBelow => Hit(!Excluded(rule, d.ArrivalIcao) && d.TaxiInSeconds is int t && rule.Min is decimal l && t < l),
            ScoringCheck.EnginesOffAtLog => Hit(d.EnginesRunningAtLog == false),
            ScoringCheck.EnginesRunningAtLog => Hit(d.EnginesRunningAtLog == true),
            ScoringCheck.TaxiSpeed => TaxiSpeed(rule, samples),
            ScoringCheck.Offline => Hit(d.Network == FlightNetwork.Offline),
            ScoringCheck.EarlyFlapRetraction => Hit(d.FlapsRetractedAtFt is int h && rule.Max is decimal max && h < max),
            ScoringCheck.UnstableApproach => Hit(d.LandingConfiguredAtFt is int h && rule.Min is decimal min && h < min),
            ScoringCheck.LowFuelAtPushback => Hit(d.FuelAtPushbackKg is decimal f && rule.Min is decimal min && f < min),
            ScoringCheck.FuelInPounds => Hit(d.FuelInPounds == true),
            ScoringCheck.LowOnlinePercent => Hit(d.Network != FlightNetwork.Offline && d.OnlinePercent is decimal p && rule.Min is decimal min && p < min),
            ScoringCheck.UnreportedDiversion => Hit(!d.DiversionReported && d.ArrivalIcao != d.PlannedArrivalIcao),
            _ => (0, 0)
        };
    }

    private static bool InRange(decimal value, decimal? min, decimal? max) =>
        (min is null || value >= min) && (max is null || value <= max);

    private static HashSet<int> Positions(ScoringRule rule) =>
        (rule.Text ?? "")
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(v => int.TryParse(v, out var n) ? n : (int?)null)
            .Where(n => n is not null)
            .Select(n => n!.Value)
            .ToHashSet();

    private static bool Excluded(ScoringRule rule, string icao) =>
        (rule.Text ?? "")
            .ToUpperInvariant()
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Contains(icao.ToUpperInvariant());

    private static (int, int) Events(ScoringRule rule, FlightEvaluationData d, FlightEventType type)
    {
        var events = d.Events.Where(e => e.Type == type && e.DurationSeconds >= rule.GraceSeconds).ToList();
        return (events.Count, events.Sum(e => Math.Max(0, e.DurationSeconds - rule.GraceSeconds)));
    }

    private static (int, int) FlapsOverspeed(ScoringRule rule, List<TelemetrySample> samples)
    {
        if (rule.Max is not decimal max || !int.TryParse(rule.Text, out var flap)) return (0, 0);
        return Episodes(samples, s => s.Flaps == flap && s.IasKt is int ias && ias > max, rule.GraceSeconds);
    }

    private static (int, int) TaxiSpeed(ScoringRule rule, List<TelemetrySample> samples)
    {
        if (rule.Max is not decimal max) return (0, 0);
        return Episodes(samples, s => IsTaxi(s) && s.GroundSpeedKt > max && s.GroundSpeedKt < TakeoffRollMinKt, rule.GraceSeconds);
    }

    private static bool IsTaxi(TelemetrySample s) =>
        s.Phase is { } phase && phase.Contains("taxi", StringComparison.OrdinalIgnoreCase);

    private static (int, int) GearOverspeed(ScoringRule rule, List<TelemetrySample> samples, bool down)
    {
        if (rule.Max is not decimal max) return (0, 0);

        var count = 0;
        for (var i = 1; i < samples.Count; i++)
        {
            var (prev, cur) = (samples[i - 1], samples[i]);
            if (prev.GearDown is bool before && cur.GearDown is bool after && before != after && after == down
                && cur.IasKt is int ias && ias > max)
                count++;
        }

        return (count, 0);
    }

    private static (int Count, int Seconds) Episodes(List<TelemetrySample> samples, Func<TelemetrySample, bool> violates, int graceSeconds)
    {
        var count = 0;
        var seconds = 0;
        DateTime? start = null;
        DateTime last = default;

        void Close()
        {
            if (start is null) return;
            var duration = (int)(last - start.Value).TotalSeconds;
            if (duration >= graceSeconds)
            {
                count++;
                seconds += duration - graceSeconds;
            }
            start = null;
        }

        foreach (var sample in samples)
        {
            if (violates(sample))
            {
                start ??= sample.At;
                last = sample.At;
            }
            else
            {
                Close();
            }
        }

        Close();
        return (count, seconds);
    }
}