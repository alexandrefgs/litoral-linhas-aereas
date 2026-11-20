using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Litoral.Application.Bookings;
using Litoral.Application.Common;
using Litoral.Domain.Common;
using Litoral.Domain.Flights;
using Microsoft.Extensions.Options;

namespace Litoral.Infrastructure.External;

public sealed class SimBriefProvider(HttpClient http, IOptions<SimBriefOptions> options) : ISimBriefProvider
{
    private const string NotResponding = "SimBrief is not responding. Try again in a few minutes.";
    private const string LoaderUrl = "https://www.simbrief.com/ofp/ofp.loader.api.php";

    public bool CanGenerate => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

        public string BuildGenerateUrl(SimBriefRequest request)
    {
        if (!CanGenerate)
            throw new AppException(AppErrorKind.Conflict, "The SimBrief API is not configured.");

        var now = DateTimeOffset.UtcNow;
        var timestamp = now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var outputPage = options.Value.OutputPage;
        var flight = request.Flight;
        var apiCode = Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(
            options.Value.ApiKey + flight.Origin + flight.Destination + flight.Type + timestamp + outputPage)));

        var query = SimBriefDispatch.Parameters(flight, now.UtcDateTime).ToDictionary(p => p.Key, p => p.Value);
        query["fo"] = request.FirstOfficer;
        query["find_sidstar"] = "R";
        query["static_id"] = request.StaticId;
        query["outputpage"] = outputPage;
        query["timestamp"] = timestamp;
        query["apicode"] = apiCode;

        return LoaderUrl + "?" + string.Join('&', query.Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}"));
    }

    public async Task<SimBriefOfpDto?> GetLatestAsync(string username, string? staticId, string expectedOrigin, string expectedDestination, CancellationToken ct)
    {
        if (staticId is not null && await FetchAsync(username, staticId, expectedOrigin, expectedDestination, ct) is { } plan)
            return plan;

        return await FetchAsync(username, null, expectedOrigin, expectedDestination, ct);
    }

    private async Task<SimBriefOfpDto?> FetchAsync(string username, string? staticId, string expectedOrigin, string expectedDestination, CancellationToken ct)
    {
        HttpResponseMessage response;
        var url = $"https://www.simbrief.com/api/xml.fetcher.php?username={Uri.EscapeDataString(username)}&json=1";
        if (staticId is not null) url += $"&static_id={Uri.EscapeDataString(staticId)}";

        try
        {
            response = await http.GetAsync(url, ct);
        }
        catch (HttpRequestException)
        {
            throw new AppException(AppErrorKind.Conflict, NotResponding);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                return null;
            if (!response.IsSuccessStatusCode)
                throw new AppException(AppErrorKind.Conflict, NotResponding);

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = json.RootElement;

            var origin = Text(root, "origin", "icao_code") ?? string.Empty;
            var destination = Text(root, "destination", "icao_code") ?? string.Empty;
            var directory = Text(root, "files", "directory");
            var pdf = Text(root, "files", "pdf", "link");
            var generated = Number(root, "params", "time_generated");
            var enroute = Number(root, "times", "est_time_enroute");

            return new SimBriefOfpDto(
                origin,
                destination,
                origin == expectedOrigin && destination == expectedDestination,
                Text(root, "general", "route"),
                (int?)Number(root, "general", "initial_altitude"),
                AlternateIcao(root),
                (int?)Number(root, "fuel", "plan_ramp"),
                Text(root, "params", "units"),
                enroute is null ? null : (int)(enroute.Value / 60),
                directory is not null && pdf is not null ? directory + pdf : null,
                Text(root, "prefile", "vatsim", "link"),
                Text(root, "prefile", "ivao", "link"),
                generated is null ? null : DateTimeOffset.FromUnixTimeSeconds(generated.Value).UtcDateTime,
                Text(root, "origin", "plan_rwy"),
                Text(root, "destination", "plan_rwy"),
                (int?)Number(root, "general", "costindex"),
                Text(root, "params", "airac"),
                new SimBriefLoadsheetDto(
                    Weight(root, "weights", "pax_count"),
                    Weight(root, "weights", "cargo"),
                    Weight(root, "weights", "payload"),
                    Weight(root, "weights", "est_zfw"),
                    Weight(root, "weights", "max_zfw"),
                    Weight(root, "weights", "est_tow"),
                    Weight(root, "weights", "max_tow"),
                    Weight(root, "weights", "est_ldw"),
                    Weight(root, "weights", "max_ldw"),
                    Weight(root, "fuel", "taxi"),
                    Weight(root, "fuel", "enroute_burn"),
                    Weight(root, "fuel", "contingency"),
                    Weight(root, "fuel", "alternate_burn"),
                    Weight(root, "fuel", "reserve"),
                    Weight(root, "fuel", "extra"),
                    Weight(root, "fuel", "plan_takeoff"),
                    Weight(root, "fuel", "plan_ramp")),
                    Text(root, "text", "plan_html"));
        }
    }

    public async Task<byte[]> DownloadPdfAsync(string url, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !(uri.Host == "simbrief.com" || uri.Host.EndsWith(".simbrief.com", StringComparison.OrdinalIgnoreCase)))
            throw new AppException(AppErrorKind.Validation, "Invalid OFP address.");

        try
        {
            using var response = await http.GetAsync(uri, ct);
            if (!response.IsSuccessStatusCode)
                throw new AppException(AppErrorKind.NotFound, "The OFP PDF is not available on SimBrief anymore. Generate a new one.");

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException)
        {
            throw new AppException(AppErrorKind.Conflict, NotResponding);
        }
    }

    private static string? AlternateIcao(JsonElement root)
    {
        if (!root.TryGetProperty("alternate", out var alternate)) return null;
        if (alternate.ValueKind == JsonValueKind.Array) alternate = alternate.EnumerateArray().FirstOrDefault();
        return alternate.ValueKind == JsonValueKind.Object ? Text(alternate, "icao_code") : null;
    }

    private static string? Text(JsonElement element, params string[] path)
    {
        foreach (var key in path)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(key, out element))
                return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => string.IsNullOrWhiteSpace(element.GetString()) ? null : element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null
        };
    }

    private static long? Number(JsonElement element, params string[] path) =>
        long.TryParse(Text(element, path), out var value) ? value : null;

    private static int? Weight(JsonElement element, params string[] path) =>
        double.TryParse(Text(element, path), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? (int)Math.Round(value)
            : null;
}