using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class MelItem : Entity
{
    public const int MaxCodeLength = 12;
    public const int MaxTitleLength = 120;
    public const int MaxTextLength = 600;
    public const int MaxAircraftTypesLength = 60;
    public const int MaxCheckTextLength = 40;

    public string Code { get; private set; } = null!;
    public MelKind Kind { get; private set; }
    public string Title { get; private set; } = null!;
    public MelCategory Category { get; private set; }
    public int? CategoryADays { get; private set; }
    public string Restriction { get; private set; } = null!;
    public string Procedure { get; private set; } = null!;
    public MelVerification Verification { get; private set; }
    public MelAcarsCheck AcarsCheck { get; private set; }
    public int? CheckValue { get; private set; }
    public string? CheckText { get; private set; }
    public decimal? FuelPenaltyPercent { get; private set; }
    public string AircraftTypes { get; private set; } = "";
    public bool AutoEligible { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<string> AircraftTypeList => AircraftTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);
    public int Days => Category switch
    {
        MelCategory.A => CategoryADays ?? 1,
        MelCategory.B => 3,
        MelCategory.C => 10,
        _ => 120
    };

    private MelItem() { }

    public MelItem(
        string code, MelKind kind, string title, MelCategory category, int? categoryADays,
        string restriction, string procedure, MelVerification verification, MelAcarsCheck acarsCheck,
        int? checkValue, string? checkText, decimal? fuelPenaltyPercent, IEnumerable<string> aircraftTypes, bool autoEligible, bool isActive = true)
    {
        Update(code, kind, title, category, categoryADays, restriction, procedure, verification, acarsCheck,
            checkValue, checkText, fuelPenaltyPercent, aircraftTypes, autoEligible, isActive);
    }

    public void Update(
        string code, MelKind kind, string title, MelCategory category, int? categoryADays,
        string restriction, string procedure, MelVerification verification, MelAcarsCheck acarsCheck,
        int? checkValue, string? checkText, decimal? fuelPenaltyPercent, IEnumerable<string> aircraftTypes, bool autoEligible, bool isActive)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        var t = (title ?? "").Trim();
        var r = (restriction ?? "").Trim();
        var p = (procedure ?? "").Trim();
        var text = string.IsNullOrWhiteSpace(checkText) ? null : checkText.Trim().ToUpperInvariant();
        var types = aircraftTypes.Select(a => (a ?? "").Trim().ToUpperInvariant()).Where(a => a.Length > 0).Distinct().OrderBy(a => a).ToList();

        if (c.Length == 0 || c.Length > MaxCodeLength)
            throw new DomainException($"The item code is required and must have up to {MaxCodeLength} characters, e.g. 21-01.");
        if (t.Length == 0 || t.Length > MaxTitleLength)
            throw new DomainException($"The title is required and must have up to {MaxTitleLength} characters.");
        if (r.Length == 0 || r.Length > MaxTextLength)
            throw new DomainException($"The restriction is required and must have up to {MaxTextLength} characters.");
        if (p.Length == 0 || p.Length > MaxTextLength)
            throw new DomainException($"The procedure is required and must have up to {MaxTextLength} characters.");
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(category) || !Enum.IsDefined(verification) || !Enum.IsDefined(acarsCheck))
            throw new DomainException("Invalid option.");
        if (category == MelCategory.A && categoryADays is not (>= 1 and <= 30))
            throw new DomainException("Category A items need a deadline between 1 and 30 days.");
        if (verification == MelVerification.Acars && acarsCheck == MelAcarsCheck.None)
            throw new DomainException("Choose what the ACARS checks.");
        if (acarsCheck == MelAcarsCheck.MaxAltitude && checkValue is not (>= 5000 and <= 45000))
            throw new DomainException("Enter the maximum altitude in feet, e.g. 25000.");
        if (acarsCheck == MelAcarsCheck.RemarksContain && (text is null || text.Length > MaxCheckTextLength))
            throw new DomainException($"Enter the required remarks text, up to {MaxCheckTextLength} characters.");
        if (fuelPenaltyPercent is < 0 or > 20)
            throw new DomainException("The fuel penalty must be between 0 and 20%.");
        if (string.Join(',', types).Length > MaxAircraftTypesLength)
            throw new DomainException("Too many aircraft types.");
        if (kind == MelKind.Cdl && autoEligible)
            throw new DomainException("CDL items are opened by the staff only.");
        if (autoEligible && category is MelCategory.A or MelCategory.B)
            throw new DomainException("Only category C and D items can be opened by the system.");

        Code = c;
        Kind = kind;
        Title = t;
        Category = category;
        CategoryADays = category == MelCategory.A ? categoryADays : null;
        Restriction = r;
        Procedure = p;
        Verification = verification;
        AcarsCheck = verification == MelVerification.Acars ? acarsCheck : MelAcarsCheck.None;
        CheckValue = AcarsCheck == MelAcarsCheck.MaxAltitude ? checkValue : null;
        CheckText = AcarsCheck == MelAcarsCheck.RemarksContain ? text : null;
        FuelPenaltyPercent = fuelPenaltyPercent is 0 ? null : fuelPenaltyPercent;
        AircraftTypes = string.Join(',', types);
        AutoEligible = autoEligible;
        IsActive = isActive;
    }

    public bool AppliesTo(string aircraftTypeIcao) =>
        AircraftTypeList.Count == 0 || AircraftTypeList.Contains(aircraftTypeIcao);
}

