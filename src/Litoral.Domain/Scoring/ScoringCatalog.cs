namespace Litoral.Domain.Scoring;

public sealed record ScoringCategoryInfo(ScoringCategory Category, string Title, string? Subtitle);

public sealed record ScoringCheckInfo(
    ScoringCheck Check,
    string Label,
    ScoringSource Source,
    IReadOnlyList<ScoringMode> Modes,
    string? MinLabel = null,
    string? MaxLabel = null,
    string? TextLabel = null);

public static class ScoringCatalog
{
    private static readonly ScoringMode[] Once = [ScoringMode.Once];
    private static readonly ScoringMode[] Repeatable = [ScoringMode.Once, ScoringMode.PerOccurrence];
    private static readonly ScoringMode[] Timed = [ScoringMode.Once, ScoringMode.PerOccurrence, ScoringMode.PerSecond];

    public static readonly IReadOnlyList<ScoringCategoryInfo> Categories =
    [
        new(ScoringCategory.FlightTime, "Flight time", "Block time, from pushback to arrival"),
        new(ScoringCategory.TouchdownRate, "Touchdown rate", "The first touchdown of the flight counts"),
        new(ScoringCategory.Flaps, "Flaps", "Applied as soon as detected"),
        new(ScoringCategory.Configuration, "Takeoff & landing configuration", "Applied as soon as detected"),
        new(ScoringCategory.Takeoff, "Takeoff", "Applied as soon as detected"),
        new(ScoringCategory.LandingGear, "Landing gear", "Applied as soon as detected"),
        new(ScoringCategory.Overspeed, "Overspeed", null),
        new(ScoringCategory.Simulator, "Simulator", null),
        new(ScoringCategory.Communications, "Communications", "Applied after review"),
        new(ScoringCategory.FlightData, "Flight data", "Applied as soon as detected"),
        new(ScoringCategory.Procedures, "Procedures", "Applied as soon as detected"),
        new(ScoringCategory.Rejection, "Other rejection criteria", "A flight with one or more of these items is rejected. Rejected flights still count as flight hours."),
        new(ScoringCategory.SpecialFlights, "Ferry & charter flights", "Only flights offered on the Schedules page"),
        new(ScoringCategory.NetworkEvents, "Network events", "Events organized by the networks and offered on the Schedules page"),
        new(ScoringCategory.LitoralEvents, "Litoral events", "Events organized by Litoral and offered on the Schedules page"),
        new(ScoringCategory.CrewDuty, "Flights of other pilots", "When you are rostered as crew"),
        new(ScoringCategory.Cancellation, "Booking cancellation", "Applied after confirmation"),
        new(ScoringCategory.Inactivity, "Inactivity", "Only applied when the VA has open positions"),
        new(ScoringCategory.PilotsOfTheMonth, "Pilots of the month", "Granted after all reviews of the previous month are complete"),
        new(ScoringCategory.AccountLoss, "Account loss", "Deleted automatically, without recovery")
    ];

