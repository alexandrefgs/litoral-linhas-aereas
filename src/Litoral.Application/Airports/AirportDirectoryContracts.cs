namespace Litoral.Application.Airports;

public sealed record AirportPilotDto(int PilotNumber, string DisplayId, string Name, string FirstName, string LastName, string? PhotoUrl);

public sealed record AirportListItemDto(
    string Icao,
    string? Iata,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string? PhotoUrl,
    string? PhotoCredit,
    bool IsBase,
    bool Passenger,
    bool Cargo,
    int MinimumRankLevel,
    string MinimumRankName,
    string MinimumRankCode,
    int AircraftOnGround,
    int Departures,
    int JumpseatCost,
    int PilotCount,
    IReadOnlyList<AirportPilotDto> Pilots);

public sealed record AirportDirectoryDto(
    string CurrentIcao,
    int Points,
    string RankName,
    string? JumpseatBlockedReason,
    IReadOnlyList<AirportListItemDto> Airports);

public sealed record AirportAircraftDto(
    string Registration,
    string TypeIcao,
    string TypeName,
    string? PhotoUrl,
    string? PhotoCredit,
    string Status,
    string? LastCallsign,
    DateTime? LastFlightAt);

public sealed record AirportDepartureDto(
    Guid RouteId,
    string Callsign,
    string ArrivalIcao,
    string? ArrivalIata,
    string ArrivalName,
    string ArrivalCity,
    string ArrivalCountry,
    bool IsCargo,
    int DistanceNm,
    int EstimatedMinutes,
    IReadOnlyList<string> AircraftTypes,
    bool HasAircraft);

public sealed record JumpseatResultDto(string FromIcao, string ToIcao, int Cost, int Points);

public interface IAirportDirectoryService
{
    Task<AirportDirectoryDto> ListAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<AirportAircraftDto>> AircraftAsync(string icao, CancellationToken ct);
    Task<IReadOnlyList<AirportDepartureDto>> DeparturesAsync(string userId, string icao, CancellationToken ct);
    Task<JumpseatResultDto> JumpseatAsync(string userId, string icao, CancellationToken ct);
}