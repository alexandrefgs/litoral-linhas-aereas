using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Scoring;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Persistence;

public static class Boeing777Seed
{
    public const string Types737 = "B736,B737,B738,B739,B38M";
    public const string Types777 = "B77W,B77F";
    public const string DeliveryAirport = "KPAE";

    private const string SopDescription = "ACARS: operations not following the Litoral standard procedures";

    private static readonly ScoringCheck[] TypeSpecificChecks =
    [
        ScoringCheck.FlapsOverspeed,
        ScoringCheck.TakeoffFlapsSet,
        ScoringCheck.LandingFlapsSet,
        ScoringCheck.TakeoffFlapsNotSet,
        ScoringCheck.LandingFlapsNotSet,
        ScoringCheck.RotationSpeedAbove,
        ScoringCheck.RotationSpeedWithin,
        ScoringCheck.GearUpOverspeed,
        ScoringCheck.GearDownOverspeed,
        ScoringCheck.LowFuelAtPushback
    ];

    private static readonly string[] Mel737 = Types737.Split(',');
    private static readonly string[] Mel777 = Types777.Split(',');

    public static async Task SeedAsync(LitoralDbContext db)
    {
        await db.SaveChangesAsync();
        await SeedTypesAsync(db);
        await SeedScoringAsync(db);
        await SeedMelAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task SeedTypesAsync(LitoralDbContext db)
    {
        (string Icao, string Name, bool Cargo, int Range, string Prefix)[] types =
        [
            ("B77W", "Boeing 777-300ER", false, 7370, "LG"),
            ("B77F", "Boeing 777F", true, 4970, "LH")
        ];

        var existing = await db.AircraftTypes.ToListAsync();

        foreach (var t in types)
        {
            if (existing.Any(x => x.IcaoCode == t.Icao && x.IsCargo == t.Cargo)) continue;
            if (existing.Any(x => x.RegistrationPrefix == t.Prefix)) continue;

            var type = new AircraftType(t.Icao, t.Name, t.Cargo, t.Range, t.Prefix, 1000, 7500, 25000);
            type.SetDeliveryAirport(DeliveryAirport);
            db.AircraftTypes.Add(type);
        }
    }

    private static async Task SeedScoringAsync(LitoralDbContext db)
    {
        var rules = await db.ScoringRules.ToListAsync();
        if (rules.Count == 0 || rules.Any(r => r.AircraftTypes != null && r.AircraftTypes.Contains("B77W"))) return;

        foreach (var rule in rules.Where(r => r.AircraftTypes == null && TypeSpecificChecks.Contains(r.Check)))
        {
            var description = rule.Description.Contains("737") ? rule.Description : $"{rule.Description} (737)";
            rule.Update(rule.Category, rule.Check, description, rule.Outcome, rule.Points, rule.Mode, rule.Min, rule.Max,
                rule.Text, Types737, rule.GraceSeconds, rule.Order);
        }

        foreach (var rule in rules.Where(r => r.Check == ScoringCheck.SopDeviation && r.Description.Contains("FCOM")))
        {
            rule.Update(rule.Category, rule.Check, SopDescription, rule.Outcome, rule.Points, rule.Mode, rule.Min, rule.Max,
                rule.Text, rule.AircraftTypes, rule.GraceSeconds, rule.Order);
        }

        var order = rules.GroupBy(r => r.Category).ToDictionary(g => g.Key, g => g.Max(r => r.Order));
        int Next(ScoringCategory category) => order[category] = order.GetValueOrDefault(category) + 1;

        const ScoringOutcome pts = ScoringOutcome.Points;
        const ScoringOutcome reject = ScoringOutcome.Reject;
        const ScoringMode once = ScoringMode.Once;

        (string Flap, int Speed)[] flaps = [("1", 265), ("5", 245), ("15", 230), ("20", 225), ("25", 200), ("30", 180)];
        foreach (var (flap, speed) in flaps)
            db.ScoringRules.Add(new ScoringRule(ScoringCategory.Flaps, ScoringCheck.FlapsOverspeed, $"Flaps {flap} above {speed} kt (777)",
                reject, 0, once, null, speed, flap, Types777, order: Next(ScoringCategory.Flaps)));

        db.ScoringRules.AddRange(
            new ScoringRule(ScoringCategory.Configuration, ScoringCheck.TakeoffFlapsSet, "Flaps set for takeoff (777)", pts, 5, once,
                text: "5,15,20", aircraftTypes: Types777, order: Next(ScoringCategory.Configuration)),
            new ScoringRule(ScoringCategory.Configuration, ScoringCheck.LandingFlapsSet, "Flaps set for landing (777)", pts, 5, once,
                text: "25,30", aircraftTypes: Types777, order: Next(ScoringCategory.Configuration)),
            new ScoringRule(ScoringCategory.Configuration, ScoringCheck.TakeoffFlapsNotSet, "Flaps not set for takeoff (777: 5, 15 or 20)", reject, 0, once,
                text: "5,15,20", aircraftTypes: Types777, order: Next(ScoringCategory.Configuration)),
            new ScoringRule(ScoringCategory.Configuration, ScoringCheck.LandingFlapsNotSet, "Flaps not set for landing (777: 25 or 30)", reject, 0, once,
                text: "25,30", aircraftTypes: Types777, order: Next(ScoringCategory.Configuration)),

            new ScoringRule(ScoringCategory.Takeoff, ScoringCheck.TailStrike, "777-300ER: liftoff with pitch 10° or above (tail strike)", reject, 0, once,
                10, aircraftTypes: "B77W", order: Next(ScoringCategory.Takeoff)),
            new ScoringRule(ScoringCategory.Takeoff, ScoringCheck.TailStrike, "777F: liftoff with pitch 12.1° or above (tail strike)", reject, 0, once,
                12.1m, aircraftTypes: "B77F", order: Next(ScoringCategory.Takeoff)),
            new ScoringRule(ScoringCategory.Takeoff, ScoringCheck.RotationSpeedAbove, "VR above 195 kt (777)", reject, 0, once,
                max: 195, aircraftTypes: Types777, order: Next(ScoringCategory.Takeoff)),
            new ScoringRule(ScoringCategory.Takeoff, ScoringCheck.RotationSpeedWithin, "VR below 195 kt (777)", pts, 10, once,
                max: 195, aircraftTypes: Types777, order: Next(ScoringCategory.Takeoff)),

            new ScoringRule(ScoringCategory.LandingGear, ScoringCheck.GearUpOverspeed, "Gear up above 270 kt (777)", reject, 0, once,
                max: 270, aircraftTypes: Types777, order: Next(ScoringCategory.LandingGear)),
            new ScoringRule(ScoringCategory.LandingGear, ScoringCheck.GearDownOverspeed, "Gear down above 270 kt (777)", reject, 0, once,
                max: 270, aircraftTypes: Types777, order: Next(ScoringCategory.LandingGear)),

            new ScoringRule(ScoringCategory.Rejection, ScoringCheck.LowFuelAtPushback, "Fuel: below 12,000 kg at pushback (777)", reject, 0, once,
                12000, aircraftTypes: Types777, order: Next(ScoringCategory.Rejection)));
    }

    private static async Task SeedMelAsync(LitoralDbContext db)
    {
        var items = await db.MelItems.ToListAsync();
        if (items.Count == 0 || items.Any(m => m.Code == "49-71")) return;

        foreach (var item in items.Where(m => m.Code is "49-01" or "49-02" && m.AircraftTypeList.Count == 0))
            item.Update(item.Code, item.Kind, item.Title, item.Category, item.CategoryADays, item.Restriction, item.Procedure, item.Verification,
                item.AcarsCheck, item.CheckValue, item.CheckText, item.FuelPenaltyPercent, Mel737, item.AutoEligible, item.IsActive);

        const MelVerification safety = MelVerification.Safety;
        const MelAcarsCheck none = MelAcarsCheck.None;

        db.MelItems.AddRange(
            Item("21-71", "Bulk cargo heat inoperative", MelCategory.D,
                "No live animals or perishable cargo in the bulk compartment.",
                "Tell the load planner. Load live animals and perishables in the heated compartments only.",
                MelVerification.Info, none, auto: true),
            Item("22-71", "One of the three autopilots inoperative", MelCategory.C,
                "No CAT III approaches. CAT II is the lowest approach for the flight.",
                "Brief CAT II minimums as the lowest. Fly the approach with the two working autopilots.",
                safety, none, auto: true),
            Item("22-72", "Thrust asymmetry compensation inoperative", MelCategory.C,
                "Maximum crosswind 20 kt for takeoff and landing.",
                "Check the winds at departure and arrival. Be ready to hold the rudder by hand after an engine failure.",
                safety, none, auto: true),
            Item("23-71", "One HF radio inoperative", MelCategory.C,
                "Flights outside VHF coverage need the other HF radio and SATCOM working.",
                "Check the other HF radio and SATCOM before departure. Prefer routes within VHF coverage.",
                safety, none, auto: true),
            Item("23-72", "SATCOM inoperative", MelCategory.C,
                "Both HF radios must work for flights outside VHF coverage.",
                "Check both HF radios before departure and plan the position reports by HF.",
                safety, none, auto: true),
            Item("24-71", "Backup generator inoperative", MelCategory.C,
                "The route must stay within 120 minutes of an en-route alternate.",
                "Check the OFP for en-route alternates within 120 minutes along the whole route.",
                safety, none, auto: true),
            Item("26-71", "One cargo fire extinguisher bottle inoperative", MelCategory.B,
                "The route must stay within 60 minutes of an en-route alternate.",
                "Plan the route with an en-route alternate within 60 minutes at all times.",
                safety, none),
            Item("27-71", "Tail strike protection inoperative", MelCategory.C,
                "Extra care during rotation, mainly at heavy weights.",
                "Rotate smoothly at about 2 to 3 degrees per second and hold the liftoff attitude until airborne.",
                safety, none, auto: true),
            Item("28-71", "Fuel jettison inoperative", MelCategory.C,
                "Takeoff weight limited so a quick return can be made at or below the maximum landing weight.",
                "Check the OFP: takeoff weight minus one hour of fuel burn must be at or below the maximum landing weight.",
                safety, none, auto: true),
            Item("31-71", "Electronic checklist inoperative", MelCategory.D,
                "Paper checklists only.",
                "Keep the paper checklists at hand for the whole flight.",
                MelVerification.Info, none, auto: true),
            Item("49-71", "APU inoperative", MelCategory.C,
                "The route must stay within 120 minutes of an en-route alternate. Ground power and an air start unit for engine start.",
                "Keep the APU OFF during the whole flight. Start the engines with ground air and check the en-route alternates in the OFP.",
                MelVerification.Acars, MelAcarsCheck.ApuNotUsed, auto: true));
    }

    private static MelItem Item(
        string code, string title, MelCategory category, string restriction, string procedure,
        MelVerification verification, MelAcarsCheck check, bool auto = false) =>
        new(code, MelKind.Mel, title, category, null, restriction, procedure, verification, check, null, null, null, Mel777, auto);
}