public sealed class AircraftMelItem : Entity
{
    public const int MaxNoteLength = 300;

    public Guid AircraftId { get; private set; }
    public Aircraft Aircraft { get; private set; } = null!;
    public Guid MelItemId { get; private set; }
    public MelItem MelItem { get; private set; } = null!;
    public MelSource Source { get; private set; }
    public string OpenedBy { get; private set; } = null!;
    public string? Note { get; private set; }
    public DateTime OpenedAt { get; private set; } = DateTime.UtcNow;
    public DateTime DueAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? ClosedBy { get; private set; }
    public string? CloseReason { get; private set; }

    public bool IsOpen => ClosedAt is null;
    public bool IsExpired(DateTime now) => IsOpen && now >= DueAt;

    private AircraftMelItem() { }

    public AircraftMelItem(Aircraft aircraft, MelItem item, MelSource source, string openedBy, string? note)
    {
        var n = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (n?.Length > MaxNoteLength)
            throw new DomainException($"The note must have up to {MaxNoteLength} characters.");

        Aircraft = aircraft;
        AircraftId = aircraft.Id;
        MelItem = item;
        MelItemId = item.Id;
        Source = source;
        OpenedBy = openedBy;
        Note = n;
        DueAt = OpenedAt.AddDays(item.Days);
    }

    public void Close(string closedBy, string reason, DateTime now)
    {
        if (!IsOpen) return;
        ClosedAt = now;
        ClosedBy = closedBy;
        CloseReason = reason;
    }
}

public sealed class FlightMelCheck : Entity
{
    public Guid PirepId { get; private set; }
    public Guid AircraftMelItemId { get; private set; }
    public AircraftMelItem AircraftMelItem { get; private set; } = null!;
    public MelCheckResult Result { get; private set; }
    public string? Detail { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecidedBy { get; private set; }

    private FlightMelCheck() { }

    public FlightMelCheck(Guid pirepId, AircraftMelItem item, MelCheckResult result, string? detail, string? decidedBy)
    {
        PirepId = pirepId;
        AircraftMelItem = item;
        AircraftMelItemId = item.Id;
        Result = result;
        Detail = detail;
        if (result != MelCheckResult.Pending)
        {
            DecidedAt = DateTime.UtcNow;
            DecidedBy = decidedBy;
        }
    }

    public void Decide(bool complied, string decidedBy)
    {
        if (Result != MelCheckResult.Pending)
            throw new DomainException("This MEL item was already decided.");

        Result = complied ? MelCheckResult.Complied : MelCheckResult.Violated;
        DecidedAt = DateTime.UtcNow;
        DecidedBy = decidedBy;
    }
}