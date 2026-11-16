namespace Litoral.Application.Admin;

public sealed record AirportLookupDto(
    string Icao,
    string Name,
    string City,
    string Country,
    string CountryCode,
    string Continent,
    double Latitude,
    double Longitude);

public interface IAirportLookupService
{
    Task<AirportLookupDto> FindAsync(string icao, CancellationToken ct);
}