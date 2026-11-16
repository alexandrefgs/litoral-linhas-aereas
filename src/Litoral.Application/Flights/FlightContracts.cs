namespace Litoral.Application.Flights;

public sealed record StartFlightRequest(string Network, string? Simulator, string? Client);

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

public sealed record ManualPirepRequest(
    string ArrivalIcao,
    string Network,
    DateTime BlockOffAt,
    DateTime? TakeoffAt,
    DateTime? LandingAt,
    DateTime BlockOnAt,
    int? LandingRateFpm,
    decimal? FuelUsedKg,
    string? FlightRoute,
    string Comments);

public sealed record FlightSessionDto(
    Guid Id,
    Guid BookingId,
    string Callsign,
    string Network,
    string Status,
    DateTime StartedAt,
    double? Latitude,
    double? Longitude,
    int? AltitudeFt,
    int? GroundSpeedKt,
    int? Heading,
    string? Phase,
    DateTime? LastReportAt);

public sealed record PirepScoreItemDto(
    Guid? RuleId,
    string Category,
    string Check,
    string Description,
    string Outcome,
    int Points,
    int Quantity,
    bool IsManual,
    bool IsVoided);

public sealed record PirepEventDto(string Type, DateTime At, int DurationSeconds, decimal? Value, string? Detail);

public sealed record TrackPointDto(DateTime At, double Latitude, double Longitude, int AltitudeFt, int GroundSpeedKt);

public sealed record PirepSummaryDto(
    Guid Id,
    string Callsign,
    string DepartureIcao,
    string ArrivalIcao,
    string PlannedArrivalIcao,
    string Registration,
    string AircraftTypeIcao,
    string Source,
    string Status,
    string Network,
    int BlockMinutes,
    int? LandingRateFpm,
    int ScorePoints,
    bool HasRejection,
    int Conformity,
    DateTime SubmittedAt);

public sealed record PirepDto(
    PirepSummaryDto Summary,
    string PilotId,
    string PilotName,
    DateTime BlockOffAt,
    DateTime? TakeoffAt,
    DateTime? LandingAt,
    DateTime BlockOnAt,
    int? AirMinutes,
    decimal? FuelUsedKg,
    decimal? FuelAtPushbackKg,
    int? DistanceNm,
    string? FlightRoute,
    int? CruiseAltitudeFt,
    decimal? OnlinePercent,
    string? Comments,
    DateTime? ReviewedAt,
    string? ReviewNote,
    IReadOnlyList<PirepScoreItemDto> Items,
    IReadOnlyList<PirepEventDto> Events,
    IReadOnlyList<TrackPointDto> Track);

public sealed record SimulateFlightRequest(string Scenario);

public interface IFlightTrackingService
{
    Task<FlightSessionDto?> GetActiveAsync(string userId, CancellationToken ct);
    Task<FlightSessionDto> StartAsync(string userId, StartFlightRequest request, CancellationToken ct);
    Task<int> ReportPositionsAsync(string userId, PositionBatchRequest request, CancellationToken ct);
    Task<PirepDto> CompleteAsync(string userId, CompleteFlightRequest request, CancellationToken ct);
    Task<IReadOnlyList<LiveFlightDto>> LiveAsync(CancellationToken ct);
    Task<IReadOnlyList<TrackPointDto>> LiveTrackAsync(Guid sessionId, CancellationToken ct);
}

public interface IPirepService
{
    Task<IReadOnlyList<PirepSummaryDto>> ListMineAsync(string userId, CancellationToken ct);
    Task<PirepDto> GetMineAsync(string userId, Guid id, CancellationToken ct);
    Task<PirepDto> FileManualAsync(string userId, ManualPirepRequest request, CancellationToken ct);
}

public sealed record LiveAirportDto(string Icao, string City, string Country, double Latitude, double Longitude);

public sealed record LiveFlightDto(
    Guid Id,
    string Callsign,
    string PilotId,
    string PilotName,
    string Registration,
    string AircraftTypeIcao,
    string AircraftTypeName,
    string? PhotoUrl,
    string? PhotoCredit,
    bool IsCargo,
    LiveAirportDto Departure,
    LiveAirportDto Arrival,
    string Network,
    string? Simulator,
    double Latitude,
    double Longitude,
    int AltitudeFt,
    int GroundSpeedKt,
    int Heading,
    string? Phase,
    int DistanceNm,
    int RemainingNm,
    int Progress,
    int? MinutesToArrival,
    DateTime LastReportAt);

public interface IFlightSimulator
{
    Task<PirepDto> SimulateAsync(string userId, SimulateFlightRequest request, CancellationToken ct);
}