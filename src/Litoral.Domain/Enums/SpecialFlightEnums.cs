namespace Litoral.Domain.Enums;

public enum SpecialFlightType
{
    Charter,
    Ferry,
    Redelivery,
    MaintenanceFerry,
    CheckFlight
}

public enum SpecialFlightStatus
{
    Open,
    Taken,
    Completed,
    Expired,
    Cancelled
}