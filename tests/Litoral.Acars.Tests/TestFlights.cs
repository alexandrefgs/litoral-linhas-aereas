using System.Net;
using System.Text;
using Litoral.Acars.Core.Api;
using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Tests;

public static class TestFlights
{
    public const string Type = "B738";
    public const double SbgrLat = -23.4356;
    public const double SbgrLon = -46.4731;
    public const double SbflLat = -27.6703;
    public const double SbflLon = -48.5525;

    public static readonly DateTime Start = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    public static ScriptedFlightOptions Options(string type = Type) => new(type, SbgrLat, SbgrLon, SbflLat, SbflLon);

    public static List<SimSnapshot> Script(ScriptedFlightOptions? options = null) =>
        ScriptedFlight.Generate(Start, options ?? Options()).ToList();
}

public sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
        Requests.Add((request, body));
        return respond(request);
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public LitoralApiClient Client(string key = "ltl_test") =>
        new(LitoralApiClient.CreateHttp(new Uri("https://litoral.test/"), key, this));
}