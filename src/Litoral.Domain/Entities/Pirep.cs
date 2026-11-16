using Litoral.Domain.Common;
using Litoral.Domain.Enums;
using Litoral.Domain.Flights;
using Litoral.Domain.Scoring;

namespace Litoral.Domain.Entities;

public sealed record PirepReport(
    DateTime BlockOffAt,
    DateTime BlockOnAt,
    DateTime? TakeoffAt,
    DateTime? LandingAt,
    int? LandingRateFpm,
    decimal? FuelUsedKg,
    decimal? FuelAtPushbackKg,
    int? DistanceNm,
    string? FlightRoute,
    int? CruiseAltitudeFt,
    decimal? OnlinePercent,
    string? Comments);

public sealed class Pirep : Entity
{
    public const int MaxCommentsLength = 1000;
    public const int MaxBlockMinutes = 24 * 60;

    private readonly List<PirepScoreItem> _items = [];
    private readonly List<PirepEvent> _events = [];

    public Guid BookingId { get; private set; }
    public Booking Booking { get; private set; } = null!;
    public Guid PilotId { get; private set; }
    public Pilot Pilot { get; private set; } = null!;
    public Guid? FlightSessionId { get; private set; }
    public FlightSession? FlightSession { get; private set; }
    public PirepSource Source { get; private set; }
    public PirepStatus Status { get; private set; } = PirepStatus.Pending;
    public FlightNetwork Network { get; private set; }
    public string Callsign { get; private set; } = null!;
    public string DepartureIcao { get; private set; } = null!;
    public string PlannedArrivalIcao { get; private set; } = null!;
    public string ArrivalIcao { get; private set; } = null!;
    public string Registration { get; private set; } = null!;
    public string AircraftTypeIcao { get; private set; } = null!;
    public DateTime BlockOffAt { get; private set; }
    public DateTime BlockOnAt { get; private set; }
    public DateTime? TakeoffAt { get; private set; }
    public DateTime? LandingAt { get; private set; }
    public int BlockMinutes { get; private set; }
    public int? AirMinutes { get; private set; }
    public int? LandingRateFpm { get; private set; }
    public decimal? FuelUsedKg { get; private set; }
    public decimal? FuelAtPushbackKg { get; private set; }
    public int? DistanceNm { get; private set; }
    public string? FlightRoute { get; private set; }
    public int? CruiseAltitudeFt { get; private set; }
    public decimal? OnlinePercent { get; private set; }
    public string? Comments { get; private set; }
    public int ScorePoints { get; private set; }
    public bool HasRejection { get; private set; }
    public int Conformity { get; private set; }
    public DateTime SubmittedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? ReviewNote { get; private set; }

    public IReadOnlyCollection<PirepScoreItem> Items => _items;
    public IReadOnlyCollection<PirepEvent> Events => _events;

    public bool IsDiversion => ArrivalIcao != PlannedArrivalIcao;
    public bool IsPending => Status == PirepStatus.Pending;

    private Pirep() { }

    public Pirep(Booking booking, FlightSession? session, PirepSource source, FlightNetwork network, string arrivalIcao, PirepReport report)
    {
        if (report.BlockOnAt <= report.BlockOffAt)
            throw new DomainException("The block on time must be after the block off time.");
        if (report.BlockOnAt > DateTime.UtcNow.AddMinutes(5))
            throw new DomainException("The block on time can't be in the future.");

        var blockMinutes = (int)Math.Round((report.BlockOnAt - report.BlockOffAt).TotalMinutes);
        if (blockMinutes < 1 || blockMinutes > MaxBlockMinutes)
            throw new DomainException("The block time must be between 1 minute and 24 hours.");

        if (report.TakeoffAt is DateTime takeoff && (takeoff < report.BlockOffAt || takeoff > report.BlockOnAt))
            throw new DomainException("The takeoff time must be between block off and block on.");
        if (report.LandingAt is DateTime landing && (landing < (report.TakeoffAt ?? report.BlockOffAt) || landing > report.BlockOnAt))
            throw new DomainException("The landing time must be between takeoff and block on.");

        var comments = FlightSession.Trim(report.Comments, MaxCommentsLength + 1);
        if (comments is { Length: > MaxCommentsLength })
            throw new DomainException($"Comments must have up to {MaxCommentsLength} characters.");

        Booking = booking;
        BookingId = booking.Id;
        Pilot = booking.Pilot;
        PilotId = booking.PilotId;
        FlightSession = session;
        FlightSessionId = session?.Id;
        Source = source;
        Network = network;
        Callsign = booking.Callsign;
        DepartureIcao = booking.Route.DepartureAirport.Icao;
        PlannedArrivalIcao = booking.Route.ArrivalAirport.Icao;
        ArrivalIcao = arrivalIcao.Trim().ToUpperInvariant();
        Registration = booking.Aircraft.Registration;
        AircraftTypeIcao = booking.Aircraft.AircraftType.IcaoCode;
        BlockOffAt = report.BlockOffAt;
        BlockOnAt = report.BlockOnAt;
        TakeoffAt = report.TakeoffAt;
        LandingAt = report.LandingAt;
        BlockMinutes = blockMinutes;
        AirMinutes = report is { TakeoffAt: DateTime t, LandingAt: DateTime l } ? (int)Math.Round((l - t).TotalMinutes) : null;
        LandingRateFpm = report.LandingRateFpm;
        FuelUsedKg = report.FuelUsedKg;
        FuelAtPushbackKg = report.FuelAtPushbackKg;
        DistanceNm = report.DistanceNm;
        FlightRoute = FlightSession.Trim(report.FlightRoute, 1000);
        CruiseAltitudeFt = report.CruiseAltitudeFt;
        OnlinePercent = report.OnlinePercent;
        Comments = comments;
    }

