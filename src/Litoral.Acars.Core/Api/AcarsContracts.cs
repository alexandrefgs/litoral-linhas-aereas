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

public sealed record ChangeNetworkRequest(string Network);

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


public sealed record BriefingAirport(string Icao, string Name, string City, string Country, double Latitude, double Longitude);

public sealed record BriefingAircraft(string Registration, string TypeIcao, string TypeName);

public sealed record BriefingBooking(
    Guid Id,
    string Callsign,
    string NetworkCallsign,
    BriefingAirport Departure,
    BriefingAirport Arrival,
    int DistanceNm,
    int EstimatedMinutes,
    BriefingAircraft Aircraft,
    DateTime ExpiresAt);

public sealed record CrewMember(string? DisplayId, string Name, string RankCode);

public sealed record AirportWeather(
    string Icao,
    string? Raw,
    string? Category,
    int? TemperatureC,
    string? WindDirection,
    int? WindSpeedKt,
    int? WindGustKt,
    string? Visibility,
    int? QnhHpa,
    string? Taf);

public sealed record AcarsBriefing(
    BriefingBooking Booking,
    string? SimBriefUsername,
    IReadOnlyList<AirportWeather> Metars,
    CrewMember Captain,
    CrewMember FirstOfficer,
    string FlightPlanRemarks);

public sealed record OfpLoadsheet(int? Passengers, int? Cargo, int? ZeroFuelWeight, int? TakeoffWeight, int? LandingWeight, int? TripFuel, int? BlockFuel);

public sealed record AcarsOfp(
    string Origin,
    string Destination,
    bool MatchesBooking,
    string? Route,
    int? CruiseAltitude,
    string? Alternate,
    int? BlockFuel,
    string? FuelUnits,
    int? EnrouteMinutes,
    string? VatsimPrefileUrl,
    string? IvaoPrefileUrl,
    string? DepartureRunway,
    string? ArrivalRunway,
    int? CostIndex,
    string? Airac,
    OfpLoadsheet Loadsheet);

public sealed record LiveAirport(string Icao, string City, double Latitude, double Longitude);

public sealed record LiveFlight(
    Guid Id,
    string Callsign,
    string PilotId,
    string PilotName,
    string Registration,
    string AircraftTypeIcao,
    string AircraftTypeName,
    bool IsCargo,
    LiveAirport Departure,
    LiveAirport Arrival,
    string Network,
    double Latitude,
    double Longitude,
    int AltitudeFt,
    int GroundSpeedKt,
    int Heading,
    string? Phase,
    int Progress,
    int? MinutesToArrival);