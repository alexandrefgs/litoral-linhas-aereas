using System.Net.Http.Headers;
using Litoral.Domain.Entities;
using Litoral.Domain.Legal;
using Litoral.Infrastructure.Applications;
using Litoral.Infrastructure.Auth;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;

namespace Litoral.Api.IntegrationTests.Infrastructure;

public sealed class LitoralApiFactory : WebApplicationFactory<Program>
{
    public const string RouteNumber = "1001";
    public const string Departure = "SBGR";
    public const string Arrival = "SBFL";
    public const string Password = "Tests-Password-123";
    private const string JwtKey = "litoral-integration-tests-signing-key-0123456789-abcdefghijklmnopqrstuvwxyz";

    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private int _pilots;
    private int _aircraft;
    private bool _containerDisposed;

    public Guid RouteId { get; private set; }

    public LitoralApiFactory()
    {
        Task.Run(async () =>
        {
            await _sql.StartAsync();
            await DbSeeder.SeedAsync(Services, importAirportCatalog: false);
            await SeedRouteAsync();
        }).GetAwaiter().GetResult();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connection = new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = "LitoralTests" };

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Litoral", connection.ConnectionString);
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("TwoFactor:RequiredForStaff", "false");

        builder.ConfigureTestServices(services =>
        {
            var jobs = services
                .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Assembly == typeof(DbSeeder).Assembly)
                .ToList();
            foreach (var job in jobs) services.Remove(job);
        });
    }

    public async Task<T> WithDbAsync<T>(Func<LitoralDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LitoralDbContext>();
        return await action(db);
    }

        public async Task<TestPilot> CreatePilotAsync()
    {
        var number = Interlocked.Increment(ref _pilots);
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var registrar = scope.ServiceProvider.GetRequiredService<PilotRegistrar>();
        var tokens = scope.ServiceProvider.GetRequiredService<JwtTokenGenerator>();

        var email = $"pilot{number}@tests.litoral";
        var user = new ApplicationUser { UserName = email, Email = email };
        var created = await users.CreateAsync(user, Password);
        if (!created.Succeeded)
            throw new InvalidOperationException(string.Join(" ", created.Errors.Select(e => e.Description)));

        var pilot = await registrar.CreateAsync(user, "Test", $"Pilot{number}", Departure, LegalTerms.CurrentVersion, CancellationToken.None);
        var (token, _) = tokens.Generate(user, pilot, []);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new TestPilot(pilot.Id, client);
    }

    public Task<Guid> CreateAircraftAsync() => WithDbAsync(async db =>
    {
        var index = Interlocked.Increment(ref _aircraft) - 1;
        var registration = $"PR-T{(char)('A' + index / 26 % 16)}{(char)('A' + index % 26)}";
        var type = await db.AircraftTypes.SingleAsync(t => t.IcaoCode == "B738" && !t.IsCargo);
        var airport = await db.Airports.SingleAsync(a => a.Icao == Departure);

        var aircraft = new Aircraft(registration, type, airport);
        db.Aircraft.Add(aircraft);
        await db.SaveChangesAsync();
        return aircraft.Id;
    });

    private async Task SeedRouteAsync()
    {
        RouteId = await WithDbAsync(async db =>
        {
            var type = await db.AircraftTypes.SingleAsync(t => t.IcaoCode == "B738" && !t.IsCargo);
            var departure = await db.Airports.SingleAsync(a => a.Icao == Departure);
            var arrival = await db.Airports.SingleAsync(a => a.Icao == Arrival);

            var route = new Route(int.Parse(RouteNumber), departure, arrival, [type], 70);
            db.Routes.Add(route);
            await db.SaveChangesAsync();
            return route.Id;
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await DisposeContainerAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) Task.Run(DisposeContainerAsync).GetAwaiter().GetResult();
    }

    private async Task DisposeContainerAsync()
    {
        if (_containerDisposed) return;
        _containerDisposed = true;
        await _sql.DisposeAsync();
    }
}

public sealed record TestPilot(Guid PilotId, HttpClient Client);