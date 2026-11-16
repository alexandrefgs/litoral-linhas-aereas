using System.Net;
using System.Text.Json;
using Litoral.Application.Bookings;
using Litoral.Application.Common;

namespace Litoral.Infrastructure.External;

public sealed class SimBriefProvider(HttpClient http) : ISimBriefProvider
{
    public async Task<SimBriefOfpDto?> GetLatestAsync(string username, string expectedOrigin, string expectedDestination, CancellationToken ct)
    {
        HttpResponseMessage response;

        try
        {
            response = await http.GetAsync($"https://www.simbrief.com/api/xml.fetcher.php?username={Uri.EscapeDataString(username)}&json=1", ct);
        }
        catch (HttpRequestException)
        {
            throw new AppException(AppErrorKind.Conflict, "SimBrief is not responding. Try again in a few minutes.");
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                return null;
            if (!response.IsSuccessStatusCode)
                throw new AppException(AppErrorKind.Conflict, "SimBrief is not responding. Try again in a few minutes.");

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
                generated is null ? null : DateTimeOffset.FromUnixTimeSeconds(generated.Value).UtcDateTime);
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
}