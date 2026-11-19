using System.Net;
using System.Net.Http.Json;
using Litoral.Acars.Core.Api;
using Litoral.Acars.Core.Flight;
using Litoral.Acars.Core.Map;
using Litoral.Acars.Core.Sim;
using Litoral.Api.IntegrationTests.Infrastructure;
using Litoral.Application.Bookings;
using Litoral.Application.Pilots;
using Litoral.Domain.Enums;
using Litoral.Domain.Scoring;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Api.IntegrationTests.Acars;

[Collection(ApiCollection.Name)]
public class AcarsClientTests(LitoralApiFactory api)
{
    [Fact]
    public async Task Without_a_booking_the_acars_has_nothing_to_load()
    {
        var pilot = await api.CreatePilotAsync();
        var client = await AcarsClientAsync(pilot);

        Assert.Null(await client.GetBookingAsync());
        Assert.Null(await AcarsFlight.LoadAsync(client));
    }

    [Fact]
    public async Task The_booking_endpoint_describes_the_flight_to_fly()
    {
        var pilot = await BookAsync();
        var client = await AcarsClientAsync(pilot);

        var booking = await client.GetBookingAsync();

        Assert.NotNull(booking);
        Assert.Equal(LitoralApiFactory.Departure, booking!.DepartureIcao);
        Assert.Equal(LitoralApiFactory.Arrival, booking.ArrivalIcao);
        Assert.Equal("B738", booking.AircraftTypeIcao);
        Assert.StartsWith("PR-T", booking.Registration);
        Assert.False(string.IsNullOrWhiteSpace(booking.Callsign));
        Assert.InRange(booking.DepartureLatitude, -24, -23);
        Assert.InRange(booking.ArrivalLatitude, -28, -27);
    }

