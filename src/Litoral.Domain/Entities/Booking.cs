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
    public string Callsign { get; private set; } = null!;
    public BookingStatus Status { get; private set; } = BookingStatus.Booked;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public bool IsOpen => Status is BookingStatus.Booked or BookingStatus.InProgress;

    private Booking() { }

    public Booking(Pilot pilot, Route route, string? callsign = null)
    {
        if (!route.IsActive)
            throw new DomainException("Rota inativa.");

        var finalCallsign = (callsign ?? route.Callsign).ToUpperInvariant();
        if (!CallsignPattern.IsMatch(finalCallsign))
            throw new DomainException($"Callsign deve começar com {Airline.Icao} e ter até 4 caracteres depois.");

        Pilot = pilot;
        PilotId = pilot.Id;
        Route = route;
        RouteId = route.Id;
        Callsign = finalCallsign;
    }

    public void Start()
    {
        if (Status != BookingStatus.Booked)
            throw new DomainException("Só é possível iniciar um booking reservado.");

        Status = BookingStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != BookingStatus.InProgress)
            throw new DomainException("Só é possível concluir um voo em andamento.");

        Status = BookingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Completed)
            throw new DomainException("Booking concluído não pode ser cancelado.");

        Status = BookingStatus.Cancelled;
    }
}