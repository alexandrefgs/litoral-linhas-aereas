namespace Litoral.Acars.Core.Api;

public sealed record AcarsBooking(
    Guid BookingId,
    string Callsign,
    string DepartureIcao,
    double DepartureLatitude,
    double DepartureLongitude,
    string ArrivalIcao,
    double ArrivalLatitude,
    double ArrivalLongitude,
    string Registration,
    string AircraftTypeIcao,
    string AircraftTypeName);

public sealed record StartFlightRequest(string Network, string? Simulator, string? Client);

public sealed record FlightSession(Guid Id, Guid BookingId, string Callsign, string Network, string Status, DateTime StartedAt);

public sealed record PositionReport(
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

public sealed record PositionBatchRequest(IReadOnlyList<PositionReport> Positions);

public sealed record FlightEventReport(string Type, DateTime At, int DurationSeconds, decimal? Value, string? Detail);

public sealed record CompleteFlightRequest(
    string ArrivalIcao,
    DateTime BlockOffAt,
    DateTime? TakeoffAt,
    DateTime? LandingAt,
    DateTime BlockOnAt,
    int? LandingRateFpm,
    decimal? FuelAtPushback,
    decimal? FuelUsed,
    string? FuelUnit,
    int? DistanceNm,
    string? FlightRoute,
    int? CruiseAltitudeFt,
    decimal? OnlinePercent,
    int? FirstEngineStarted,
    int? TaxiInSeconds,
    bool? EnginesRunningAtLog,
    decimal? LiftoffPitch,
    int? RotationSpeedKt,
    int? TakeoffFlaps,
    int? LandingFlaps,
    int? FlapsRetractedAtFt,
    int? LandingConfiguredAtFt,
    bool DiversionReported,
    bool IncompleteData,
    string? Comments,
    IReadOnlyList<FlightEventReport>? Events);

public sealed record PirepSummary(
    Guid Id,
    string Callsign,
    string ArrivalIcao,
    string Status,
    int BlockMinutes,
    int? LandingRateFpm,
    int ScorePoints,
    bool HasRejection);

public sealed record PirepResult(PirepSummary Summary);