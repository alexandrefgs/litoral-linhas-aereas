using System.Net;
using System.Net.Http.Json;
using Litoral.Api.IntegrationTests.Infrastructure;
using Litoral.Application.Bookings;
using Litoral.Application.Flights;
using Litoral.Application.Pilots;
using Litoral.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Api.IntegrationTests.Acars;

[Collection(ApiCollection.Name)]
public class AcarsKeyTests(LitoralApiFactory api)
{
    private const string KeysUrl = "/api/pilots/me/acars-keys";

    [Fact]
    public async Task A_new_key_signs_in_to_the_acars()
    {
        var pilot = await api.CreatePilotAsync();
        var created = await CreateKeyAsync(pilot, "Home PC");

        var response = await KeyClient(created.Secret).GetAsync("/api/acars/flight");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task The_key_is_shown_once_and_only_its_hash_is_stored()
    {
        var pilot = await api.CreatePilotAsync();
        var created = await CreateKeyAsync(pilot, "Home PC");

        Assert.StartsWith(AcarsKey.KeyPrefix, created.Secret);
        Assert.Equal(created.Secret[..AcarsKey.PrefixLength], created.Key.Prefix);

        var stored = await api.WithDbAsync(db => db.AcarsKeys.SingleAsync(k => k.Id == created.Key.Id));
        Assert.Equal(AcarsKey.HashOf(created.Secret), stored.Hash);
        Assert.NotEqual(created.Secret, stored.Hash);

        var list = await pilot.Client.GetFromJsonAsync<List<AcarsKeyDto>>(KeysUrl);
        var item = Assert.Single(list!);
        Assert.Equal("Home PC", item.Name);
    }

    [Theory]
    [InlineData("/api/bookings")]
    [InlineData("/api/pilots/me/settings")]
    [InlineData("/api/pilots/me/acars-keys")]
    [InlineData("/api/notifications")]
    public async Task The_key_does_not_open_anything_outside_the_acars(string url)
    {
        var pilot = await api.CreatePilotAsync();
        var created = await CreateKeyAsync(pilot, "Home PC");

        var response = await KeyClient(created.Secret).GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("ltl_0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("not-a-key")]
    [InlineData("")]
    public async Task Unknown_or_malformed_keys_are_refused(string key)
    {
        var response = await KeyClient(key).GetAsync("/api/acars/flight");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_revoked_key_stops_working()
    {
        var pilot = await api.CreatePilotAsync();
        var created = await CreateKeyAsync(pilot, "Home PC");

        var revoke = await pilot.Client.DeleteAsync($"{KeysUrl}/{created.Key.Id}");
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);

        var response = await KeyClient(created.Secret).GetAsync("/api/acars/flight");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty((await pilot.Client.GetFromJsonAsync<List<AcarsKeyDto>>(KeysUrl))!);
    }

    [Fact]
    public async Task A_pilot_cannot_revoke_another_pilots_key()
    {
        var owner = await api.CreatePilotAsync();
        var other = await api.CreatePilotAsync();
        var created = await CreateKeyAsync(owner, "Home PC");

        var response = await other.Client.DeleteAsync($"{KeysUrl}/{created.Key.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await KeyClient(created.Secret).GetAsync("/api/acars/flight")).StatusCode);
    }

    [Fact]
    public async Task A_pilot_can_have_up_to_three_keys()
    {
        var pilot = await api.CreatePilotAsync();
        for (var i = 1; i <= AcarsKey.MaxPerPilot; i++)
            await CreateKeyAsync(pilot, $"PC {i}");

        var response = await pilot.Client.PostAsJsonAsync(KeysUrl, new CreateAcarsKeyRequest("One more"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_key_needs_a_name()
    {
        var pilot = await api.CreatePilotAsync();

        var response = await pilot.Client.PostAsJsonAsync(KeysUrl, new CreateAcarsKeyRequest("  "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Changing_the_password_revokes_every_key()
    {
        var pilot = await api.CreatePilotAsync();
        var first = await CreateKeyAsync(pilot, "Home PC");
        var second = await CreateKeyAsync(pilot, "Laptop");

        var change = await pilot.Client.PutAsJsonAsync("/api/pilots/me/password",
            new ChangePasswordRequest(LitoralApiFactory.Password, "Another-Password-456"));
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await KeyClient(first.Secret).GetAsync("/api/acars/flight")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await KeyClient(second.Secret).GetAsync("/api/acars/flight")).StatusCode);
    }

    [Fact]
    public async Task A_deactivated_pilot_cannot_use_the_key()
    {
        var pilot = await api.CreatePilotAsync();
        var created = await CreateKeyAsync(pilot, "Home PC");

        await api.WithDbAsync(async db =>
        {
            var entity = await db.Pilots.SingleAsync(p => p.Id == pilot.PilotId);
            entity.Deactivate();
            return await db.SaveChangesAsync();
        });

        var response = await KeyClient(created.Secret).GetAsync("/api/acars/flight");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_whole_flight_can_be_flown_with_the_key_only()
    {
        var pilot = await api.CreatePilotAsync();
        var aircraftId = await api.CreateAircraftAsync();
        var booked = await pilot.Client.PostAsJsonAsync("/api/bookings/custom", new BookFlightRequest(api.RouteId, aircraftId));
        Assert.Equal(HttpStatusCode.OK, booked.StatusCode);

        var created = await CreateKeyAsync(pilot, "Home PC");
        var acars = KeyClient(created.Secret);
        var now = DateTime.UtcNow;

        Assert.Equal(HttpStatusCode.OK, (await acars.PostAsJsonAsync("/api/acars/flight", AcarsTestFlight.Start())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await acars.PostAsJsonAsync("/api/acars/flight/positions", AcarsTestFlight.Climb(now.AddMinutes(-60)))).StatusCode);

        var complete = await acars.PostAsJsonAsync("/api/acars/flight/complete", AcarsTestFlight.Complete(now));
        Assert.True(complete.IsSuccessStatusCode, await complete.Content.ReadAsStringAsync());
        var pirep = await complete.Content.ReadFromJsonAsync<PirepDto>();
        Assert.Equal(LitoralApiFactory.Arrival, pirep!.Summary.ArrivalIcao);

        var key = await api.WithDbAsync(db => db.AcarsKeys.SingleAsync(k => k.Id == created.Key.Id));
        Assert.NotNull(key.LastUsedAt);

        var logged = await api.WithDbAsync(db => db.AuditLogs.AnyAsync(l => l.Action == "ACARS key first used" && l.TargetId == pilot.PilotId.ToString()));
        Assert.True(logged);
    }

    private static async Task<CreatedAcarsKeyDto> CreateKeyAsync(TestPilot pilot, string name)
    {
        var response = await pilot.Client.PostAsJsonAsync(KeysUrl, new CreateAcarsKeyRequest(name));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatedAcarsKeyDto>())!;
    }

    private HttpClient KeyClient(string key)
    {
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Add("X-Acars-Key", key);
        return client;
    }
}