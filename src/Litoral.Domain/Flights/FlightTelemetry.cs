using Litoral.Domain.Enums;

namespace Litoral.Domain.Flights;

public sealed record TelemetrySample(
    DateTime At,
    double Latitude,
    double Longitude,
    int AltitudeFt,
    int? AglFt,
    int? IasKt,
    int GroundSpeedKt,
    int Heading,
    int? VerticalSpeedFpm,
    int? Flaps,
    bool? GearDown,
    bool? OnGround,
    string? Phase);

public sealed record TelemetryEvent(FlightEventType Type, DateTime At, int DurationSeconds, decimal? Value, string? Detail);

public sealed record FlightEvaluationData
{
    public required string AircraftTypeIcao { get; init; }
    public required string PlannedArrivalIcao { get; init; }
    public required string ArrivalIcao { get; init; }
    public required FlightNetwork Network { get; init; }
    public int? BlockMinutes { get; init; }
    public int? LandingRateFpm { get; init; }
    public decimal? FuelAtPushbackKg { get; init; }
    public bool? FuelInPounds { get; init; }
    public decimal? OnlinePercent { get; init; }
    public int? FirstEngineStarted { get; init; }
    public int? TaxiInSeconds { get; init; }
    public bool? EnginesRunningAtLog { get; init; }
    public decimal? LiftoffPitch { get; init; }
    public int? RotationSpeedKt { get; init; }
    public int? TakeoffFlaps { get; init; }
    public int? LandingFlaps { get; init; }
    public int? FlapsRetractedAtFt { get; init; }
    public int? LandingConfiguredAtFt { get; init; }
    public bool DiversionReported { get; init; }
    public bool IncompleteData { get; init; }
    public IReadOnlyList<TelemetrySample> Samples { get; init; } = [];
    public IReadOnlyList<TelemetryEvent> Events { get; init; } = [];
}