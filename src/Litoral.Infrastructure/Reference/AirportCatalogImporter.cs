using System.Globalization;
using System.Text.RegularExpressions;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Litoral.Infrastructure.Reference;

public sealed partial class AirportCatalogImporter(
    LitoralDbContext db,
    HttpClient http,
    ILogger<AirportCatalogImporter> logger)
{
    private const string BaseUrl = "https://davidmegginson.github.io/ourairports-data/";
    private const int BatchSize = 2000;

    private static readonly HashSet<string> AirportTypes = ["large_airport", "medium_airport", "small_airport"];

        public async Task ImportIfEmptyAsync(CancellationToken ct = default)
    {
        if (!await db.AirportCatalog.AnyAsync(a => a.IataCode != null, ct))
        {
            try
            {
                var count = await ImportAsync(ct);
                logger.LogInformation("Airport catalog imported: {Count} airports.", count);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(ex, "Could not download the airport catalog. ICAO lookup will be unavailable until the next start.");
                return;
            }
        }

        await FillMissingIataAsync(ct);
    }

    private async Task FillMissingIataAsync(CancellationToken ct)
    {
        var airports = await db.Airports.Where(a => a.IataCode == null).ToListAsync(ct);
        if (airports.Count == 0) return;

        var icaos = airports.Select(a => a.Icao).ToList();
        var codes = await db.AirportCatalog.AsNoTracking()
            .Where(c => icaos.Contains(c.Icao) && c.IataCode != null)
            .ToDictionaryAsync(c => c.Icao, c => c.IataCode!, ct);

        foreach (var airport in airports)
            if (codes.TryGetValue(airport.Icao, out var iata))
                airport.SetIataCode(iata);

        await db.SaveChangesAsync(ct);
    }

    public async Task<int> ImportAsync(CancellationToken ct = default)
    {
        var countries = await DownloadAsync("countries.csv", ct);
        var countryHeader = Header(countries[0]);
        var countryNames = countries.Skip(1)
            .Where(r => r.Length == countries[0].Length)
            .ToDictionary(r => r[countryHeader["code"]], r => r[countryHeader["name"]]);

        var airports = await DownloadAsync("airports.csv", ct);
        var h = Header(airports[0]);
        var entries = new Dictionary<string, AirportCatalogEntry>();

        foreach (var row in airports.Skip(1))
        {
            if (row.Length != airports[0].Length || !AirportTypes.Contains(row[h["type"]])) continue;

            var icao = IcaoPattern().IsMatch(row[h["icao_code"]]) ? row[h["icao_code"]]
                : GpsPattern().IsMatch(row[h["gps_code"]]) ? row[h["gps_code"]]
                : null;

            if (icao is null || entries.ContainsKey(icao)) continue;

            if (!double.TryParse(row[h["latitude_deg"]], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(row[h["longitude_deg"]], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
                continue;

            var countryCode = row[h["iso_country"]];

            entries[icao] = new AirportCatalogEntry
            {
                Icao = icao,
                IataCode = IataPattern().IsMatch(row[h["iata_code"]]) ? row[h["iata_code"]] : null,
                Name = Truncate(row[h["name"]], 150),
                City = Truncate(row[h["municipality"]], 100),
                CountryCode = Truncate(countryCode, 2),
                CountryName = Truncate(countryNames.GetValueOrDefault(countryCode, countryCode), 100),
                Continent = Truncate(row[h["continent"]], 2),
                Type = row[h["type"]],
                Latitude = latitude,
                Longitude = longitude
            };
        }

        await db.AirportCatalog.ExecuteDeleteAsync(ct);

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            foreach (var batch in entries.Values.Chunk(BatchSize))
            {
                db.AirportCatalog.AddRange(batch);
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();
            }
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        return entries.Count;
    }

    private async Task<List<string[]>> DownloadAsync(string file, CancellationToken ct) =>
        CsvParser.Parse(await http.GetStringAsync(BaseUrl + file, ct));

    private static Dictionary<string, int> Header(string[] header) =>
        header.Select((name, index) => (name, index)).ToDictionary(x => x.name, x => x.index);

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    [GeneratedRegex("^[A-Z0-9]{4}$")]
    private static partial Regex IcaoPattern();

    [GeneratedRegex("^[A-Z]{4}$")]
    private static partial Regex GpsPattern();

    [GeneratedRegex("^[A-Z0-9]{3}$")]
    private static partial Regex IataPattern();
}