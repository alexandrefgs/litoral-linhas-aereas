using System.Net;
using System.Text.Json;
using Litoral.Acars.Core.Flight;
using Litoral.Acars.Tests.Api;

namespace Litoral.Acars.Tests.Flight;

public class AcarsFlightTests
{
    private const string BookingJson = """
        {"bookingId":"7d3c1a0e-0000-0000-0000-000000000001","callsign":"LTL1001","departureIcao":"SBGR","departureLatitude":-23.4356,"departureLongitude":-46.4731,"arrivalIcao":"SBFL",
         "arrivalLatitude":-27.6703,"arrivalLongitude":-48.5525,"registration":"PR-TAA","aircraftTypeIcao":"B738","aircraftTypeName":"Boeing 737-800"}
        """;

    private const string SessionJson = """{"id":"7d3c1a0e-0000-0000-0000-000000000002","bookingId":"7d3c1a0e-0000-0000-0000-000000000001","callsign":"LTL1001","network":"Vatsim","status":"Active","startedAt":"2026-11-19T12:00:00Z"}""";

    private const string PirepJson = """{"summary":{"id":"7d3c1a0e-0000-0000-0000-000000000003","callsign":"LTL1001","arrivalIcao":"SBFL","status":"Pending","blockMinutes":30,"landingRateFpm":-150,"scorePoints":12,"hasRejection":false}}""";

    private static FakeHandler Api() => new(request => request.RequestUri!.AbsolutePath switch
    {
        "/api/acars/booking" => FakeHandler.Json(BookingJson),
        "/api/acars/flight" => FakeHandler.Json(SessionJson),
        "/api/acars/flight/positions" => FakeHandler.Json("1"),
        "/api/acars/flight/network" => FakeHandler.Json(SessionJson.Replace("\"Vatsim\"", "\"Ivao\"")),
        "/api/acars/flight/complete" => FakeHandler.Json(PirepJson),
        _ => new HttpResponseMessage(HttpStatusCode.NotFound)
    });

    private static async Task<(AcarsFlight Flight, FakeHandler Handler)> StartedAsync()
    {
        var handler = Api();
        var flight = (await AcarsFlight.LoadAsync(handler.Client()))!;
        await flight.StartAsync("Vatsim", "MSFS 2024");
        return (flight, handler);
    }

