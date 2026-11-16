using System.Text.RegularExpressions;
using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class Aircraft : Entity
{
    private static readonly Regex RegistrationPattern = new("^P[PRSTU]-[A-Z]{3}$");

    public string Registration { get; private set; } = null!;
    public Guid AircraftTypeId { get; private set; }
    public AircraftType AircraftType { get; private set; } = null!;
    public string? Variant { get; private set; }
    public string? PhotoUrl { get; private set; }
    public Guid CurrentAirportId { get; private set; }
    public Airport CurrentAirport { get; private set; } = null!;
    public AircraftStatus Status { get; private set; } = AircraftStatus.Available;
    public DateTime? LastFlightAt { get; private set; }

    public bool IsAvailable => Status == AircraftStatus.Available;

    private Aircraft() { }

    public Aircraft(string registration, AircraftType aircraftType, Airport currentAirport, string? variant = null, string? photoUrl = null)
    {
        var normalized = registration.Trim().ToUpperInvariant();
        if (!RegistrationPattern.IsMatch(normalized))
            throw new DomainException("Registration must follow the Brazilian format, e.g. PS-LTA.");

        Registration = normalized;
        AircraftType = aircraftType;
        AircraftTypeId = aircraftType.Id;
        CurrentAirport = currentAirport;
        CurrentAirportId = currentAirport.Id;
        UpdateDetails(variant, photoUrl);
    }

    public void UpdateDetails(string? variant, string? photoUrl)
    {
        Variant = string.IsNullOrWhiteSpace(variant) ? null : variant.Trim();
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();
    }

    public void Reserve()
    {
        if (Status != AircraftStatus.Available)
            throw new DomainException($"Aircraft {Registration} is not available.");

        Status = AircraftStatus.Reserved;
    }

    public void Release()
    {
        if (Status is AircraftStatus.Reserved or AircraftStatus.InFlight)
            Status = AircraftStatus.Available;
    }

    public void StartFlight()
    {
        if (Status != AircraftStatus.Reserved)
            throw new DomainException($"Aircraft {Registration} must be reserved to start the flight.");

        Status = AircraftStatus.InFlight;
    }

    public void CompleteFlight(Airport destination)
    {
        if (Status != AircraftStatus.InFlight)
            throw new DomainException($"Aircraft {Registration} is not in flight.");

        MoveTo(destination);
        Status = AircraftStatus.Available;
        LastFlightAt = DateTime.UtcNow;
    }

    public void Relocate(Airport destination)
    {
        if (Status != AircraftStatus.Available)
            throw new DomainException($"Aircraft {Registration} can only be moved when it is available.");

        MoveTo(destination);
    }

    public void SendToMaintenance()
    {
        if (Status is AircraftStatus.Reserved or AircraftStatus.InFlight)
            throw new DomainException($"Aircraft {Registration} is in use and cannot go into maintenance.");

        Status = AircraftStatus.Maintenance;
    }

    public void ReturnFromMaintenance()
    {
        if (Status == AircraftStatus.Maintenance)
            Status = AircraftStatus.Available;
    }

    private void MoveTo(Airport destination)
    {
        CurrentAirport = destination;
        CurrentAirportId = destination.Id;
    }
}