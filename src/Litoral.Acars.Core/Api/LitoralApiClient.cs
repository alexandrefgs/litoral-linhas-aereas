using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Litoral.Acars.Core.Api;

public sealed class LitoralApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

public sealed class LitoralApiClient(HttpClient http)
{
    public const string KeyHeader = "X-Acars-Key";
    public const int MaxPositionsPerBatch = 500;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static HttpClient CreateHttp(Uri baseAddress, string key, HttpMessageHandler? handler = null)
    {
        var client = handler is null ? new HttpClient() : new HttpClient(handler);
        client.BaseAddress = baseAddress;
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add(KeyHeader, key.Trim());
        return client;
    }

    public Task<AcarsBooking?> GetBookingAsync(CancellationToken ct = default) =>
        GetAsync<AcarsBooking>("api/acars/booking", ct);
    
    public async Task<AcarsBriefing> GetBriefingAsync(CancellationToken ct = default) =>
        (await GetAsync<AcarsBriefing>("api/acars/briefing", ct))!;

    public async Task<AcarsOfp> GetOfpAsync(CancellationToken ct = default) =>
        (await GetAsync<AcarsOfp>("api/acars/briefing/ofp", ct))!;

    public async Task<IReadOnlyList<LiveFlight>> GetLiveFlightsAsync(CancellationToken ct = default) =>
        await GetAsync<List<LiveFlight>>("api/acars/live", ct) ?? [];

    public Task<FlightSession?> GetActiveFlightAsync(CancellationToken ct = default) =>
        GetAsync<FlightSession>("api/acars/flight", ct);

    public async Task<FlightSession> StartFlightAsync(StartFlightRequest request, CancellationToken ct = default) =>
        await PostAsync<FlightSession>("api/acars/flight", request, ct);
    
    public async Task<FlightSession> ChangeNetworkAsync(string network, CancellationToken ct = default) =>
        await PostAsync<FlightSession>("api/acars/flight/network", new ChangeNetworkRequest(network), ct);

    public async Task<int> SendPositionsAsync(IReadOnlyList<PositionReport> positions, CancellationToken ct = default)
    {
        if (positions.Count == 0) return 0;
        if (positions.Count > MaxPositionsPerBatch)
            throw new ArgumentException($"Send up to {MaxPositionsPerBatch} positions at a time.", nameof(positions));
        return await PostAsync<int>("api/acars/flight/positions", new PositionBatchRequest(positions), ct);
    }

    public async Task<PirepResult> CompleteFlightAsync(CompleteFlightRequest request, CancellationToken ct = default) =>
        await PostAsync<PirepResult>("api/acars/flight/complete", request, ct);

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await http.GetAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json, ct);
    }

    private async Task<T> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, body, Json, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<T>(Json, ct))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "The ACARS key was refused. Create a new key on your profile page.",
            HttpStatusCode.Forbidden => "This ACARS key is not allowed to do that.",
            _ => $"The server answered {(int)response.StatusCode}."
        };

        var text = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized && !string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("detail", out var detail) && detail.GetString() is { Length: > 0 } d) message = d;
                    else if (doc.RootElement.TryGetProperty("title", out var title) && title.GetString() is { Length: > 0 } t) message = t;
                }
            }
            catch (JsonException)
            {
            }
        }

        throw new LitoralApiException(response.StatusCode, message);
    }
}