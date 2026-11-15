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
    public bool IsActive { get; private set; } = true;

    public FlightBlock Block => FlightBlockRules.FromNumber(FlightNumber);
    public string Callsign => $"{Airline.Icao}{FlightNumber}";

    private Route() { }

    public Route(int flightNumber, Airport departure, Airport arrival, AircraftType aircraftType)
    {
        var block = FlightBlockRules.FromNumber(flightNumber);

        if (departure.Id == arrival.Id)
            throw new DomainException("Origem e destino não podem ser iguais.");
        if (aircraftType.IsCargo && block != FlightBlock.FerryAndCargo)
            throw new DomainException("Voos cargueiros devem usar o bloco 9XXX.");

        var distance = departure.DistanceNmTo(arrival);
        if (distance > aircraftType.MaxRangeNm)
            throw new DomainException(
                $"Distância de {distance} NM excede o alcance do {aircraftType.IcaoCode} ({aircraftType.MaxRangeNm} NM).");

        FlightNumber = flightNumber;
        DepartureAirport = departure;
        DepartureAirportId = departure.Id;
        ArrivalAirport = arrival;
        ArrivalAirportId = arrival.Id;
        AircraftType = aircraftType;
        AircraftTypeId = aircraftType.Id;
        DistanceNm = distance;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}