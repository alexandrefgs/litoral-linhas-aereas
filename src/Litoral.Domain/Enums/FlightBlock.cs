using Litoral.Domain.Common;

namespace Litoral.Domain.Enums;

public enum FlightBlock
{
    Domestic,
    SouthAmerica,
    CentralNorthAmerica,
    Seasonal,
    EuropeAndOthers,
    FerryAndCargo
}

public static class FlightBlockRules
{
    public static FlightBlock FromNumber(int flightNumber) => flightNumber switch
    {
        >= 1000 and <= 4999 => FlightBlock.Domestic,
        >= 5000 and <= 5999 => FlightBlock.SouthAmerica,
        >= 6000 and <= 6999 => FlightBlock.CentralNorthAmerica,
        >= 7000 and <= 7999 => FlightBlock.Seasonal,
        >= 8000 and <= 8999 => FlightBlock.EuropeAndOthers,
        >= 9000 and <= 9999 => FlightBlock.FerryAndCargo,
        _ => throw new DomainException("Flight number must be between 1000 and 9999.")
    };

    public static (int Min, int Max) Range(FlightBlock block) => block switch
    {
        FlightBlock.Domestic => (1000, 4999),
        FlightBlock.SouthAmerica => (5000, 5999),
        FlightBlock.CentralNorthAmerica => (6000, 6999),
        FlightBlock.Seasonal => (7000, 7999),
        FlightBlock.EuropeAndOthers => (8000, 8999),
        _ => (9000, 9999)
    };
}