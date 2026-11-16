namespace Litoral.Infrastructure.Reference;

public sealed class AirportCatalogEntry
{
    public string Icao { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string City { get; set; } = null!;
    public string CountryCode { get; set; } = null!;
    public string CountryName { get; set; } = null!;
    public string Continent { get; set; } = null!;
    public string Type { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}