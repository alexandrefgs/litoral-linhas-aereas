using System.Net;
using System.Net.Http.Json;
using Litoral.Api.IntegrationTests.Infrastructure;
using Litoral.Application.Bookings;
using Litoral.Application.Flights;
using Litoral.Domain.Enums;
using Litoral.Domain.Scoring;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Api.IntegrationTests.Acars;

[Collection(ApiCollection.Name)]
public class AcarsFlowTests(LitoralApiFactory api)
{
    [Fact]
    public async Task Acars_endpoints_require_a_login()
    {
        var response = await api.CreateClient().GetAsync("/api/acars/flight");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Starting_without_a_booking_is_not_found()
    {
        var pilot = await api.CreatePilotAsync();

        var response = await pilot.Client.PostAsJsonAsync("/api/acars/flight", AcarsTestFlight.Start());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Positions_without_a_flight_in_progress_are_refused()
    {
        var pilot = await api.CreatePilotAsync();

        var response = await pilot.Client.PostAsJsonAsync("/api/acars/flight/positions", AcarsTestFlight.Climb(DateTime.UtcNow));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task An_invalid_network_is_refused()
    {
        var pilot = await BookAsync();

        var response = await pilot.Client.PostAsJsonAsync("/api/acars/flight", AcarsTestFlight.Start("PilotEdge"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Starting_twice_returns_the_same_session()
    {
        var pilot = await BookAsync();

        var first = await StartAsync(pilot);
        var second = await StartAsync(pilot);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Active", second.Status);
        Assert.Equal("Vatsim", second.Network);
    }

    [Fact]
    public async Task More_than_500_positions_in_one_batch_are_refused()
    {
        var pilot = await BookAsync();
        await StartAsync(pilot);

        var batch = AcarsTestFlight.Climb(DateTime.UtcNow, seconds: 501 * 5);
        var response = await pilot.Client.PostAsJsonAsync("/api/acars/flight/positions", batch);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Positions_feed_the_live_map()
    {
        var pilot = await BookAsync();
        var session = await StartAsync(pilot);

        var response = await pilot.Client.PostAsJsonAsync("/api/acars/flight/positions", AcarsTestFlight.Climb(DateTime.UtcNow.AddSeconds(-30)));
        Assert.Equal(7, await response.Content.ReadFromJsonAsync<int>());

        var live = await pilot.Client.GetFromJsonAsync<List<LiveFlightDto>>("/api/acars/live");
        Assert.Contains(live!, f => f.Id == session.Id && f.Callsign == session.Callsign);
    }

    [Fact]
    public async Task A_clean_flight_is_scored_and_auto_approved()
    {
        var pilot = await BookAsync();
        await StartAsync(pilot);
        var now = DateTime.UtcNow;
        await SendAsync(pilot, AcarsTestFlight.Climb(now.AddMinutes(-60)));

        var pirep = await CompleteAsync(pilot, AcarsTestFlight.Complete(now));

        var saved = await api.WithDbAsync(db => db.Pireps.Include(p => p.Items).SingleAsync(p => p.Id == pirep.Summary.Id));
        Assert.Equal(PirepStatus.Approved, saved.Status);
        Assert.False(saved.HasRejection);
        Assert.Equal(100, saved.Conformity);
        Assert.True(saved.ScorePoints > 0);
        Assert.Contains(saved.Items, i => i.Check == ScoringCheck.BlockTime);
        Assert.Contains(saved.Items, i => i.Check == ScoringCheck.TouchdownRate);

        var after = await api.WithDbAsync(db => db.Pilots.SingleAsync(p => p.Id == pilot.PilotId));
        Assert.Equal(LitoralApiFactory.Arrival, after.CurrentLocationIcao);
        Assert.Equal(saved.BlockMinutes, after.TotalFlightMinutes);
        Assert.Equal(saved.ScorePoints, after.Points);

        var booking = await api.WithDbAsync(db => db.Bookings.Include(b => b.Aircraft).ThenInclude(a => a.CurrentAirport).SingleAsync(b => b.Id == saved.BookingId));
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(LitoralApiFactory.Arrival, booking.Aircraft.CurrentAirport.Icao);
        Assert.Equal(AircraftStatus.Available, booking.Aircraft.Status);

        var session = await pilot.Client.GetAsync("/api/acars/flight");
        Assert.Equal(HttpStatusCode.NoContent, session.StatusCode);
    }

    [Fact]
    public async Task A_hard_landing_is_rejected_and_waits_for_review()
    {
        var pilot = await BookAsync();
        await StartAsync(pilot);

        var pirep = await CompleteAsync(pilot, AcarsTestFlight.Complete(DateTime.UtcNow, f => f.LandingRateFpm = -650));

        Assert.Equal("Pending", pirep.Summary.Status);
        Assert.True(pirep.Summary.HasRejection);
        Assert.Equal(0, pirep.Summary.Conformity);

        var after = await api.WithDbAsync(db => db.Pilots.SingleAsync(p => p.Id == pilot.PilotId));
        Assert.Equal(0, after.Points);
        Assert.Equal(LitoralApiFactory.Arrival, after.CurrentLocationIcao);
        Assert.Equal(pirep.Summary.BlockMinutes, after.TotalFlightMinutes);
    }

    [Fact]
    public async Task Flaps_overspeed_in_the_track_is_detected()
    {
        var pilot = await BookAsync();
        await StartAsync(pilot);
        var now = DateTime.UtcNow;
        await SendAsync(pilot, AcarsTestFlight.Climb(now.AddMinutes(-65), flaps: 5, ias: 265));

        var pirep = await CompleteAsync(pilot, AcarsTestFlight.Complete(now));

        Assert.Equal("Pending", pirep.Summary.Status);
        Assert.Contains(pirep.Items, i => i.Check == nameof(ScoringCheck.FlapsOverspeed) && i.Outcome == nameof(ScoringOutcome.Reject));
        Assert.Equal(7, pirep.Track.Count);
    }

    [Fact]
    public async Task Fuel_in_pounds_is_converted_and_flagged()
    {
        var pilot = await BookAsync();
        await StartAsync(pilot);

        var pirep = await CompleteAsync(pilot, AcarsTestFlight.Complete(DateTime.UtcNow, f =>
        {
            f.FuelUnit = "lbs";
            f.FuelAtPushback = 15000;
        }));

        Assert.Equal(6803.9m, pirep.FuelAtPushbackKg);
        Assert.Contains(pirep.Items, i => i.Check == nameof(ScoringCheck.FuelInPounds));
        Assert.Equal("Pending", pirep.Summary.Status);
    }

    [Fact]
    public async Task An_offline_flight_is_rejected()
    {
        var pilot = await BookAsync();
        await StartAsync(pilot, "Offline");

        var pirep = await CompleteAsync(pilot, AcarsTestFlight.Complete(DateTime.UtcNow));

        Assert.True(pirep.Summary.HasRejection);
        Assert.Contains(pirep.Items, i => i.Check == nameof(ScoringCheck.Offline));
    }

    [Fact]
    public async Task An_unreported_diversion_is_rejected_and_the_aircraft_stays_where_it_landed()
    {
        var pilot = await BookAsync();
        await StartAsync(pilot);

        var pirep = await CompleteAsync(pilot, AcarsTestFlight.Complete(DateTime.UtcNow, f => f.ArrivalIcao = "SBBR"));

        Assert.Equal("SBBR", pirep.Summary.ArrivalIcao);
        Assert.Equal(LitoralApiFactory.Arrival, pirep.Summary.PlannedArrivalIcao);
        Assert.Contains(pirep.Items, i => i.Check == nameof(ScoringCheck.UnreportedDiversion));

        var after = await api.WithDbAsync(db => db.Pilots.SingleAsync(p => p.Id == pilot.PilotId));
        Assert.Equal("SBBR", after.CurrentLocationIcao);
    }

    [Fact]
    public async Task A_flight_can_only_be_completed_once()
    {
        var pilot = await BookAsync();
        await StartAsync(pilot);
        await CompleteAsync(pilot, AcarsTestFlight.Complete(DateTime.UtcNow));

        var again = await pilot.Client.PostAsJsonAsync("/api/acars/flight/complete", AcarsTestFlight.Complete(DateTime.UtcNow));

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    private async Task<TestPilot> BookAsync()
    {
        var pilot = await api.CreatePilotAsync();
        var aircraftId = await api.CreateAircraftAsync();

        var response = await pilot.Client.PostAsJsonAsync("/api/bookings/custom", new BookFlightRequest(api.RouteId, aircraftId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return pilot;
    }

    private static async Task<FlightSessionDto> StartAsync(TestPilot pilot, string network = "Vatsim")
    {
        var response = await pilot.Client.PostAsJsonAsync("/api/acars/flight", AcarsTestFlight.Start(network));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FlightSessionDto>())!;
    }

    private static async Task SendAsync(TestPilot pilot, PositionBatchRequest batch)
    {
        var response = await pilot.Client.PostAsJsonAsync("/api/acars/flight/positions", batch);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<PirepDto> CompleteAsync(TestPilot pilot, CompleteFlightRequest request)
    {
        var response = await pilot.Client.PostAsJsonAsync("/api/acars/flight/complete", request);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PirepDto>())!;
    }
}