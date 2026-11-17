using System.Text.RegularExpressions;
using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class Booking : Entity
{
    public static readonly TimeSpan DefaultValidity = TimeSpan.FromHours(24);
    public static readonly TimeSpan SpecialValidity = TimeSpan.FromHours(48);

    private static readonly Regex CallsignPattern = new($"^{Airline.Icao}[0-9A-Z]{{1,4}}$");

    public Guid PilotId { get; private set; }
    public Pilot Pilot { get; private set; } = null!;
    public Guid? CrewPilotId { get; private set; }
    public Pilot? CrewPilot { get; private set; }
    public Guid RouteId { get; private set; }
    public Route Route { get; private set; } = null!;
    public Guid AircraftId { get; private set; }
    public Aircraft Aircraft { get; private set; } = null!;
    public BookingType Type { get; private set; }
    public int PointsCost { get; private set; }
    public string Callsign { get; private set; } = null!;
    public BookingStatus Status { get; private set; } = BookingStatus.Booked;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public bool IsOpen => Status is BookingStatus.Booked or BookingStatus.InProgress;

    private Booking() { }

        public Booking(Pilot pilot, Route route, Aircraft aircraft, BookingType type, int pointsCost = 0, string? callsign = null, DateTime? expiresAt = null)
    {
        if (!route.IsActive)
            throw new DomainException("This route is inactive.");
        if (!route.DepartureAirport.IsAccessibleTo(pilot.Rank))
            throw new DomainException($"Your rank does not allow operations at {route.DepartureAirport.Icao}.");
        if (!route.ArrivalAirport.IsAccessibleTo(pilot.Rank))
            throw new DomainException($"Your rank does not allow operations at {route.ArrivalAirport.Icao}.");
        if (pilot.CurrentLocationIcao != route.DepartureAirport.Icao)
            throw new DomainException($"You are at {pilot.CurrentLocationIcao}, but the route departs from {route.DepartureAirport.Icao}.");
        if (!route.AllowsAircraftType(aircraft.AircraftTypeId))
            throw new DomainException($"Aircraft {aircraft.Registration} is not one of the types allowed on this route.");
        if (aircraft.CurrentAirportId != route.DepartureAirportId)
            throw new DomainException($"Aircraft {aircraft.Registration} is not at {route.DepartureAirport.Icao}.");
        if (pointsCost < 0)
            throw new DomainException("Points cost cannot be negative.");
        if (expiresAt is { } limit && limit <= DateTime.UtcNow)
            throw new DomainException("The booking deadline must be in the future.");
        var finalCallsign = (callsign ?? route.Callsign).ToUpperInvariant();
        if (!CallsignPattern.IsMatch(finalCallsign))
            throw new DomainException($"Callsign must start with {Airline.Icao} followed by up to 4 characters.");

        aircraft.Reserve();

        Pilot = pilot;
        PilotId = pilot.Id;
        Route = route;
        RouteId = route.Id;
        Aircraft = aircraft;
        AircraftId = aircraft.Id;
        Type = type;
        PointsCost = pointsCost;
        Callsign = finalCallsign;
        ExpiresAt = expiresAt ?? CreatedAt + (type == BookingType.Special ? SpecialValidity : DefaultValidity);
    }

    public void AssignCrew(Pilot crew)
    {
        if (crew.Id == PilotId)
            throw new DomainException("The pilot cannot be their own crew.");

        CrewPilot = crew;
        CrewPilotId = crew.Id;
        crew.MarkCrewAssigned();
    }

    public void Start()
    {
        if (Status != BookingStatus.Booked)
            throw new DomainException("Only a booked flight can be started.");

        Aircraft.StartFlight();
        Status = BookingStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(int flightMinutes, Airport? arrival = null)
    {
        if (Status != BookingStatus.InProgress)
            throw new DomainException("Only a flight in progress can be completed.");

        Aircraft.CompleteFlight(arrival ?? Route.ArrivalAirport, flightMinutes);
        Status = BookingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ClosedAt = CompletedAt;
    }

    public void Cancel()
    {
        if (!IsOpen)
            throw new DomainException("This booking is already closed.");

        Aircraft.Release();
        Status = BookingStatus.Cancelled;
        ClosedAt = DateTime.UtcNow;
    }

    public bool Expire(DateTime now)
    {
        if (Status != BookingStatus.Booked || now < ExpiresAt)
            return false;

        Aircraft.Release();
        Status = BookingStatus.Expired;
        ClosedAt = now;
        return true;
    }
}