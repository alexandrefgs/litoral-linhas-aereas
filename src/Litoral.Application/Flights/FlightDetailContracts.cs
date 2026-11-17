namespace Litoral.Application.Flights;

public sealed record FlightCrewMemberDto(
    int? PilotNumber,
    string? DisplayId,
    string Name,
    string RankCode,
    string RankName,
    string? PhotoUrl,
    int Flights,
    int Minutes,
    int Points,
    bool IsVirtual);

public sealed record FlightAirportDto(
    string Icao,
    string? Iata,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string? PhotoUrl);

public sealed record FlightAircraftDto(
    string Registration,
    string TypeIcao,
    string TypeName,
    string? Variant,
    string? PhotoUrl,
    string? PhotoCredit,
    bool IsCargo);

public sealed record FlightDetailDto(
    Guid Id,
    string Callsign,
    string Status,
    string Source,
    string BookingType,
    string Network,
    string? Simulator,
    FlightAirportDto Departure,
    FlightAirportDto Arrival,
    string PlannedArrivalIcao,
    FlightCrewMemberDto Captain,
    FlightCrewMemberDto FirstOfficer,
    FlightAircraftDto Aircraft,
    DateTime BlockOffAt,
    DateTime? TakeoffAt,
    DateTime? LandingAt,
    DateTime BlockOnAt,
    int BlockMinutes,
    int? AirMinutes,
    int? LandingRateFpm,
    decimal? FuelUsedKg,
    decimal? FuelAtPushbackKg,
    int? DistanceNm,
    string? FlightRoute,
    int? CruiseAltitudeFt,
    decimal? OnlinePercent,
    int ScorePoints,
    int Conformity,
    bool HasRejection,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? Comments,
    string? ReviewNote,
    bool CanSeePrivate,
    IReadOnlyList<PirepScoreItemDto> Items,
    IReadOnlyList<PirepEventDto> Events,
    IReadOnlyList<TrackPointDto> Track);

public sealed record RecentFlightDto(
    Guid Id,
    string Callsign,
    string DepartureIcao,
    string ArrivalIcao,
    string DepartureCountry,
    string Registration,
    string TypeName,
    string? Variant,
    string? AircraftPhotoUrl,
    bool IsCargo,
    int BlockMinutes,
    string Network,
    string Status,
    int PilotNumber,
    string PilotName,
    string RankCode,
    string? PilotPhotoUrl,
    DateTime SubmittedAt);

public sealed record RecentFlightsDto(int MonthTotal, IReadOnlyList<RecentFlightDto> Flights);

public interface IFlightDetailService
{
    Task<FlightDetailDto> GetAsync(string viewerUserId, Guid id, CancellationToken ct);
    Task<RecentFlightsDto> RecentAsync(int take, CancellationToken ct);
}