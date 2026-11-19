using System.Globalization;
using Litoral.Acars.Core.Api;

namespace Litoral.Acars.Core.Flight;

public static class BriefingFormat
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string Cruise(int? feet) => feet is int ft and > 0 ? $"FL{(int)Math.Round(ft / 100d):000}" : "—";

    public static string Duration(int? minutes) => minutes is int m and >= 0 ? $"{m / 60}h{m % 60:00}" : "—";

    public static string Weight(int? value, string? units) =>
        value is int v ? $"{v.ToString("N0", Invariant)} {Units(units)}" : "—";

    private static string Units(string? units)
    {
        var u = (units ?? "").Trim().ToLowerInvariant();
        return u.StartsWith("lb", StringComparison.Ordinal) ? "lbs" : "kg";
    }

    public static string Wind(AirportWeather? w)
    {
        if (w?.WindSpeedKt is not int speed) return "—";
        var direction = string.IsNullOrWhiteSpace(w.WindDirection) ? "VRB" : w.WindDirection.Trim();
        return w.WindGustKt is int gust ? $"{direction}° {speed}G{gust} kt" : $"{direction}° {speed} kt";
    }

    public static AirportWeather? WeatherFor(AcarsBriefing? briefing, string? icao) =>
        briefing is null || icao is null
            ? null
            : briefing.Metars.FirstOrDefault(m => string.Equals(m.Icao, icao, StringComparison.OrdinalIgnoreCase));
}