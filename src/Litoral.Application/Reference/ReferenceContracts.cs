namespace Litoral.Application.Reference;

public sealed record HubDto(string Icao, string Name, string City, bool Passenger, bool Cargo);

public sealed record AirportDto(string Icao, string Name, string City, string Country, double Latitude, double Longitude, bool IsHub);

public sealed record RankDto(string Code, string Name, int Level, int MinFlightHours, int MinPoints);

public sealed record PublicStatsDto(int ActivePilots, int CompletedFlights, int AircraftTypes, int Destinations, int Hubs);

public interface IReferenceService
{
    Task<IReadOnlyList<HubDto>> GetHubsAsync(CancellationToken ct);
    Task<IReadOnlyList<RankDto>> GetRanksAsync(CancellationToken ct);
    Task<AirportDto?> GetAirportAsync(string icao, CancellationToken ct);
    Task<PublicStatsDto> GetPublicStatsAsync(CancellationToken ct);
}