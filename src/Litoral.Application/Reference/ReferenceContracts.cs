namespace Litoral.Application.Reference;

public sealed record HubDto(string Icao, string Name, string City, bool Passenger, bool Cargo);

public sealed record AirportDto(
    string Icao,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    bool IsHub,
    int AircraftOnGround,
    int Departures);

public sealed record RankDto(string Code, string Name, int Level, int MinFlightHours, int MinPoints);

public sealed record PublicStatsDto(int ActivePilots, int CompletedFlights, int AircraftTypes, int Destinations, int Hubs);

public sealed record PublicStaffMemberDto(int PilotNumber, string DisplayId, string Name, string? PhotoUrl, string Rank);

public sealed record PublicStaffPositionDto(string Code, string Name, string Team, IReadOnlyList<PublicStaffMemberDto> Holders);

public sealed record CareerRankDto(
    string Code,
    string Name,
    int Level,
    int MinFlightHours,
    int MinPoints,
    int Pilots,
    int Airports);

public interface IReferenceService
{
    Task<IReadOnlyList<HubDto>> GetHubsAsync(CancellationToken ct);
    Task<IReadOnlyList<RankDto>> GetRanksAsync(CancellationToken ct);
    Task<AirportDto?> GetAirportAsync(string icao, CancellationToken ct);
    Task<PublicStatsDto> GetPublicStatsAsync(CancellationToken ct);
    Task<IReadOnlyList<PublicStaffPositionDto>> GetStaffAsync(CancellationToken ct);
    Task<IReadOnlyList<CareerRankDto>> GetCareerAsync(CancellationToken ct);
}