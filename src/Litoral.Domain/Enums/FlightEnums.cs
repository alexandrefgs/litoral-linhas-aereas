namespace Litoral.Domain.Enums;

public enum FlightNetwork
{
    Offline,
    Vatsim,
    Ivao
}

public enum FlightSessionStatus
{
    Active,
    Completed,
    Abandoned
}

public enum FlightEventType
{
    Overspeed,
    Slew,
    Stall,
    Pause,
    SimulationRate,
    Refuel,
    Crash,
    Other
}

public enum PirepSource
{
    Acars,
    Manual
}

public enum PirepStatus
{
    Pending,
    Approved,
    Rejected
}