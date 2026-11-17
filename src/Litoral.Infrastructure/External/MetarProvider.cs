using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Litoral.Application.Bookings;
using Microsoft.Extensions.Logging;

namespace Litoral.Infrastructure.External;

public sealed partial class MetarProvider(HttpClient http, ILogger<MetarProvider> logger) : IMetarProvider
{
    private const string BaseUrl = "https://aviationweather.gov/api/data";
    private const int Attempts = 3;

    public async Task<IReadOnlyList<MetarDto>> GetAsync(IReadOnlyList<string> icaos, CancellationToken ct)
    {
        var codes = icaos.Distinct().ToList();
        var ids = string.Join(',', codes);

        var metarsTask = FetchAsync($"{BaseUrl}/metar?ids={ids}&format=json", ct);
        var tafsTask = FetchAsync($"{BaseUrl}/taf?ids={ids}&format=json", ct);
        await Task.WhenAll(metarsTask, tafsTask);

        var tafs = new Dictionary<string, string>();
        foreach (var item in tafsTask.Result)
        {
            var icao = Text(item, "icaoId");
            var raw = Text(item, "rawTAF");
            if (icao is not null && raw is not null) tafs.TryAdd(icao, raw);
        }

        var found = new Dictionary<string, MetarDto>();
        foreach (var item in metarsTask.Result)
        {
            var icao = Text(item, "icaoId");
            if (icao is null || found.ContainsKey(icao)) continue;

            DateTime? observed = item.TryGetProperty("obsTime", out var time) && time.TryGetInt64(out var unix)
                ? DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime
                : null;

            var raw = Text(item, "rawOb");
            found[icao] = new MetarDto(
                icao,
                raw,
                observed,
                Text(item, "fltCat"),
                Int(item, "temp"),
                Int(item, "dewp"),
                Text(item, "wdir"),
                Int(item, "wspd"),
                Int(item, "wgst"),
                Visibility(raw, Text(item, "visib")),
                Int(item, "altim"),
                tafs.GetValueOrDefault(icao));
        }

        return codes
            .Select(c => found.GetValueOrDefault(c) ?? new MetarDto(c, null, null, null, null, null, null, null, null, null, null, tafs.GetValueOrDefault(c)))
            .ToList();
    }

    private async Task<List<JsonElement>> FetchAsync(string url, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                using var response = await http.GetAsync(url, ct);
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return [];
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                return json.RootElement.ValueKind == JsonValueKind.Array
                    ? json.RootElement.EnumerateArray().Select(e => e.Clone()).ToList()
                    : [];
            }
            catch (Exception ex) when (!ct.IsCancellationRequested && ex is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
            {
                if (attempt == Attempts)
                {
                    logger.LogWarning(ex, "Could not load weather from {Url}.", url);
                    return [];
                }

                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
            }
        }

        return [];
    }

    private static string? Visibility(string? raw, string? statuteMiles)
    {
        if (raw is not null)
        {
            if (CavokRegex().IsMatch(raw)) return "CAVOK";
            if (!StatuteMilesRegex().IsMatch(raw) && MetersRegex().Match(raw) is { Success: true } match)
            {
                var meters = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                return meters >= 9999 ? "10+ km"
                    : meters >= 5000 ? $"{meters / 1000} km"
                    : $"{meters} m";
            }
        }

        return statuteMiles is null ? null : $"{statuteMiles} SM";
    }

    [GeneratedRegex(@"\bCAVOK\b")]
    private static partial Regex CavokRegex();

    [GeneratedRegex(@"\d+SM\b")]
    private static partial Regex StatuteMilesRegex();

    [GeneratedRegex(@"(?:KT|MPS)(?:\s+\d{3}V\d{3})?\s+(\d{4})(?:NDV)?\b")]
    private static partial Regex MetersRegex();

    private static string? Text(JsonElement item, string key)
    {
        if (!item.TryGetProperty(key, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ? null : value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static int? Int(JsonElement item, string key) =>
        double.TryParse(Text(item, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? (int)Math.Round(value)
            : null;
}