    [Fact]
    public async Task The_booking_endpoint_also_accepts_the_web_login()
    {
        var pilot = await BookAsync();

        var response = await pilot.Client.GetAsync("/api/acars/booking");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_flight_flown_by_the_acars_client_files_a_pirep_with_its_track()
    {
        var pilot = await BookAsync();
        var flight = (await AcarsFlight.LoadAsync(await AcarsClientAsync(pilot)))!;
        await flight.StartAsync("Vatsim", "MSFS 2024");

        var sent = 0;
        foreach (var snapshot in Script(flight.Booking))
        {
            flight.OnSnapshot(snapshot);
            if (flight.Recorder.PendingPositions >= 100) sent += await flight.FlushAsync();
        }
        var pending = flight.Recorder.PendingPositions;
        var pirep = await flight.CompleteAsync(null, "Flown by the ACARS client");

        Assert.Equal(LitoralApiFactory.Arrival, pirep.Summary.ArrivalIcao);
        Assert.Equal(-150, pirep.Summary.LandingRateFpm);

        var saved = await api.WithDbAsync(db => db.Pireps.Include(p => p.Items).SingleAsync(p => p.Id == pirep.Summary.Id));
        Assert.Equal(10000, saved.CruiseAltitudeFt);
        Assert.True(saved.FuelUsedKg > 0);
        Assert.DoesNotContain(saved.Items, i => i.Check is ScoringCheck.TakeoffFlapsNotSet or ScoringCheck.LandingFlapsNotSet or ScoringCheck.TailStrike);
        Assert.DoesNotContain(saved.Items, i => i.Check is ScoringCheck.FuelInPounds or ScoringCheck.IncompleteData or ScoringCheck.UnreportedDiversion);

        var points = await api.WithDbAsync(db => db.FlightTrackPoints.CountAsync(p => p.SessionId == flight.Session!.Id));
        Assert.Equal(sent + pending, points);
    }

    [Fact]
    public async Task Taking_off_without_flaps_is_caught_through_the_acars_client()
    {
        var pilot = await BookAsync();
        var flight = (await AcarsFlight.LoadAsync(await AcarsClientAsync(pilot)))!;
        await flight.StartAsync("Vatsim", "MSFS 2024");

        foreach (var snapshot in Script(flight.Booking, takeoffFlaps: 0)) flight.OnSnapshot(snapshot);
        var pirep = await flight.CompleteAsync(null, null);

        var saved = await api.WithDbAsync(db => db.Pireps.Include(p => p.Items).SingleAsync(p => p.Id == pirep.Summary.Id));
        Assert.Contains(saved.Items, i => i.Check == ScoringCheck.TakeoffFlapsNotSet);
        Assert.True(pirep.Summary.HasRejection);
    }

    [Fact]
    public async Task A_pause_recorded_by_the_acars_client_reaches_the_pirep()
    {
        var pilot = await BookAsync();
        var flight = (await AcarsFlight.LoadAsync(await AcarsClientAsync(pilot)))!;
        await flight.StartAsync("Vatsim", "MSFS 2024");

        var paused = 0;
        foreach (var snapshot in Script(flight.Booking))
        {
            var cruising = !snapshot.OnGround && snapshot.VerticalSpeedFpm == 0;
            flight.OnSnapshot(cruising && paused++ < 90 ? snapshot with { Paused = true } : snapshot);
        }
        var pirep = await flight.CompleteAsync(null, null);

        var saved = await api.WithDbAsync(db => db.Pireps.Include(p => p.Events).SingleAsync(p => p.Id == pirep.Summary.Id));
        Assert.Contains(saved.Events, e => e.Type == FlightEventType.Pause && e.DurationSeconds == 90);
    }

        [Fact]
    public async Task The_briefing_through_the_acars_key_needs_an_open_flight()
    {
        var pilot = await api.CreatePilotAsync();
        var client = await AcarsClientAsync(pilot);

        var ex = await Assert.ThrowsAsync<LitoralApiException>(() => client.GetBriefingAsync());

        Assert.Equal(HttpStatusCode.NotFound, ex.Status);
    }

    [Fact]
    public async Task The_ofp_through_the_acars_key_needs_a_simbrief_username()
    {
        var pilot = await BookAsync();
        var client = await AcarsClientAsync(pilot);

        var ex = await Assert.ThrowsAsync<LitoralApiException>(() => client.GetOfpAsync());

        Assert.Equal(HttpStatusCode.BadRequest, ex.Status);
        Assert.Contains("SimBrief", ex.Message);
    }

    [Fact]
    public async Task A_started_flight_shows_on_the_live_map_through_the_acars_key()
    {
        var pilot = await BookAsync();
        var client = await AcarsClientAsync(pilot);
        var flight = (await AcarsFlight.LoadAsync(client))!;
        await flight.StartAsync("Ivao", "MSFS 2024");
        await client.SendPositionsAsync(
            [new PositionReport(DateTime.UtcNow, -25.0, -48.0, 35000, 34900, 280, 450, 30, 0, 0, false, false, "Cruise")]);

        var live = await client.GetLiveFlightsAsync();

        var mine = Assert.Single(live, f => f.Id == flight.Session!.Id);
        Assert.Equal(35000, mine.AltitudeFt);
        Assert.Equal(LitoralApiFactory.Arrival, mine.Arrival.Icao);
        Assert.Contains(flight.Booking.Callsign, LiveMapMessages.Flights(live, flight.Booking.Callsign));
    }

    private static List<SimSnapshot> Script(AcarsBooking booking, int takeoffFlaps = 5)
    {
        var options = new ScriptedFlightOptions(booking.AircraftTypeIcao, booking.DepartureLatitude, booking.DepartureLongitude,
            booking.ArrivalLatitude, booking.ArrivalLongitude, TakeoffFlaps: takeoffFlaps);
        return ScriptedFlight.Generate(DateTime.UtcNow.AddMinutes(-40), options).ToList();
    }

    private async Task<LitoralApiClient> AcarsClientAsync(TestPilot pilot)
    {
        var response = await pilot.Client.PostAsJsonAsync("/api/pilots/me/acars-keys", new CreateAcarsKeyRequest("Simulator PC"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CreatedAcarsKeyDto>())!;

        return new LitoralApiClient(LitoralApiClient.CreateHttp(api.Server.BaseAddress, created.Secret, api.Server.CreateHandler()));
    }

    private async Task<TestPilot> BookAsync()
    {
        var pilot = await api.CreatePilotAsync();
        var aircraftId = await api.CreateAircraftAsync();

        var response = await pilot.Client.PostAsJsonAsync("/api/bookings/custom", new BookFlightRequest(api.RouteId, aircraftId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return pilot;
    }
}