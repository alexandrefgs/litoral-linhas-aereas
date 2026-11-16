namespace Litoral.Application.Admin;

public sealed record AdminAirportDto(
    string Icao,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string MinimumRank,
    bool PassengerHub,
    bool CargoHub);

public sealed record CreateAirportRequest(
    string Icao,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string MinimumRank);

public sealed record UpdateAirportRequest(
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string MinimumRank);

public sealed record SetHubRequest(bool Passenger, bool Cargo);

public interface IAirportAdminService
{
    Task<IReadOnlyList<AdminAirportDto>> ListAsync(CancellationToken ct);
    Task<AdminAirportDto> GetAsync(string icao, CancellationToken ct);
    Task<AdminAirportDto> CreateAsync(CreateAirportRequest request, CancellationToken ct);
    Task<AdminAirportDto> UpdateAsync(string icao, UpdateAirportRequest request, CancellationToken ct);
    Task<AdminAirportDto> SetHubAsync(string icao, SetHubRequest request, CancellationToken ct);
    Task DeleteAsync(string icao, CancellationToken ct);
}