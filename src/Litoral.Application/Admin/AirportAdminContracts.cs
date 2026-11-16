namespace Litoral.Application.Admin;

public sealed record AdminAirportDto(
    string Icao,
    string? Iata,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string MinimumRank,
    bool Passenger,
    bool Cargo,
    bool IsHub,
    string? PhotoUrl,
    string? PhotoCredit);

public sealed record CreateAirportRequest(
    string Icao,
    string? Iata,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string MinimumRank,
    bool Passenger,
    bool Cargo,
    bool IsHub);

public sealed record UpdateAirportRequest(
    string? Iata,
    string Name,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string MinimumRank,
    bool Passenger,
    bool Cargo,
    bool IsHub);

public sealed record AirportPhotoCreditRequest(string? Credit);

public interface IAirportAdminService
{
    Task<IReadOnlyList<AdminAirportDto>> ListAsync(CancellationToken ct);
    Task<AdminAirportDto> GetAsync(string icao, CancellationToken ct);
    Task<AdminAirportDto> CreateAsync(CreateAirportRequest request, CancellationToken ct);
    Task<AdminAirportDto> UpdateAsync(string icao, UpdateAirportRequest request, CancellationToken ct);
    Task DeleteAsync(string icao, CancellationToken ct);
    Task<AdminAirportDto> UploadPhotoAsync(string icao, PhotoUpload upload, CancellationToken ct);
    Task<AdminAirportDto> UpdatePhotoCreditAsync(string icao, string? credit, CancellationToken ct);
    Task<AdminAirportDto> RemovePhotoAsync(string icao, CancellationToken ct);
}