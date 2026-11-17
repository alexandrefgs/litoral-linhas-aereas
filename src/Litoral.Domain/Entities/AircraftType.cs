using System.Text.RegularExpressions;
using Litoral.Domain.Common;
using Litoral.Domain.Enums;
using Litoral.Domain.Fleet;

namespace Litoral.Domain.Entities;

public sealed class AircraftType : Entity
{
    public const int DefaultMaintenanceIntervalHours = 500;
    public const int DefaultCCheckIntervalHours = 5000;
    public const int DefaultDCheckIntervalHours = 20000;
    public const string RegistrationNationality = "PS-";

    private static readonly Regex PrefixPattern = new("^[A-Z]{2}$");

    public string IcaoCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsCargo { get; private set; }
    public int MaxRangeNm { get; private set; }
    public int MaintenanceIntervalHours { get; private set; } = DefaultMaintenanceIntervalHours;
    public int CCheckIntervalHours { get; private set; } = DefaultCCheckIntervalHours;
    public int DCheckIntervalHours { get; private set; } = DefaultDCheckIntervalHours;
    public string? RegistrationPrefix { get; private set; }
    public string? DeliveryAirportIcao { get; private set; }

    public string? RegistrationStart => RegistrationPrefix is null ? null : $"{RegistrationNationality}{RegistrationPrefix}";

    public OperationType Operation => IsCargo ? OperationType.Cargo : OperationType.Passenger;

    private AircraftType() { }

    public AircraftType(
        string icaoCode,
        string name,
        bool isCargo,
        int maxRangeNm,
        string registrationPrefix,
        int maintenanceIntervalHours = DefaultMaintenanceIntervalHours,
        int cCheckIntervalHours = DefaultCCheckIntervalHours,
        int dCheckIntervalHours = DefaultDCheckIntervalHours)
    {
        Update(icaoCode, name, isCargo, maxRangeNm, registrationPrefix, maintenanceIntervalHours, cCheckIntervalHours, dCheckIntervalHours);
    }

    public int IntervalHoursOf(MaintenanceCheck check) => check switch
    {
        MaintenanceCheck.A => MaintenanceIntervalHours,
        MaintenanceCheck.C => CCheckIntervalHours,
        _ => DCheckIntervalHours
    };

    public void Update(
        string icaoCode,
        string name,
        bool isCargo,
        int maxRangeNm,
        string registrationPrefix,
        int maintenanceIntervalHours,
        int cCheckIntervalHours,
        int dCheckIntervalHours)
    {
        var code = (icaoCode ?? string.Empty).Trim().ToUpperInvariant();

        if (code.Length != 4)
            throw new DomainException("Aircraft ICAO code must have 4 characters.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Aircraft name is required.");
        if (maxRangeNm <= 0)
            throw new DomainException("Range must be greater than zero.");
        if (maintenanceIntervalHours <= 0)
            throw new DomainException("A-check interval must be greater than zero.");
        if (cCheckIntervalHours <= maintenanceIntervalHours)
            throw new DomainException("C-check interval must be greater than the A-check interval.");
        if (dCheckIntervalHours <= cCheckIntervalHours)
            throw new DomainException("D-check interval must be greater than the C-check interval.");

        AssignRegistrationPrefix(registrationPrefix);
        IcaoCode = code;
        Name = name.Trim();
        IsCargo = isCargo;
        MaxRangeNm = maxRangeNm;
        MaintenanceIntervalHours = maintenanceIntervalHours;
        CCheckIntervalHours = cCheckIntervalHours;
        DCheckIntervalHours = dCheckIntervalHours;
    }

    public void SetDeliveryAirport(string? icao)
    {
        var code = string.IsNullOrWhiteSpace(icao) ? null : icao.Trim().ToUpperInvariant();
        if (code is not null && (code.Length != 4 || !code.All(char.IsAsciiLetterOrDigit)))
            throw new DomainException("Delivery airport must be a 4-character ICAO code.");

        DeliveryAirportIcao = code;
    }

    public void AssignRegistrationPrefix(string registrationPrefix)
    {
        var prefix = (registrationPrefix ?? string.Empty).Trim().ToUpperInvariant();

        if (!PrefixPattern.IsMatch(prefix))
            throw new DomainException("Registration prefix must have 2 letters, e.g. LA.");
        if (RegistrationMarks.ProhibitedReason(prefix) is { } reason)
            throw new DomainException($"Prefix {RegistrationNationality}{prefix} is prohibited by ANAC: {reason}.");

        RegistrationPrefix = prefix;
    }
}