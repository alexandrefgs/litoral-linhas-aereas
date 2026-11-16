using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class Airport : Entity
{
    private const double EarthRadiusNm = 3440.065;

    public string Icao { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public int MinimumRankLevel { get; private set; } = 1;

    private Airport() { }

    public Airport(string icao, string name, string city, string country, double latitude, double longitude, int minimumRankLevel = 1)
    {
        if (string.IsNullOrWhiteSpace(icao) || icao.Length != 4)
            throw new DomainException("ICAO do aeroporto deve ter 4 letras.");

        Icao = icao.ToUpperInvariant();
        Name = name;
        City = city;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
        SetMinimumRankLevel(minimumRankLevel);
    }

    public void SetMinimumRankLevel(int level)
    {
        if (level < 1)
            throw new DomainException("Nível mínimo de patente deve ser 1 ou maior.");

        MinimumRankLevel = level;
    }

    public bool IsAccessibleTo(Rank rank) => rank.Level >= MinimumRankLevel;

    public int DistanceNmTo(Airport other)
    {
        var lat1 = ToRad(Latitude);
        var lat2 = ToRad(other.Latitude);
        var dLat = lat2 - lat1;
        var dLon = ToRad(other.Longitude - Longitude);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        return (int)Math.Round(2 * EarthRadiusNm * Math.Asin(Math.Sqrt(a)));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180;
}