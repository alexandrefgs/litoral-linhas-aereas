using System.Net;
using System.Text.Json;
using Litoral.Acars.Core.Api;

namespace Litoral.Acars.Tests.Api;

public class LitoralApiClientTests
{
    [Fact]
    public async Task Every_request_carries_the_acars_key()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await handler.Client(" ltl_abc ").GetActiveFlightAsync();

        var (request, _) = Assert.Single(handler.Requests);
        Assert.Equal("ltl_abc", Assert.Single(request.Headers.GetValues(LitoralApiClient.KeyHeader)));
        Assert.Equal("https://litoral.test/api/acars/flight", request.RequestUri!.ToString());
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task No_content_means_there_is_nothing_open()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        Assert.Null(await handler.Client().GetBookingAsync());
        Assert.Null(await handler.Client().GetActiveFlightAsync());
    }

    [Fact]
    public async Task The_booking_is_read_from_the_api()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json("""
            {"bookingId":"7d3c1a0e-0000-0000-0000-000000000001","callsign":"LTL1001","departureIcao":"SBGR","departureLatitude":-23.4356,"departureLongitude":-46.4731,"arrivalIcao":"SBFL",
             "arrivalLatitude":-27.67,"arrivalLongitude":-48.55,"registration":"PR-TAA","aircraftTypeIcao":"B738","aircraftTypeName":"Boeing 737-800"}
            """));

        var booking = await handler.Client().GetBookingAsync();

        Assert.Equal("LTL1001", booking!.Callsign);
        Assert.Equal("B738", booking.AircraftTypeIcao);
        Assert.Equal(-23.4356, booking.DepartureLatitude);
        Assert.Equal(-27.67, booking.ArrivalLatitude);
        Assert.Equal("/api/acars/booking", handler.Requests[0].Request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Requests_are_sent_as_camel_case_json()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json("""{"id":"7d3c1a0e-0000-0000-0000-000000000002","bookingId":"7d3c1a0e-0000-0000-0000-000000000001","callsign":"LTL1001","network":"Vatsim","status":"Active","startedAt":"2026-11-19T12:00:00Z"}"""));

        var session = await handler.Client().StartFlightAsync(new StartFlightRequest("Vatsim", "MSFS 2024", "Litoral ACARS 0.1"));

        using var body = JsonDocument.Parse(handler.Requests[0].Body);
        Assert.Equal("Vatsim", body.RootElement.GetProperty("network").GetString());
        Assert.Equal("MSFS 2024", body.RootElement.GetProperty("simulator").GetString());
        Assert.Equal("Active", session.Status);
    }

    [Fact]
    public async Task Positions_go_in_one_batch()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json("2"));
        var at = TestFlights.Start;
        PositionReport P(int s) => new(at.AddSeconds(s), -23.4, -46.4, 1000, 980, 160, 165, 90, 1500, 5, true, false, "Climb");

        var added = await handler.Client().SendPositionsAsync([P(0), P(1)]);

        Assert.Equal(2, added);
        using var body = JsonDocument.Parse(handler.Requests[0].Body);
        Assert.Equal(2, body.RootElement.GetProperty("positions").GetArrayLength());
    }

    [Fact]
    public async Task An_empty_batch_is_not_sent()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json("0"));

        Assert.Equal(0, await handler.Client().SendPositionsAsync([]));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task More_than_500_positions_are_refused_before_sending()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json("0"));
        var many = Enumerable.Range(0, 501).Select(i => new PositionReport(TestFlights.Start.AddSeconds(i), 0, 0, 0, null, null, 0, 0, null, null, null, null, null)).ToList();

        await AssertThrowsAsync<ArgumentException>(() => handler.Client().SendPositionsAsync(many));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task The_server_message_is_shown_on_errors()
    {
        var handler = new FakeHandler(_ => FakeHandler.Json("""{"status":409,"title":"You don't have a flight in progress."}""", HttpStatusCode.Conflict));

        var ex = await AssertThrowsAsync<LitoralApiException>(() => handler.Client().SendPositionsAsync([new(TestFlights.Start, 0, 0, 0, null, null, 0, 0, null, null, null, null, null)]));

        Assert.Equal(HttpStatusCode.Conflict, ex.Status);
        Assert.Equal("You don't have a flight in progress.", ex.Message);
    }

    [Fact]
    public async Task A_refused_key_explains_what_to_do()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var ex = await AssertThrowsAsync<LitoralApiException>(() => handler.Client().GetBookingAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, ex.Status);
        Assert.Contains("ACARS key", ex.Message);
    }

    [Fact]
    public async Task A_body_that_is_not_json_still_gives_a_message()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>bad gateway</html>") });

        var ex = await AssertThrowsAsync<LitoralApiException>(() => handler.Client().GetBookingAsync());

        Assert.Equal("The server answered 502.", ex.Message);
    }

    internal static async Task<T> AssertThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
        }
        catch (T ex)
        {
            return ex;
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}