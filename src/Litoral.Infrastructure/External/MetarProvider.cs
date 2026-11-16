using System.Text.Json;
using Litoral.Application.Bookings;
using Microsoft.Extensions.Logging;

namespace Litoral.Infrastructure.External;

public sealed class MetarProvider(HttpClient http, ILogger<MetarProvider> logger) : IMetarProvider
{
    public async Task<IReadOnlyList<MetarDto>> GetAsync(IReadOnlyList<string> icaos, CancellationToken ct)
    {
        var codes = icaos.Distinct().ToList();
        var found = new Dictionary<string, MetarDto>();

        try
        {
            var url = $"https://aviationweather.gov/api/data/metar?ids={string.Join(',', codes)}&format=json";
            using var response = await http.GetAsync(url, ct);

            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength != 0)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                foreach (var item in json.RootElement.EnumerateArray())
                {
                    var icao = item.TryGetProperty("icaoId", out var id) ? id.GetString() : null;
                    if (icao is null || found.ContainsKey(icao)) continue;

                    var raw = item.TryGetProperty("rawOb", out var ob) ? ob.GetString() : null;
                    DateTime? observed = item.TryGetProperty("obsTime", out var time) && time.TryGetInt64(out var unix)
                        ? DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime
                        : null;

                    found[icao] = new MetarDto(icao, raw, observed);
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Could not load METAR for {Icaos}.", string.Join(',', codes));
        }

        return codes.Select(c => found.GetValueOrDefault(c) ?? new MetarDto(c, null, null)).ToList();
    }
}