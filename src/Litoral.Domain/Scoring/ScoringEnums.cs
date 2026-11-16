namespace Litoral.Domain.Scoring;

public enum ScoringCategory
{
    FlightTime,
    TouchdownRate,
    Flaps,
    Configuration,
    Takeoff,
    LandingGear,
    Overspeed,
    Simulator,
    Communications,
    FlightData,
    Procedures,
    Rejection,
    SpecialFlights,
    NetworkEvents,
    LitoralEvents,
    CrewDuty,
    Cancellation,
    Inactivity,
    PilotsOfTheMonth,
    AccountLoss
}

public enum ScoringOutcome
{
    Points,
    Reject
}

public enum ScoringMode
{
    Once,
    PerOccurrence,
    PerSecond,
    PerDay
}

public enum ScoringSource
{
    Acars,
    Review,
    Flight,
    Pilot
}

public enum ScoringCheck
{
    BlockTime,
    TouchdownRate,
    FlapsOverspeed,
    TakeoffFlapsSet,
    TakeoffFlapsNotSet,
    LandingFlapsSet,
    LandingFlapsNotSet,
    TailStrike,
    RotationSpeedAbove,
    RotationSpeedWithin,
    GearUpOverspeed,
    GearDownOverspeed,
    Overspeed,
    OverspeedTotal,
    Slew,
    Stall,
    Pause,
    SimulationRate,
    Refuel,
    NoActiveFrequency,
    IncompleteData,
    EngineStartOrder,
    TaxiInAbove,
    TaxiInBelow,
    EnginesOffAtLog,
    EnginesRunningAtLog,
    TaxiSpeed,
    Offline,
    Crash,
    UndeclaredAlternate,
    MissingRemarks,
    EarlyFlapRetraction,
    UnstableApproach,
    LowFuelAtPushback,
    FuelInPounds,
    LowOnlinePercent,
    SimultaneousNetworks,
    UnregisteredNetworkId,
    UnreportedDiversion,
    SopDeviation,
    SpecialFlightDomestic,
    SpecialFlightInternational,
    NetworkEventVatsim,
    NetworkEventIvao,
    LitoralEventDomestic,
    LitoralEventInternational,
    CrewDuty,
    BookingCancelled,
    Inactivity,
    PilotOfTheMonth,
    AccountLoss
}