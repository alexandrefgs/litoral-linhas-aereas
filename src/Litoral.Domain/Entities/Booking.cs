using System.Text.RegularExpressions;
using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class Booking : Entity
{
    private static readonly Regex CallsignPattern = new($"^{Airline.Icao}[0-9A-Z]{{1,4}}$");

    public Guid PilotId { get; private set; }
    public Pilot Pilot { get; private set; } = null!;
    public Guid RouteId { get; private set; }
    public Route Route { get; private set; } = null!;
    public Guid AircraftId { get; private set; }
    public Aircraft Aircraft { get; private set; } = null!;
    public BookingType Type { get; private set; }
    public int PointsCost { get; private set; }
    public string Callsign { get; private set; } = null!;
    public BookingStatus Status { get; private set; } = BookingStatus.Booked;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public bool IsOpen => Status is BookingStatus.Booked or BookingStatus.InProgress;

    private Booking() { }

    public Booking(Pilot pilot, Route route, Aircraft aircraft, BookingType type, int pointsCost = 0, string? callsign = null)
    {
        if (!route.IsActive)
            throw new DomainException("Rota inativa.");
        if (!route.DepartureAirport.IsAccessibleTo(pilot.Rank))
            throw new DomainException($"Sua patente não permite operar em {route.DepartureAirport.Icao}.");
        if (!route.ArrivalAirport.IsAccessibleTo(pilot.Rank))
            throw new DomainException($"Sua patente não permite operar em {route.ArrivalAirport.Icao}.");
        if (pilot.CurrentLocationIcao != route.DepartureAirport.Icao)
            throw new DomainException($"O piloto está em {pilot.CurrentLocationIcao}, mas a rota parte de {route.DepartureAirport.Icao}.");
        if (aircraft.AircraftTypeId != route.AircraftTypeId)
            throw new DomainException($"A aeronave {aircraft.Registration} não é do tipo desta rota.");
        if (aircraft.CurrentAirportId != route.DepartureAirportId)
            throw new DomainException($"A aeronave {aircraft.Registration} não está em {route.DepartureAirport.Icao}.");
        if (pointsCost < 0)
            throw new DomainException("Custo em pontos não pode ser negativo.");

        var finalCallsign = (callsign ?? route.Callsign).ToUpperInvariant();
        if (!CallsignPattern.IsMatch(finalCallsign))
            throw new DomainException($"Callsign deve começar com {Airline.Icao} e ter até 4 caracteres depois.");

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
    }

    public void Start()
    {
        if (Status != BookingStatus.Booked)
            throw new DomainException("Só é possível iniciar um booking reservado.");

        Aircraft.StartFlight();
        Status = BookingStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != BookingStatus.InProgress)
            throw new DomainException("Só é possível concluir um voo em andamento.");

        Aircraft.CompleteFlight(Route.ArrivalAirport);
        Status = BookingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Completed)
            throw new DomainException("Booking concluído não pode ser cancelado.");

        Aircraft.Release();
        Status = BookingStatus.Cancelled;
    }
}