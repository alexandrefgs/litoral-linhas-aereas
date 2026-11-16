using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class Route : Entity
{
    public int FlightNumber { get; private set; }
    public Guid DepartureAirportId { get; private set; }
    public Airport DepartureAirport { get; private set; } = null!;
    public Guid ArrivalAirportId { get; private set; }
    public Airport ArrivalAirport { get; private set; } = null!;
    public Guid AircraftTypeId { get; private set; }
    public AircraftType AircraftType { get; private set; } = null!;
    public int DistanceNm { get; private set; }
    public int EstimatedMinutes { get; private set; }
    public bool IsActive { get; private set; } = true;

    public FlightBlock Block => FlightBlockRules.FromNumber(FlightNumber);
    public string Callsign => $"{Airline.Icao}{FlightNumber}";

    private Route() { }

    public Route(int flightNumber, Airport departure, Airport arrival, AircraftType aircraftType)
    {
        var block = FlightBlockRules.FromNumber(flightNumber);

        if (departure.Id == arrival.Id)
            throw new DomainException("Departure and arrival cannot be the same.");
        if (aircraftType.IsCargo && block != FlightBlock.FerryAndCargo)
            throw new DomainException("Cargo flights must use the 9XXX block.");
        var operation = aircraftType.IsCargo ? OperationType.Cargo : OperationType.Passenger;
        var operationName = aircraftType.IsCargo ? "cargo" : "passenger";
        if (!departure.Supports(operation))
            throw new DomainException($"{departure.Icao} does not have {operationName} operations.");
        if (!arrival.Supports(operation))
            throw new DomainException($"{arrival.Icao} does not have {operationName} operations.");
        var distance = departure.DistanceNmTo(arrival);
        if (distance > aircraftType.MaxRangeNm)
            throw new DomainException(
                $"Distance of {distance} NM exceeds the {aircraftType.IcaoCode} range ({aircraftType.MaxRangeNm} NM).");

        FlightNumber = flightNumber;
        DepartureAirport = departure;
        DepartureAirportId = departure.Id;
        ArrivalAirport = arrival;
        ArrivalAirportId = arrival.Id;
        AircraftType = aircraftType;
        AircraftTypeId = aircraftType.Id;
        DistanceNm = distance;
        EstimatedMinutes = EstimateBlockMinutes(distance);
    }

    public static int EstimateBlockMinutes(int distanceNm) =>
        (int)Math.Round(distanceNm / 430.0 * 60 + 25);

    public void SetEstimatedMinutes(int minutes)
    {
        if (minutes <= 0)
            throw new DomainException("Estimated duration must be greater than zero.");

        EstimatedMinutes = minutes;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}