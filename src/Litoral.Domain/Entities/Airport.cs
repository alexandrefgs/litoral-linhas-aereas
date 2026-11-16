using Litoral.Domain.Common;
using Litoral.Domain.Enums;

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
    public OperationType Operations { get; private set; } = OperationType.Passenger;
    public int MinimumRankLevel { get; private set; } = 1;
    public string? IataCode { get; private set; }
    public string? PhotoUrl { get; private set; }

    private Airport() { }

    public Airport(
        string icao,
        string name,
        string city,
        string country,
        double latitude,
        double longitude,
        OperationType operations,
        int minimumRankLevel = 1)
    {
        if (string.IsNullOrWhiteSpace(icao) || icao.Trim().Length != 4 || !icao.Trim().All(char.IsLetterOrDigit))
            throw new DomainException("Airport ICAO must have 4 characters (letters or numbers).");

        Icao = icao.Trim().ToUpperInvariant();
        Update(name, city, country, latitude, longitude);
        SetOperations(operations);
        SetMinimumRankLevel(minimumRankLevel);
    }

    public void Update(string name, string city, string country, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Airport name is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("Airport city is required.");
        if (string.IsNullOrWhiteSpace(country))
            throw new DomainException("Airport country is required.");
        if (latitude is < -90 or > 90)
            throw new DomainException("Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            throw new DomainException("Longitude must be between -180 and 180.");

        Name = name.Trim();
        City = city.Trim();
        Country = country.Trim();
        Latitude = latitude;
        Longitude = longitude;
    }

    public void SetOperations(OperationType operations)
    {
        if (operations == 0)
            throw new DomainException("The airport must have at least one type of operation.");

        Operations = operations;
    }

    public bool Supports(OperationType operation) => (Operations & operation) == operation;

    public void SetMinimumRankLevel(int level)
    {
        if (level < 1)
            throw new DomainException("Minimum rank level must be 1 or higher.");

        MinimumRankLevel = level;
    }

    public void SetIataCode(string? iata)
    {
        var code = string.IsNullOrWhiteSpace(iata) ? null : iata.Trim().ToUpperInvariant();
        if (code is not null && (code.Length != 3 || !code.All(char.IsAsciiLetterOrDigit)))
            throw new DomainException("IATA code must have 3 characters.");

        IataCode = code;
    }

    public string? SetPhoto(string? url)
    {
        var previous = PhotoUrl;
        PhotoUrl = url;
        return previous;
    }
    public bool IsAccessibleTo(Rank rank) => rank.Level >= MinimumRankLevel;

    public int DistanceNmTo(Airport other) => DistanceNm(Latitude, Longitude, other.Latitude, other.Longitude);

    public static int DistanceNm(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude)
    {
        var lat1 = ToRad(fromLatitude);
        var lat2 = ToRad(toLatitude);
        var dLat = lat2 - lat1;
        var dLon = ToRad(toLongitude - fromLongitude);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        return (int)Math.Round(2 * EarthRadiusNm * Math.Asin(Math.Sqrt(a)));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180;
}