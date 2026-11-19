using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Flights;
using Litoral.Domain.Scoring;

namespace Litoral.Domain.Tests.Scoring;

internal static class ScoringTestData
{
    public static readonly DateTime Start = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    public static FlightEvaluationData Flight(string type = "B738", FlightNetwork network = FlightNetwork.Vatsim) => new()
    {
        AircraftTypeIcao = type,
        PlannedArrivalIcao = "SBGR",
        ArrivalIcao = "SBGR",
        Network = network
    };

    public static ScoringRule Reject(ScoringCategory category, ScoringCheck check, decimal? min = null, decimal? max = null,
        string? text = null, string? types = null, int grace = 0) =>
        new(category, check, $"{check} rule", ScoringOutcome.Reject, 0, ScoringMode.Once, min, max, text, types, grace);

    public static ScoringRule Points(ScoringCategory category, ScoringCheck check, int points, ScoringMode mode = ScoringMode.Once,
        decimal? min = null, decimal? max = null, string? text = null, string? types = null, int grace = 0) =>
        new(category, check, $"{check} rule", ScoringOutcome.Points, points, mode, min, max, text, types, grace);

    public static TelemetrySample Sample(int second, int? ias = null, int? flaps = null, bool? gearDown = null,
        int groundSpeed = 0, string? phase = null) =>
        new(Start.AddSeconds(second), -23.43, -46.47, 1000, null, ias, groundSpeed, 90, null, flaps, gearDown, null, phase);

    public static TelemetryEvent Event(FlightEventType type, int durationSeconds, int second = 0) =>
        new(type, Start.AddSeconds(second), durationSeconds, null, null);

    public static EvaluationResult Evaluate(FlightEvaluationData data, params ScoringRule[] rules) =>
        PirepEvaluator.Evaluate(data, rules);
}