    public static readonly IReadOnlyList<ScoringCheckInfo> Checks =
    [
        new(ScoringCheck.BlockTime, "Block time", ScoringSource.Acars, Once, "From (min)", "Up to (min)"),
        new(ScoringCheck.TouchdownRate, "Touchdown rate", ScoringSource.Acars, Once, "From (fpm)", "Up to (fpm)"),
        new(ScoringCheck.FlapsOverspeed, "Flaps above speed", ScoringSource.Acars, Repeatable, null, "Speed (kt)", "Flap position"),
        new(ScoringCheck.TakeoffFlapsSet, "Takeoff flaps set", ScoringSource.Acars, Once, null, null, "Valid positions"),
        new(ScoringCheck.TakeoffFlapsNotSet, "Takeoff flaps not set", ScoringSource.Acars, Once, null, null, "Valid positions"),
        new(ScoringCheck.LandingFlapsSet, "Landing flaps set", ScoringSource.Acars, Once, null, null, "Valid positions"),
        new(ScoringCheck.LandingFlapsNotSet, "Landing flaps not set", ScoringSource.Acars, Once, null, null, "Valid positions"),
        new(ScoringCheck.TailStrike, "Liftoff pitch (tail strike)", ScoringSource.Acars, Once, "Pitch (°)"),
        new(ScoringCheck.RotationSpeedAbove, "VR above speed", ScoringSource.Acars, Once, null, "Speed (kt)"),
        new(ScoringCheck.RotationSpeedWithin, "VR within speed", ScoringSource.Acars, Once, null, "Speed (kt)"),
        new(ScoringCheck.GearUpOverspeed, "Gear up above speed", ScoringSource.Acars, Repeatable, null, "Speed (kt)"),
        new(ScoringCheck.GearDownOverspeed, "Gear down above speed", ScoringSource.Acars, Repeatable, null, "Speed (kt)"),
        new(ScoringCheck.Overspeed, "Overspeed", ScoringSource.Acars, Timed),
        new(ScoringCheck.OverspeedTotal, "Overspeed total", ScoringSource.Acars, Once, "Penalty at or above (pts)"),
        new(ScoringCheck.Slew, "Slew", ScoringSource.Acars, Repeatable),
        new(ScoringCheck.Stall, "Stall", ScoringSource.Acars, Repeatable),
        new(ScoringCheck.Pause, "Pause", ScoringSource.Acars, Repeatable),
        new(ScoringCheck.SimulationRate, "Simulation rate", ScoringSource.Acars, Repeatable),
        new(ScoringCheck.Refuel, "Refuel in flight", ScoringSource.Acars, Repeatable),
        new(ScoringCheck.NoActiveFrequency, "No ATC or UNICOM frequency", ScoringSource.Review, Once),
        new(ScoringCheck.IncompleteData, "Incomplete data", ScoringSource.Acars, Once),
        new(ScoringCheck.EngineStartOrder, "Engine started first", ScoringSource.Acars, Once, "Engine #"),
        new(ScoringCheck.TaxiInAbove, "Taxi in above time", ScoringSource.Acars, Once, "Time (s)", null, "Airports not monitored"),
        new(ScoringCheck.TaxiInBelow, "Taxi in below time", ScoringSource.Acars, Once, "Time (s)", null, "Airports not monitored"),
        new(ScoringCheck.EnginesOffAtLog, "Engines off before sending the log", ScoringSource.Acars, Once),
        new(ScoringCheck.EnginesRunningAtLog, "Engines running when sending the log", ScoringSource.Acars, Once),
        new(ScoringCheck.TaxiSpeed, "Taxi speed", ScoringSource.Acars, Repeatable, null, "Speed (kt)"),
        new(ScoringCheck.Offline, "Offline flight", ScoringSource.Acars, Once),
        new(ScoringCheck.Crash, "Crash", ScoringSource.Acars, Once),
        new(ScoringCheck.UndeclaredAlternate, "Alternate without justification", ScoringSource.Review, Once, null, null, "Accepted codes"),
        new(ScoringCheck.MissingRemarks, "Missing flight plan remarks", ScoringSource.Review, Once, null, null, "Required remarks"),
        new(ScoringCheck.EarlyFlapRetraction, "Flaps retracted below height", ScoringSource.Acars, Once, null, "Height (ft AFE)"),
        new(ScoringCheck.UnstableApproach, "Not configured for landing", ScoringSource.Acars, Once, "Height (ft AFE)"),
        new(ScoringCheck.LowFuelAtPushback, "Fuel at pushback below", ScoringSource.Acars, Once, "Fuel (kg)"),
        new(ScoringCheck.FuelInPounds, "Fuel unit in pounds", ScoringSource.Acars, Once),
        new(ScoringCheck.LowOnlinePercent, "Online time below", ScoringSource.Acars, Once, "Online (%)"),
        new(ScoringCheck.SimultaneousNetworks, "Simultaneous flights", ScoringSource.Review, Once),
        new(ScoringCheck.UnregisteredNetworkId, "Network ID not registered", ScoringSource.Acars, Once),
        new(ScoringCheck.UnreportedDiversion, "Unreported diversion or incomplete flight", ScoringSource.Acars, Once),
        new(ScoringCheck.SopDeviation, "Operation not following FCOM/FCTM/OM", ScoringSource.Review, Once),
        new(ScoringCheck.SpecialFlightDomestic, "Ferry/charter, domestic leg", ScoringSource.Flight, Once),
        new(ScoringCheck.SpecialFlightInternational, "Ferry/charter, international leg", ScoringSource.Flight, Once),
        new(ScoringCheck.NetworkEventVatsim, "VATSIM event", ScoringSource.Flight, Once),
        new(ScoringCheck.NetworkEventIvao, "IVAO event", ScoringSource.Flight, Once),
        new(ScoringCheck.LitoralEventDomestic, "Litoral event, domestic leg", ScoringSource.Flight, Once),
        new(ScoringCheck.LitoralEventInternational, "Litoral event, international leg", ScoringSource.Flight, Once),
        new(ScoringCheck.CrewDuty, "Rostered as crew", ScoringSource.Flight, Once),
        new(ScoringCheck.BookingCancelled, "Booking cancelled", ScoringSource.Pilot, [ScoringMode.Once, ScoringMode.PerOccurrence]),
        new(ScoringCheck.Inactivity, "Inactivity", ScoringSource.Pilot, [ScoringMode.Once, ScoringMode.PerDay], "After (days)"),
        new(ScoringCheck.PilotOfTheMonth, "Pilot of the month", ScoringSource.Pilot, Once, "Place"),
        new(ScoringCheck.AccountLoss, "Account loss", ScoringSource.Pilot, Once, null, "Balance at or below (pts)")
    ];

    private static readonly Dictionary<ScoringCheck, ScoringCheckInfo> ByCheck = Checks.ToDictionary(c => c.Check);
    private static readonly Dictionary<ScoringCategory, ScoringCategoryInfo> ByCategory = Categories.ToDictionary(c => c.Category);

    public static ScoringCheckInfo Get(ScoringCheck check) => ByCheck[check];

    public static ScoringCategoryInfo Get(ScoringCategory category) => ByCategory[category];
}