    [Fact]
    public async Task Without_a_booking_there_is_nothing_to_fly()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        Assert.Null(await AcarsFlight.LoadAsync(handler.Client()));
    }

    [Fact]
    public async Task The_recorder_uses_the_booked_aircraft_type()
    {
        var (flight, _) = await StartedAsync();

        Assert.Equal("B738", flight.Booking.AircraftTypeIcao);
        Assert.Equal("LTL1001", flight.Session!.Callsign);
    }

    [Fact]
    public async Task Start_sends_the_network_simulator_and_client_name()
    {
        var (_, handler) = await StartedAsync();

        using var body = JsonDocument.Parse(handler.Requests.Single(r => r.Request.Method == HttpMethod.Post).Body);
        Assert.Equal("Vatsim", body.RootElement.GetProperty("network").GetString());
        Assert.Equal("MSFS 2024", body.RootElement.GetProperty("simulator").GetString());
        Assert.Equal(AcarsFlight.ClientName, body.RootElement.GetProperty("client").GetString());
    }

    [Fact]
    public async Task An_unknown_network_is_refused_before_calling_the_api()
    {
        var handler = Api();
        var flight = (await AcarsFlight.LoadAsync(handler.Client()))!;

        await LitoralApiClientTests.AssertThrowsAsync<ArgumentException>(() => flight.StartAsync("PilotEdge", null));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Nothing_is_sent_before_the_flight_starts()
    {
        var handler = Api();
        var flight = (await AcarsFlight.LoadAsync(handler.Client()))!;
        foreach (var s in TestFlights.Script().Take(100)) flight.OnSnapshot(s);

        Assert.Equal(0, await flight.FlushAsync());
    }

    [Fact]
    public async Task Flush_sends_everything_in_batches_of_500()
    {
        var (flight, handler) = await StartedAsync();
        foreach (var s in TestFlights.Script()) flight.OnSnapshot(s);
        var pending = flight.Recorder.PendingPositions;

        var sent = await flight.FlushAsync();

        var batches = handler.Requests.Where(r => r.Request.RequestUri!.AbsolutePath.EndsWith("/positions")).ToList();
        Assert.Equal(pending, sent);
        Assert.Equal((pending + 499) / 500, batches.Count);
        Assert.Equal(0, flight.Recorder.PendingPositions);
    }

    [Fact]
    public async Task A_failed_batch_stays_queued_for_the_next_try()
    {
        var fail = true;
        var handler = new FakeHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/acars/booking" => FakeHandler.Json(BookingJson),
            "/api/acars/flight" => FakeHandler.Json(SessionJson),
            _ => fail ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : FakeHandler.Json("1")
        });
        var flight = (await AcarsFlight.LoadAsync(handler.Client()))!;
        await flight.StartAsync("Offline", null);
        foreach (var s in TestFlights.Script().Take(300)) flight.OnSnapshot(s);
        var pending = flight.Recorder.PendingPositions;

        await LitoralApiClientTests.AssertThrowsAsync<Litoral.Acars.Core.Api.LitoralApiException>(() => flight.FlushAsync());
        Assert.Equal(pending, flight.Recorder.PendingPositions);

        fail = false;
        Assert.Equal(pending, await flight.FlushAsync());
    }

    [Fact]
    public async Task Completing_flushes_positions_before_filing()
    {
        var (flight, handler) = await StartedAsync();
        foreach (var s in TestFlights.Script()) flight.OnSnapshot(s);

        var pirep = await flight.CompleteAsync(null, "Smooth");

        var paths = handler.Requests.Select(r => r.Request.RequestUri!.AbsolutePath).ToList();
        Assert.Equal("/api/acars/flight/complete", paths[^1]);
        Assert.Contains("/api/acars/flight/positions", paths);
        Assert.Equal("Pending", pirep.Summary.Status);
        Assert.Equal(0, flight.Recorder.PendingPositions);
    }

        [Fact]
    public async Task The_network_can_be_changed_during_the_flight()
    {
        var (flight, handler) = await StartedAsync();

        var session = await flight.ChangeNetworkAsync("Ivao");

        var request = handler.Requests.Single(r => r.Request.RequestUri!.AbsolutePath == "/api/acars/flight/network");
        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("Ivao", body.RootElement.GetProperty("network").GetString());
        Assert.Equal("Ivao", session.Network);
        Assert.Same(session, flight.Session);
        Assert.Equal("Network changed from VATSIM to IVAO", flight.Log.Entries[^1].Message);
    }

    [Fact]
    public async Task Picking_the_same_network_sends_nothing()
    {
        var (flight, handler) = await StartedAsync();

        await flight.ChangeNetworkAsync("vatsim");

        Assert.DoesNotContain(handler.Requests, r => r.Request.RequestUri!.AbsolutePath == "/api/acars/flight/network");
    }

    [Fact]
    public async Task The_network_needs_a_started_flight_and_a_known_network()
    {
        var handler = Api();
        var flight = (await AcarsFlight.LoadAsync(handler.Client()))!;
        await LitoralApiClientTests.AssertThrowsAsync<InvalidOperationException>(() => flight.ChangeNetworkAsync("Ivao"));

        await flight.StartAsync("Vatsim", null);
        await LitoralApiClientTests.AssertThrowsAsync<ArgumentException>(() => flight.ChangeNetworkAsync("PilotEdge"));
    }
    
    [Fact]
    public async Task The_log_covers_the_start_the_flight_and_the_pirep()
    {
        var (flight, _) = await StartedAsync();
        foreach (var s in TestFlights.Script()) flight.OnSnapshot(s);
        await flight.CompleteAsync(null, null);

        var messages = flight.Log.Entries.Select(e => e.Message).ToList();
        Assert.StartsWith("Flight LTL1001 ", messages[0]);
        Assert.Contains("· VATSIM · MSFS 2024", messages[0]);
        Assert.Contains(messages, m => m.StartsWith("Touchdown"));
        Assert.Equal("PIREP filed · 30 min · landing -150 fpm · 12 points · Pending", messages[^1]);
    }

    [Fact]
    public async Task A_repeated_snapshot_is_logged_once()
    {
        var (flight, _) = await StartedAsync();
        var first = TestFlights.Script()[0];

        flight.OnSnapshot(first);
        flight.OnSnapshot(first);

        Assert.Single(flight.Log.Entries, e => e.Message.StartsWith("Recording started"));
    }

    [Fact]
    public async Task The_planned_arrival_is_used_when_none_is_typed()
    {
        var (flight, handler) = await StartedAsync();
        foreach (var s in TestFlights.Script()) flight.OnSnapshot(s);

        await flight.CompleteAsync("  ", null);

        using var body = JsonDocument.Parse(handler.Requests[^1].Body);
        Assert.Equal("SBFL", body.RootElement.GetProperty("arrivalIcao").GetString());
        Assert.False(body.RootElement.GetProperty("diversionReported").GetBoolean());
        Assert.Equal("kg", body.RootElement.GetProperty("fuelUnit").GetString());
    }

    [Fact]
    public async Task A_different_arrival_is_reported_as_a_diversion()
    {
        var (flight, handler) = await StartedAsync();
        foreach (var s in TestFlights.Script()) flight.OnSnapshot(s);

        await flight.CompleteAsync("sbjv", null);

        using var body = JsonDocument.Parse(handler.Requests[^1].Body);
        Assert.Equal("SBJV", body.RootElement.GetProperty("arrivalIcao").GetString());
        Assert.True(body.RootElement.GetProperty("diversionReported").GetBoolean());
    }

    [Fact]
    public async Task Parking_far_from_the_planned_arrival_looks_like_a_diversion()
    {
        var handler = Api();
        var flight = (await AcarsFlight.LoadAsync(handler.Client()))!;
        var options = TestFlights.Options() with { ArrivalLatitude = -26.2245, ArrivalLongitude = -48.7974 };
        foreach (var s in TestFlights.Script(options)) flight.OnSnapshot(s);

        Assert.True(flight.LooksLikeDiversion);
    }

    [Fact]
    public async Task Parking_at_the_planned_arrival_is_not_a_diversion()
    {
        var (flight, _) = await StartedAsync();
        foreach (var s in TestFlights.Script()) flight.OnSnapshot(s);

        Assert.False(flight.LooksLikeDiversion);
    }

    [Fact]
    public async Task Completing_twice_files_only_once()
    {
        var (flight, handler) = await StartedAsync();
        foreach (var s in TestFlights.Script()) flight.OnSnapshot(s);

        var first = await flight.CompleteAsync(null, null);
        var second = await flight.CompleteAsync(null, null);

        Assert.Same(first, second);
        Assert.Single(handler.Requests, r => r.Request.RequestUri!.AbsolutePath.EndsWith("/complete"));
    }

    [Fact]
    public async Task Completing_before_starting_is_refused()
    {
        var handler = Api();
        var flight = (await AcarsFlight.LoadAsync(handler.Client()))!;

        await LitoralApiClientTests.AssertThrowsAsync<InvalidOperationException>(() => flight.CompleteAsync(null, null));
    }
}