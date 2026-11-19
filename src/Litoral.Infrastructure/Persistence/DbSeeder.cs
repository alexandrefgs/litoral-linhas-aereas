using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Staff;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Litoral.Infrastructure.Reference;
using Litoral.Domain.Scoring;

namespace Litoral.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, bool importAirportCatalog = true)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LitoralDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        await SeedStaffRolesAsync(roleManager);
        await SeedRanksAsync(db);
        await SeedAircraftTypesAsync(db);
        await SeedAirportsAndHubsAsync(db);
        await SeedScoringRulesAsync(db);
        await SeedRejectionPenaltiesAsync(db);
        await MelCatalogSeed.SeedAsync(db);
        await Boeing777Seed.SeedAsync(db);
        await SeedDeliveryAirportsAsync(db);

        await db.SaveChangesAsync();
        if (importAirportCatalog)
        await scope.ServiceProvider.GetRequiredService<AirportCatalogImporter>().ImportIfEmptyAsync();
    }  

    private static async Task SeedDeliveryAirportsAsync(LitoralDbContext db)
    {
        var boeing = await db.AircraftTypes
            .Where(t => t.DeliveryAirportIcao == null && t.Name.StartsWith("Boeing"))
            .ToListAsync();

        foreach (var type in boeing)
            type.SetDeliveryAirport("KBFI");
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

    private static async Task SeedScoringRulesAsync(LitoralDbContext db)
    {
        if (await db.ScoringRules.AnyAsync()) return;

        const ScoringOutcome pts = ScoringOutcome.Points;
        const ScoringOutcome reject = ScoringOutcome.Reject;
        const ScoringMode once = ScoringMode.Once;
        const ScoringMode each = ScoringMode.PerOccurrence;
        const string ng = "B736,B737,B738,B739";

        ScoringRule[] rules =
        [
            new(ScoringCategory.FlightTime, ScoringCheck.BlockTime, "Below 1 hour", pts, 15, once, null, 59, order: 1),
            new(ScoringCategory.FlightTime, ScoringCheck.BlockTime, "Between 1 and 2 hours", pts, 30, once, 60, 119, order: 2),
            new(ScoringCategory.FlightTime, ScoringCheck.BlockTime, "Between 2 and 3 hours", pts, 50, once, 120, 179, order: 3),
            new(ScoringCategory.FlightTime, ScoringCheck.BlockTime, "Between 3 and 4 hours", pts, 70, once, 180, 239, order: 4),
            new(ScoringCategory.FlightTime, ScoringCheck.BlockTime, "Between 4 and 5 hours", pts, 80, once, 240, 299, order: 5),
            new(ScoringCategory.FlightTime, ScoringCheck.BlockTime, "Between 5 and 6 hours", pts, 90, once, 300, 359, order: 6),
            new(ScoringCategory.FlightTime, ScoringCheck.BlockTime, "Above 6 hours", pts, 100, once, 360, null, order: 7),

            new(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, "0 ft/min or higher", pts, 0, once, null, 0, order: 1),
            new(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, "1 – 50 ft/min", pts, 40, once, 1, 50, order: 2),
            new(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, "51 – 100 ft/min", pts, 50, once, 51, 100, order: 3),
            new(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, "101 – 150 ft/min", pts, 60, once, 101, 150, order: 4),
            new(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, "151 – 200 ft/min", pts, 30, once, 151, 200, order: 5),
            new(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, "201 – 300 ft/min", pts, 20, once, 201, 300, order: 6),
            new(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, "301 – 400 ft/min", pts, 10, once, 301, 400, order: 7),
            new(ScoringCategory.TouchdownRate, ScoringCheck.TouchdownRate, "401 – 500 ft/min", pts, -50, once, 401, 500, order: 8),

            new(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, "Flaps 1 above 250 kt", reject, 0, once, null, 250, "1", order: 1),
            new(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, "Flaps 2 above 250 kt", reject, 0, once, null, 250, "2", order: 2),
            new(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, "Flaps 5 above 250 kt", reject, 0, once, null, 250, "5", order: 3),
            new(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, "Flaps 10 above 210 kt", reject, 0, once, null, 210, "10", order: 4),
            new(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, "Flaps 15 above 200 kt", reject, 0, once, null, 200, "15", order: 5),
            new(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, "Flaps 25 above 190 kt", reject, 0, once, null, 190, "25", order: 6),
            new(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, "Flaps 30 above 175 kt", reject, 0, once, null, 175, "30", order: 7),
            new(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, "Flaps 40 above 162 kt (737 NG)", reject, 0, once, null, 162, "40", ng, order: 8),
            new(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, "Flaps 40 above 166 kt (737 MAX)", reject, 0, once, null, 166, "40", "B38M", order: 9),

            new(ScoringCategory.Configuration, ScoringCheck.TakeoffFlapsSet, "Flaps set for takeoff", pts, 5, once, text: "1,5,10,15,25", order: 1),
            new(ScoringCategory.Configuration, ScoringCheck.LandingFlapsSet, "Flaps set for landing", pts, 5, once, text: "30,40", order: 2),
            new(ScoringCategory.Configuration, ScoringCheck.TakeoffFlapsNotSet, "Flaps not set for takeoff (1, 5, 10, 15 or 25)", reject, 0, once, text: "1,5,10,15,25", order: 3),
            new(ScoringCategory.Configuration, ScoringCheck.LandingFlapsNotSet, "Flaps not set for landing (30 or 40)", reject, 0, once, text: "30,40", order: 4),

            new(ScoringCategory.Takeoff, ScoringCheck.TailStrike, "737-700: liftoff with pitch 14° or above (tail strike)", reject, 0, once, 14, aircraftTypes: "B736,B737", order: 1),
            new(ScoringCategory.Takeoff, ScoringCheck.TailStrike, "737-800, 737-900 and 737 MAX: liftoff with pitch 11° or above (tail strike)", reject, 0, once, 11, aircraftTypes: "B738,B739,B38M", order: 2),
            new(ScoringCategory.Takeoff, ScoringCheck.RotationSpeedAbove, "VR above 192 kt", reject, 0, once, max: 192, order: 3),
            new(ScoringCategory.Takeoff, ScoringCheck.RotationSpeedWithin, "VR below 192 kt", pts, 10, once, max: 192, order: 4),

            new(ScoringCategory.LandingGear, ScoringCheck.GearUpOverspeed, "Gear up above 235 kt", reject, 0, once, max: 235, order: 1),
            new(ScoringCategory.LandingGear, ScoringCheck.GearDownOverspeed, "Gear down above 270 kt", reject, 0, once, max: 270, order: 2),

            new(ScoringCategory.Overspeed, ScoringCheck.Overspeed, "Overspeed, per second", pts, -10, ScoringMode.PerSecond, graceSeconds: 5, order: 1),

            new(ScoringCategory.Simulator, ScoringCheck.Slew, "Slew detected", pts, -100, once, graceSeconds: 5, order: 1),
            new(ScoringCategory.Simulator, ScoringCheck.Stall, "Stall detected", pts, -150, once, graceSeconds: 5, order: 2),
            new(ScoringCategory.Simulator, ScoringCheck.Pause, "Pause detected", pts, -50, once, graceSeconds: 30, order: 3),
            new(ScoringCategory.Simulator, ScoringCheck.SimulationRate, "Simulation rate detected", pts, -600, once, order: 4),
            new(ScoringCategory.Simulator, ScoringCheck.Refuel, "Refuel detected", pts, -300, once, order: 5),

            new(ScoringCategory.Communications, ScoringCheck.NoActiveFrequency, "Not keeping an ATC or UNICOM frequency active", pts, -100, once, order: 1),

            new(ScoringCategory.FlightData, ScoringCheck.IncompleteData, "Incomplete data or landing in an undefined location", pts, -300, once, order: 1),

            new(ScoringCategory.Procedures, ScoringCheck.EngineStartOrder, "Starting engine #2 first", pts, 10, once, 2, order: 1),
            new(ScoringCategory.Procedures, ScoringCheck.EngineStartOrder, "Starting engine #1 first", pts, 5, once, 1, order: 2),
            new(ScoringCategory.Procedures, ScoringCheck.TaxiInAbove, "Taxi in above 2:30 min (SBRJ and SBSP not monitored)", pts, 10, once, 150, text: "SBRJ,SBSP", order: 3),
            new(ScoringCategory.Procedures, ScoringCheck.TaxiInBelow, "Taxi in below 2:30 min (SBRJ and SBSP not monitored)", pts, -5, once, 150, text: "SBRJ,SBSP", order: 4),
            new(ScoringCategory.Procedures, ScoringCheck.EnginesOffAtLog, "Engines shut down before sending the log", pts, 10, once, order: 5),
            new(ScoringCategory.Procedures, ScoringCheck.EnginesRunningAtLog, "Engines running when sending the log", pts, -50, once, order: 6),
            new(ScoringCategory.Procedures, ScoringCheck.TaxiSpeed, "Taxi speed above 30 kt", pts, -50, once, max: 30, order: 7),

            new(ScoringCategory.Rejection, ScoringCheck.Offline, "Offline: flight kept as OFFLINE in the ACARS", reject, 0, once, order: 1),
            new(ScoringCategory.Rejection, ScoringCheck.Crash, "Crash: aircraft crashed", reject, 0, once, order: 2),
            new(ScoringCategory.Rejection, ScoringCheck.UndeclaredAlternate, "Alternate: diverting without ALT, ALT1, ALT-1, ALT2 or ALT-2 and a justification in the log comments", reject, 0, once, text: "ALT,ALT1,ALT-1,ALT2,ALT-2", order: 3),
            new(ScoringCategory.Rejection, ScoringCheck.OverspeedTotal, "Overspeed: total overspeed penalty equal to or above 300 points", reject, 0, once, 300, order: 4),
            new(ScoringCategory.Rejection, ScoringCheck.MissingRemarks, "Remarks: OPR/LITORAL and PIC/ missing from the flight plan remarks", reject, 0, once, text: "OPR/LITORAL PIC/", order: 5),
            new(ScoringCategory.Rejection, ScoringCheck.EarlyFlapRetraction, "Takeoff flaps: flaps retracted below 800 ft AFE", reject, 0, once, max: 800, order: 6),
            new(ScoringCategory.Rejection, ScoringCheck.UnstableApproach, "Approach: aircraft not configured for landing at 1000 ft AFE", reject, 0, once, 1000, order: 7),
            new(ScoringCategory.Rejection, ScoringCheck.TouchdownRate, "Touchdown rate: above 500 ft/min", reject, 0, once, 501, null, order: 8),
            new(ScoringCategory.Rejection, ScoringCheck.LowFuelAtPushback, "Fuel: below 4,400 kg at pushback", reject, 0, once, 4400, order: 9),
            new(ScoringCategory.Rejection, ScoringCheck.FuelInPounds, "Fuel: unit set to pounds in the ACARS", reject, 0, once, order: 10),
            new(ScoringCategory.Rejection, ScoringCheck.LowOnlinePercent, "Networks: flights below 80% online", reject, 0, once, 80, order: 11),
            new(ScoringCategory.Rejection, ScoringCheck.SimultaneousNetworks, "Networks: simultaneous flights on two networks or in another VA (subject to account removal)", reject, 0, once, order: 12),
            new(ScoringCategory.Rejection, ScoringCheck.UnregisteredNetworkId, "ACARS: log sent on a network whose ID is not registered on the VA website", reject, 0, once, order: 13),
            new(ScoringCategory.Rejection, ScoringCheck.UnreportedDiversion, "ACARS: diversion not reported, or incomplete flight sent", reject, 0, once, order: 14),
            new(ScoringCategory.Rejection, ScoringCheck.SopDeviation, "ACARS: operations not following the Litoral standard procedures", reject, 0, once, order: 15),

            new(ScoringCategory.SpecialFlights, ScoringCheck.SpecialFlightDomestic, "Domestic leg", pts, 300, once, order: 1),
            new(ScoringCategory.SpecialFlights, ScoringCheck.SpecialFlightInternational, "International leg", pts, 500, once, order: 2),

            new(ScoringCategory.NetworkEvents, ScoringCheck.NetworkEventVatsim, "VATSIM", pts, 500, once, order: 1),
            new(ScoringCategory.NetworkEvents, ScoringCheck.NetworkEventIvao, "IVAO", pts, 500, once, order: 2),

            new(ScoringCategory.LitoralEvents, ScoringCheck.LitoralEventDomestic, "Domestic leg", pts, 300, once, order: 1),
            new(ScoringCategory.LitoralEvents, ScoringCheck.LitoralEventInternational, "International leg", pts, 500, once, order: 2),

            new(ScoringCategory.CrewDuty, ScoringCheck.CrewDuty, "Per flight rostered", pts, 10, once, order: 1),

            new(ScoringCategory.Cancellation, ScoringCheck.BookingCancelled, "Per flight in the booking", pts, -500, each, order: 1),

            new(ScoringCategory.Inactivity, ScoringCheck.Inactivity, "Daily, after 40 days without flights", pts, -50, ScoringMode.PerDay, 40, order: 1),

            new(ScoringCategory.PilotsOfTheMonth, ScoringCheck.PilotOfTheMonth, "1st place", pts, 800, once, 1, order: 1),
            new(ScoringCategory.PilotsOfTheMonth, ScoringCheck.PilotOfTheMonth, "2nd place", pts, 600, once, 2, order: 2),
            new(ScoringCategory.PilotsOfTheMonth, ScoringCheck.PilotOfTheMonth, "3rd place", pts, 400, once, 3, order: 3),

            new(ScoringCategory.AccountLoss, ScoringCheck.AccountLoss, "Balance equal to or below -10,000 points", pts, 0, once, max: -10000, order: 1)
        ];

        db.ScoringRules.AddRange(rules);
    }
    private static async Task SeedRejectionPenaltiesAsync(LitoralDbContext db)
    {
        if (await db.ScoringRules.AnyAsync(r => r.Category == ScoringCategory.RejectionPenalties)) return;

        const ScoringCategory category = ScoringCategory.RejectionPenalties;
        const ScoringOutcome pts = ScoringOutcome.Points;
        const ScoringMode once = ScoringMode.Once;

        db.ScoringRules.AddRange(
            new ScoringRule(category, ScoringCheck.MonthlyRejection, "1st rejection in the month", pts, -1000, once, 1, order: 1),
            new ScoringRule(category, ScoringCheck.MonthlyRejection, "2nd rejection in the month", pts, -1500, once, 2, order: 2),
            new ScoringRule(category, ScoringCheck.MonthlyRejection, "3rd rejection in the month", pts, -2000, once, 3, order: 3),
            new ScoringRule(category, ScoringCheck.MonthlyRejection, "4th rejection in the month", pts, -10000, once, 4, order: 4),
            new ScoringRule(category, ScoringCheck.RejectionDeactivation, "4th rejection in the month: account deactivated", ScoringOutcome.Reject, 0, once, 4, order: 5));
    }
}