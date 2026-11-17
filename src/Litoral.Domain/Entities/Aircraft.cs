using System.Text.RegularExpressions;
using Litoral.Domain.Common;
using Litoral.Domain.Enums;
using Litoral.Domain.Fleet;

namespace Litoral.Domain.Entities;

public sealed class Aircraft : Entity
{
    private static readonly Regex RegistrationPattern = new("^P[PRSTU]-[A-Z]{3}$");

    public string Registration { get; private set; } = null!;
    public Guid AircraftTypeId { get; private set; }
    public AircraftType AircraftType { get; private set; } = null!;
    public string? Variant { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? PhotoCredit { get; private set; }
    public Guid CurrentAirportId { get; private set; }
    public Airport CurrentAirport { get; private set; } = null!;
    public AircraftStatus Status { get; private set; } = AircraftStatus.Available;
    public int TotalMinutes { get; private set; }
    public int Cycles { get; private set; }
    public int MinutesSinceMaintenance { get; private set; }
    public int MinutesSinceCCheck { get; private set; }
    public int MinutesSinceDCheck { get; private set; }
    public MaintenanceCheck? CurrentCheck { get; private set; }
    public string? MaintenanceReason { get; private set; }
    public DateTime? MaintenanceStartedAt { get; private set; }
    public DateTime? MaintenanceUntil { get; private set; }
    public DateTime? LastMaintenanceAt { get; private set; }
    public DateTime? LastFlightAt { get; private set; }

    public bool IsAvailable => Status == AircraftStatus.Available;
    public bool IsInUse => Status is AircraftStatus.Reserved or AircraftStatus.InFlight;
        public MaintenanceCheck? DueCheck =>
        MinutesSinceDCheck >= AircraftType.DCheckIntervalHours * 60 ? MaintenanceCheck.D
        : MinutesSinceCCheck >= AircraftType.CCheckIntervalHours * 60 ? MaintenanceCheck.C
        : MinutesSinceMaintenance >= AircraftType.MaintenanceIntervalHours * 60 ? MaintenanceCheck.A
        : null;

    public bool IsMaintenanceDue => DueCheck is not null;

    private Aircraft() { }

    public Aircraft(string registration, AircraftType aircraftType, Airport currentAirport, string? variant = null)
    {
        Update(registration, aircraftType, currentAirport, variant);
    }

    public void Update(string registration, AircraftType aircraftType, Airport currentAirport, string? variant)
    {
        var normalized = (registration ?? string.Empty).Trim().ToUpperInvariant();
        if (!RegistrationPattern.IsMatch(normalized))
            throw new DomainException("Registration must follow the Brazilian format, e.g. PS-LAA.");
        if (RegistrationMarks.ProhibitedReason(normalized[3..]) is { } reason)
            throw new DomainException($"{normalized} is prohibited by ANAC: {reason}.");
        var changesOperation = AircraftTypeId != aircraftType.Id || CurrentAirportId != currentAirport.Id;
        if (changesOperation && IsInUse)
            throw new DomainException($"Aircraft {Registration} is in use. Its type and location cannot be changed.");

        EnsureOperationAt(currentAirport, aircraftType);

        Registration = normalized;
        AircraftType = aircraftType;
        AircraftTypeId = aircraftType.Id;
        Variant = string.IsNullOrWhiteSpace(variant) ? null : variant.Trim();
        MoveTo(currentAirport);
    }

    public void SetPhoto(string? url, string? credit)
    {
        PhotoUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        PhotoCredit = PhotoUrl is null || string.IsNullOrWhiteSpace(credit) ? null : credit.Trim();
    }

    public void Reserve()
    {
        if (Status != AircraftStatus.Available)
            throw new DomainException($"Aircraft {Registration} is not available.");

        Status = AircraftStatus.Reserved;
    }

    public void Release()
    {
        if (IsInUse)
            Status = AircraftStatus.Available;
    }

    public void StartFlight()
    {
        if (Status != AircraftStatus.Reserved)
            throw new DomainException($"Aircraft {Registration} must be reserved to start the flight.");

        Status = AircraftStatus.InFlight;
    }

    public void CompleteFlight(Airport destination, int flightMinutes)
    {
        if (Status != AircraftStatus.InFlight)
            throw new DomainException($"Aircraft {Registration} is not in flight.");
        if (flightMinutes < 0)
            throw new DomainException("Flight time cannot be negative.");

        MoveTo(destination);
        TotalMinutes += flightMinutes;
        MinutesSinceMaintenance += flightMinutes;
        MinutesSinceCCheck += flightMinutes;
        MinutesSinceDCheck += flightMinutes;
        Cycles++;
        Status = AircraftStatus.Available;
        LastFlightAt = DateTime.UtcNow;
    }

    public void Relocate(Airport destination)
    {
        if (IsInUse)
            throw new DomainException($"Aircraft {Registration} is in use and cannot be moved.");

        EnsureOperationAt(destination, AircraftType);
        MoveTo(destination);
    }

    public void SendToMaintenance(string reason, DateTime? until)
    {
        if (IsInUse)
            throw new DomainException($"Aircraft {Registration} is in use and cannot go into maintenance.");
        if (Status == AircraftStatus.Maintenance)
            throw new DomainException($"Aircraft {Registration} is already in maintenance.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Enter the maintenance reason.");
        if (until is { } date && date <= DateTime.UtcNow)
            throw new DomainException("The maintenance end date must be in the future.");

        Status = AircraftStatus.Maintenance;
        MaintenanceReason = reason.Trim();
        MaintenanceStartedAt = DateTime.UtcNow;
        MaintenanceUntil = until;
    }

    public void StartCheck(MaintenanceCheck check, string airportIcao)
    {
        SendToMaintenance($"{check}-check at {airportIcao}", DateTime.UtcNow.Add(MaintenancePolicy.DurationOf(check)));
        CurrentCheck = check;
    }

    public (MaintenanceCheck Check, int UsedMinutes, int IntervalHours) NextCheckStatus()
    {
        var checks = new[] { MaintenanceCheck.D, MaintenanceCheck.C, MaintenanceCheck.A }
            .Select(c => (Check: c, UsedMinutes: MinutesSince(c), IntervalHours: AircraftType.IntervalHoursOf(c)))
            .ToList();

        return DueCheck is { } due
            ? checks.First(c => c.Check == due)
            : checks.MinBy(c => c.IntervalHours * 60 - c.UsedMinutes);
    }

    public int MinutesSince(MaintenanceCheck check) => check switch
    {
        MaintenanceCheck.A => MinutesSinceMaintenance,
        MaintenanceCheck.C => MinutesSinceCCheck,
        _ => MinutesSinceDCheck
    };

    public MaintenanceCheck? ReturnFromMaintenance()
    {
        if (Status != AircraftStatus.Maintenance)
            throw new DomainException($"Aircraft {Registration} is not in maintenance.");

        var completed = CurrentCheck;

        if (CurrentCheck == MaintenanceCheck.D) MinutesSinceDCheck = 0;
        if (CurrentCheck is MaintenanceCheck.C or MaintenanceCheck.D) MinutesSinceCCheck = 0;

        Status = AircraftStatus.Available;
        MinutesSinceMaintenance = 0;
        CurrentCheck = null;
        LastMaintenanceAt = DateTime.UtcNow;
        MaintenanceReason = null;
        MaintenanceStartedAt = null;
        MaintenanceUntil = null;

        return completed;
    }

    public void Deactivate()
    {
        if (IsInUse)
            throw new DomainException($"Aircraft {Registration} is in use and cannot be deactivated.");
        if (Status == AircraftStatus.Maintenance)
            throw new DomainException($"Aircraft {Registration} is in maintenance. Release it first.");

        Status = AircraftStatus.Inactive;
    }

    public void Activate()
    {
        if (Status == AircraftStatus.Inactive)
            Status = AircraftStatus.Available;
    }

    private static void EnsureOperationAt(Airport airport, AircraftType type)
    {
        if (!airport.Supports(type.Operation))
            throw new DomainException($"{airport.Icao} doesn't have {(type.IsCargo ? "cargo" : "passenger")} operations.");
    }

    private void MoveTo(Airport destination)
    {
        CurrentAirport = destination;
        CurrentAirportId = destination.Id;
    }
}