    public void RecordEvents(IEnumerable<TelemetryEvent> events)
    {
        foreach (var e in events.OrderBy(e => e.At))
            _events.Add(new PirepEvent(e.Type, e.At, e.DurationSeconds, e.Value, FlightSession.Trim(e.Detail, 200)));
    }

    public void ApplyEvaluation(EvaluationResult result)
    {
        EnsurePending();

        _items.RemoveAll(i => !i.IsManual);
        _items.AddRange(result.Items.Select(i => new PirepScoreItem(i.RuleId, i.Category, i.Check, i.Description, i.Outcome, i.Points, i.Quantity, false)));
        Recalculate();
    }

    public void AddReviewItem(ScoringRule rule, int quantity)
    {
        EnsurePending();

        if (ScoringCatalog.Get(rule.Check).Source != ScoringSource.Review)
            throw new DomainException("Only review rules can be added manually.");
        if (!rule.IsActive)
            throw new DomainException("This rule is inactive.");
        if (quantity is < 1 or > 100)
            throw new DomainException("The quantity must be between 1 and 100.");

        _items.RemoveAll(i => i.RuleId == rule.Id && i.IsManual);

        var applied = rule.Mode == ScoringMode.Once ? 1 : quantity;
        var points = rule.Outcome == ScoringOutcome.Reject ? 0 : rule.Points * applied;
        _items.Add(new PirepScoreItem(rule.Id, rule.Category, rule.Check, rule.Description, rule.Outcome, points, applied, true));
        Recalculate();
    }

    public void RemoveItem(Guid ruleId)
    {
        EnsurePending();

        var item = _items.FirstOrDefault(i => i.RuleId == ruleId)
            ?? throw new DomainException("Item not found in this PIREP.");

        if (item.IsManual) _items.Remove(item);
        else item.SetVoided(!item.IsVoided);

        Recalculate();
    }

    public void Approve(string reviewerUserId, string? note)
    {
        EnsurePending();
        if (HasRejection)
            throw new DomainException("This PIREP has a rejection item. Remove it or reject the PIREP.");

        Close(PirepStatus.Approved, reviewerUserId, note);
    }

    public void Reject(string reviewerUserId, string? note)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(note))
            throw new DomainException("Explain to the pilot why the PIREP was rejected.");

        Close(PirepStatus.Rejected, reviewerUserId, note);
    }

    private void Close(PirepStatus status, string reviewerUserId, string? note)
    {
        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note is { Length: > 1000 }) throw new DomainException("The review note must have up to 1000 characters.");

        Status = status;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewerUserId;
        ReviewNote = note;
    }

    private void EnsurePending()
    {
        if (Status != PirepStatus.Pending)
            throw new DomainException("This PIREP was already reviewed.");
    }

    private void Recalculate()
    {
        var items = _items.Where(i => !i.IsVoided).Select(i => new ScoreResultItem(i.RuleId ?? Guid.Empty, i.Category, i.Check, i.Description, i.Outcome, i.Points, i.Quantity)).ToList();
        ScorePoints = items.Sum(i => i.Points);
        HasRejection = items.Any(i => i.Outcome == ScoringOutcome.Reject);
        Conformity = EvaluationResult.ConformityOf(items);
    }
}

public sealed class PirepScoreItem
{
    public Guid? RuleId { get; private set; }
    public ScoringCategory Category { get; private set; }
    public ScoringCheck Check { get; private set; }
    public string Description { get; private set; } = null!;
    public ScoringOutcome Outcome { get; private set; }
    public int Points { get; private set; }
    public int Quantity { get; private set; }
    public bool IsManual { get; private set; }
    public bool IsVoided { get; private set; }

    private PirepScoreItem() { }

    internal void SetVoided(bool voided) => IsVoided = voided;

    internal PirepScoreItem(Guid? ruleId, ScoringCategory category, ScoringCheck check, string description,
        ScoringOutcome outcome, int points, int quantity, bool isManual)
    {
        RuleId = ruleId;
        Category = category;
        Check = check;
        Description = description;
        Outcome = outcome;
        Points = points;
        Quantity = quantity;
        IsManual = isManual;
    }
}

public sealed class PirepEvent
{
    public FlightEventType Type { get; private set; }
    public DateTime At { get; private set; }
    public int DurationSeconds { get; private set; }
    public decimal? Value { get; private set; }
    public string? Detail { get; private set; }

    private PirepEvent() { }

    internal PirepEvent(FlightEventType type, DateTime at, int durationSeconds, decimal? value, string? detail)
    {
        Type = type;
        At = at;
        DurationSeconds = Math.Max(0, durationSeconds);
        Value = value;
        Detail = detail;
    }
}