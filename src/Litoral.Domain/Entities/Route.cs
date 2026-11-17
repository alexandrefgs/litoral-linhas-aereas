using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class Route : Entity
{
    private readonly List<AircraftType> _aircraftTypes = [];

    public int FlightNumber { get; private set; }
    public Guid DepartureAirportId { get; private set; }
    public Airport DepartureAirport { get; private set; } = null!;
    public Guid ArrivalAirportId { get; private set; }
    public Airport ArrivalAirport { get; private set; } = null!;
    public bool IsCargo { get; private set; }
    public int DistanceNm { get; private set; }
    public int EstimatedMinutes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsSpecial { get; private set; }

    public IReadOnlyCollection<AircraftType> AircraftTypes => _aircraftTypes;
    public FlightBlock Block => FlightBlockRules.FromNumber(FlightNumber);
    public string Callsign => $"{Airline.Icao}{FlightNumber}";

    private Route() { }

    public Route(int flightNumber, Airport departure, Airport arrival, IEnumerable<AircraftType> aircraftTypes, int? estimatedMinutes = null)
    {
        Update(flightNumber, departure, arrival, aircraftTypes, estimatedMinutes);
    }

    public void Update(int flightNumber, Airport departure, Airport arrival, IEnumerable<AircraftType> aircraftTypes, int? estimatedMinutes = null)
    {
        var block = FlightBlockRules.FromNumber(flightNumber);
        var types = aircraftTypes.DistinctBy(t => t.Id).ToList();

        if (types.Count == 0)
            throw new DomainException("Select at least one aircraft type.");
        if (types.Select(t => t.IsCargo).Distinct().Count() > 1)
            throw new DomainException("A route cannot mix passenger and cargo aircraft.");
        if (departure.Id == arrival.Id)
            throw new DomainException("Departure and arrival cannot be the same.");

        var isCargo = types[0].IsCargo;
        if (isCargo && block != FlightBlock.FerryAndCargo)
            throw new DomainException("Cargo flights must use the 9XXX block.");

        var operation = isCargo ? OperationType.Cargo : OperationType.Passenger;
        var operationName = isCargo ? "cargo" : "passenger";
        if (!departure.Supports(operation))
            throw new DomainException($"{departure.Icao} does not have {operationName} operations.");
        if (!arrival.Supports(operation))
            throw new DomainException($"{arrival.Icao} does not have {operationName} operations.");

        var distance = departure.DistanceNmTo(arrival);
        var outOfRange = types.Where(t => distance > t.MaxRangeNm).Select(t => $"{t.IcaoCode} ({t.MaxRangeNm} NM)").ToList();
        if (outOfRange.Count > 0)
            throw new DomainException($"Distance of {distance} NM exceeds the range of {string.Join(", ", outOfRange)}.");

        FlightNumber = flightNumber;
        DepartureAirport = departure;
        DepartureAirportId = departure.Id;
        ArrivalAirport = arrival;
        ArrivalAirportId = arrival.Id;
        IsCargo = isCargo;
        DistanceNm = distance;
        SetEstimatedMinutes(estimatedMinutes ?? EstimateBlockMinutes(distance));

        _aircraftTypes.Clear();
        _aircraftTypes.AddRange(types);
    }

    public static Route CreateSpecial(int flightNumber, Airport departure, Airport arrival, AircraftType aircraftType)
    {
        if (FlightBlockRules.FromNumber(flightNumber) != FlightBlock.FerryAndCargo)
            throw new DomainException("Special flights must use the 9XXX block.");
        if (departure.Id == arrival.Id)
            throw new DomainException("Departure and arrival cannot be the same.");

        var distance = departure.DistanceNmTo(arrival);
        if (distance > aircraftType.MaxRangeNm)
            throw new DomainException($"Distance of {distance} NM exceeds the range of {aircraftType.IcaoCode} ({aircraftType.MaxRangeNm} NM).");

        var route = new Route
        {
            FlightNumber = flightNumber,
            DepartureAirport = departure,
            DepartureAirportId = departure.Id,
            ArrivalAirport = arrival,
            ArrivalAirportId = arrival.Id,
            IsCargo = aircraftType.IsCargo,
            DistanceNm = distance,
            IsSpecial = true
        };
        route.SetEstimatedMinutes(EstimateBlockMinutes(distance));
        route._aircraftTypes.Add(aircraftType);
        return route;
    }

        public static Route CreateCheckFlight(int flightNumber, Airport airport, AircraftType aircraftType)
    {
        if (FlightBlockRules.FromNumber(flightNumber) != FlightBlock.FerryAndCargo)
            throw new DomainException("Special flights must use the 9XXX block.");

        var route = new Route
        {
            FlightNumber = flightNumber,
            DepartureAirport = airport,
            DepartureAirportId = airport.Id,
            ArrivalAirport = airport,
            ArrivalAirportId = airport.Id,
            IsCargo = aircraftType.IsCargo,
            DistanceNm = 0,
            IsSpecial = true
        };
        route.SetEstimatedMinutes(CheckFlightMinutes);
        route._aircraftTypes.Add(aircraftType);
        return route;
    }

    public bool AllowsAircraftType(Guid aircraftTypeId) => _aircraftTypes.Any(t => t.Id == aircraftTypeId);

    public const int CheckFlightMinutes = 60;

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