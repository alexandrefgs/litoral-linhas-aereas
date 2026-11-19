using System.Text.Json;
using Litoral.Acars.Core.Api;

namespace Litoral.Acars.Core.Map;

public static class LiveMapMessages
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Flights(IReadOnlyList<LiveFlight> flights, string? myCallsign) =>
        JsonSerializer.Serialize(new
        {
            type = "flights",
            me = string.IsNullOrWhiteSpace(myCallsign) ? null : myCallsign.Trim().ToUpperInvariant(),
            flights = flights.Select(f => new
            {
                callsign = f.Callsign,
                pilot = $"{f.PilotId} · {f.PilotName}",
                aircraft = $"{f.Registration} · {f.AircraftTypeName}",
                cargo = f.IsCargo,
                dep = new { icao = f.Departure.Icao, city = f.Departure.City, lat = f.Departure.Latitude, lon = f.Departure.Longitude },
                arr = new { icao = f.Arrival.Icao, city = f.Arrival.City, lat = f.Arrival.Latitude, lon = f.Arrival.Longitude },
                network = f.Network,
                lat = f.Latitude,
                lon = f.Longitude,
                alt = f.AltitudeFt,
                gs = f.GroundSpeedKt,
                hdg = ((f.Heading % 360) + 360) % 360,
                phase = f.Phase ?? "",
                progress = Math.Clamp(f.Progress, 0, 100),
                eta = f.MinutesToArrival
            })
        }, Json);

    public static string Theme(bool dark) => JsonSerializer.Serialize(new { type = "theme", dark }, Json);
}