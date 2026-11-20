using System.Globalization;
using System.Text.Json;

namespace Litoral.Domain.Flights;

public sealed record SimBriefFlight(
    string Airline,
    int FlightNumber,
    string Callsign,
    string Type,
    string TypeIcao,
    string Registration,
    string Origin,
    string Destination,
    int EstimatedMinutes,
    string Captain,
    string PilotId);

public static class SimBriefDispatch
{
    public const string DispatchUrl = "https://dispatch.simbrief.com/options/custom";
    public const int MediumCostIndex = 10;

    private static readonly HashSet<string> HeavyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "A306", "A30B", "A310", "A332", "A333", "A337", "A338", "A339", "A342", "A343", "A345", "A346", "A359", "A35K", "A388",
        "B742", "B744", "B748", "B74S", "B752", "B753", "B762", "B763", "B764", "B772", "B773", "B77L", "B77W", "B77F", "B778", "B779",
        "B788", "B789", "B78X", "DC10", "MD11", "IL96", "A124", "A225"
    };
    public static readonly TimeSpan DepartureLead = TimeSpan.FromMinutes(45);
    private static readonly TimeSpan Rounding = TimeSpan.FromMinutes(5);

    public static DateTime DepartureTime(DateTime nowUtc)
    {
        var target = nowUtc + DepartureLead;
        var ticks = (target.Ticks + Rounding.Ticks - 1) / Rounding.Ticks * Rounding.Ticks;
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    public static bool IsHeavy(string typeIcao) => HeavyTypes.Contains(typeIcao.Trim());

    public static int? DefaultCostIndex(string typeIcao) => IsHeavy(typeIcao) ? null : MediumCostIndex;

    public static string AtcRemarks(string pilotId) => $"{FlightPlanRemarks.Required(pilotId)} RMK/TCAS";

    public static IReadOnlyList<KeyValuePair<string, string>> Parameters(SimBriefFlight flight, DateTime nowUtc)
    {
        var departure = DepartureTime(nowUtc);
        var block = Math.Max(0, flight.EstimatedMinutes);

        List<KeyValuePair<string, string>> parameters =
        [
            new("airline", flight.Airline),
            new("fltnum", flight.FlightNumber.ToString(CultureInfo.InvariantCulture)),
            new("callsign", flight.Callsign),
            new("type", flight.Type),
            new("reg", flight.Registration),
            new("orig", flight.Origin),
            new("dest", flight.Destination),
            new("date", departure.ToString("ddMMMyy", CultureInfo.InvariantCulture).ToUpperInvariant()),
            new("deph", departure.ToString("HH", CultureInfo.InvariantCulture)),
            new("depm", departure.ToString("mm", CultureInfo.InvariantCulture)),
            new("steh", (block / 60).ToString(CultureInfo.InvariantCulture)),
            new("stem", (block % 60).ToString("00", CultureInfo.InvariantCulture)),
            new("cpt", flight.Captain),
            new("units", "KGS"),
            new("navlog", "1"),
            new("etops", "1"),
            new("stepclimbs", "1"),
            new("tlr", "1"),
            new("notams", "1"),
            new("firnot", "1"),
            new("maps", "detail"),
            new("planformat", "lido"),
            new("manualrmk", $"LITORAL LINHAS AEREAS - ITEM 18: {FlightPlanRemarks.Required(flight.PilotId)}"),
            new("acdata", JsonSerializer.Serialize(new { extrarmk = AtcRemarks(flight.PilotId) }))
        ];

        if (DefaultCostIndex(flight.TypeIcao) is int costIndex)
        {
            parameters.Add(new("cruise", "CI"));
            parameters.Add(new("civalue", costIndex.ToString(CultureInfo.InvariantCulture)));
        }

        return parameters;
    }

    public static string Url(SimBriefFlight flight, DateTime nowUtc) =>
        DispatchUrl + "?" + string.Join('&', Parameters(flight, nowUtc).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
}