using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Staff;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Litoral.Infrastructure.Reference;

namespace Litoral.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LitoralDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        await SeedStaffRolesAsync(roleManager);
        await SeedRanksAsync(db);
        await SeedAircraftTypesAsync(db);
        await SeedAirportsAndHubsAsync(db);

        await db.SaveChangesAsync();
        await scope.ServiceProvider.GetRequiredService<AirportCatalogImporter>().ImportIfEmptyAsync();
    }

    private static async Task SeedStaffRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var position in StaffPositions.All)
        {
            if (!await roleManager.RoleExistsAsync(position.Code))
                await roleManager.CreateAsync(new IdentityRole(position.Code));
        }
    }

        private static async Task SeedRanksAsync(LitoralDbContext db)
    {
        (string Code, string Name, int Level, int Hours, int Points)[] ranks =
        [
            ("FO", "First Officer", 1, 0, 0),
            ("CMD", "Captain", 2, 500, 30000)
        ];

        var existing = await db.Ranks.ToListAsync();

        foreach (var r in ranks)
        {
            var rank = existing.FirstOrDefault(x => x.Code == r.Code);

            if (rank is null)
                db.Ranks.Add(new Rank(r.Code, r.Name, r.Level, r.Hours, r.Points));
            else
                rank.SetRequirements(r.Hours, r.Points);
        }
    }

        private static async Task SeedAircraftTypesAsync(LitoralDbContext db)
    {
        (string Icao, string Name, bool Cargo, int Range, string Prefix)[] types =
        [
            ("B738", "Boeing 737-800", false, 2900, "LA"),
            ("B738", "Boeing 737-800BCF", true, 2000, "LB"),
            ("B737", "Boeing 737-700", false, 3000, "LC"),
            ("B739", "Boeing 737-900ER", false, 2900, "LD"),
            ("B38M", "Boeing 737 MAX 8", false, 3500, "LE"),
            ("B736", "Boeing 737-600", false, 3000, "LF")
        ];

        var existing = await db.AircraftTypes.ToListAsync();

        foreach (var t in types)
        {
            var type = existing.FirstOrDefault(x => x.IcaoCode == t.Icao && x.IsCargo == t.Cargo);

            if (type is null && existing.Count == 0)
                db.AircraftTypes.Add(new AircraftType(t.Icao, t.Name, t.Cargo, t.Range, t.Prefix));
            else if (type is { RegistrationPrefix: null } && existing.All(x => x.RegistrationPrefix != t.Prefix))
                type.AssignRegistrationPrefix(t.Prefix);
        }
    }

    private static async Task SeedAirportsAndHubsAsync(LitoralDbContext db)
    {
        if (await db.Airports.AnyAsync()) return;

        const OperationType passenger = OperationType.Passenger;
        const OperationType mixed = OperationType.Passenger | OperationType.Cargo;

        var sbgr = new Airport("SBGR", "Aeroporto Internacional de São Paulo/Guarulhos", "São Paulo", "Brazil", -23.4356, -46.4731, passenger);
        var sbfl = new Airport("SBFL", "Aeroporto Internacional de Florianópolis - Hercílio Luz", "Florianópolis", "Brazil", -27.6703, -48.5525, passenger);
        var sbbr = new Airport("SBBR", "Aeroporto Internacional de Brasília", "Brasília", "Brazil", -15.8711, -47.9186, passenger);
        var sbeg = new Airport("SBEG", "Aeroporto Internacional Eduardo Gomes", "Manaus", "Brazil", -3.0386, -60.0497, mixed);
        var sbfz = new Airport("SBFZ", "Aeroporto Internacional Pinto Martins", "Fortaleza", "Brazil", -3.7763, -38.5326, mixed);
        var sbkp = new Airport("SBKP", "Aeroporto Internacional de Viracopos", "Campinas", "Brazil", -23.0074, -47.1345, OperationType.Cargo);

        db.Airports.AddRange(sbgr, sbfl, sbbr, sbeg, sbfz, sbkp);
        db.Hubs.AddRange(new Hub(sbgr), new Hub(sbfl), new Hub(sbbr), new Hub(sbeg), new Hub(sbfz), new Hub(sbkp));
    }
}