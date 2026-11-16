using System.Text.RegularExpressions;
using Litoral.Domain.Common;
using Litoral.Domain.Scoring;

namespace Litoral.Domain.Entities;

public sealed partial class ScoringRule : Entity
{
    public const int MaxDescriptionLength = 200;
    public const int MaxTextLength = 100;
    public const int MaxGraceSeconds = 600;
    public const int MaxPoints = 100_000;

    public ScoringCategory Category { get; private set; }
    public ScoringCheck Check { get; private set; }
    public string Description { get; private set; } = null!;
    public ScoringOutcome Outcome { get; private set; }
    public int Points { get; private set; }
    public ScoringMode Mode { get; private set; }
    public decimal? Min { get; private set; }
    public decimal? Max { get; private set; }
    public string? Text { get; private set; }
    public string? AircraftTypes { get; private set; }
    public int GraceSeconds { get; private set; }
    public int Order { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ScoringRule() { }

    public ScoringRule(
        ScoringCategory category, ScoringCheck check, string description, ScoringOutcome outcome, int points,
        ScoringMode mode = ScoringMode.Once, decimal? min = null, decimal? max = null, string? text = null,
        string? aircraftTypes = null, int graceSeconds = 0, int order = 0)
    {
        Update(category, check, description, outcome, points, mode, min, max, text, aircraftTypes, graceSeconds, order);
    }

    public IReadOnlyList<string> AircraftTypeList =>
        string.IsNullOrEmpty(AircraftTypes) ? [] : AircraftTypes.Split(',');

    public bool AppliesTo(string aircraftTypeIcao) =>
        AircraftTypeList.Count == 0 || AircraftTypeList.Contains(aircraftTypeIcao.ToUpperInvariant());

    public void Update(
        ScoringCategory category, ScoringCheck check, string description, ScoringOutcome outcome, int points,
        ScoringMode mode, decimal? min, decimal? max, string? text, string? aircraftTypes, int graceSeconds, int order)
    {
        if (!Enum.IsDefined(category)) throw new DomainException("Choose a valid category.");
        if (!Enum.IsDefined(check)) throw new DomainException("Choose a valid check.");
        if (!Enum.IsDefined(outcome)) throw new DomainException("Choose a valid outcome.");

        var info = ScoringCatalog.Get(check);

        description = description?.Trim() ?? "";
        if (description.Length == 0) throw new DomainException("Enter the rule description.");
        if (description.Length > MaxDescriptionLength)
            throw new DomainException($"The description must have up to {MaxDescriptionLength} characters.");

        if (!info.Modes.Contains(mode)) throw new DomainException($"This check can't be applied {Describe(mode)}.");

        if (outcome == ScoringOutcome.Reject) points = 0;
        if (Math.Abs(points) > MaxPoints) throw new DomainException($"Points must be between -{MaxPoints} and {MaxPoints}.");

        min = info.MinLabel is null ? null : min;
        max = info.MaxLabel is null ? null : max;
        text = info.TextLabel is null ? null : text?.Trim();

        if (info.MinLabel is not null && info.MaxLabel is not null)
        {
            if (min is null && max is null) throw new DomainException("Enter at least one limit.");
            if (min is not null && max is not null && min > max)
                throw new DomainException("The lower limit can't be greater than the upper limit.");
        }
        else
        {
            if (info.MinLabel is not null && min is null) throw new DomainException($"Enter the value for \"{info.MinLabel}\".");
            if (info.MaxLabel is not null && max is null) throw new DomainException($"Enter the value for \"{info.MaxLabel}\".");
        }

        if (info.TextLabel is not null && string.IsNullOrEmpty(text))
            throw new DomainException($"Enter the value for \"{info.TextLabel}\".");
        if (text is { Length: > MaxTextLength })
            throw new DomainException($"\"{info.TextLabel}\" must have up to {MaxTextLength} characters.");

        if (graceSeconds < 0 || graceSeconds > MaxGraceSeconds)
            throw new DomainException($"The grace period must be between 0 and {MaxGraceSeconds} seconds.");

        Category = category;
        Check = check;
        Description = description;
        Outcome = outcome;
        Points = points;
        Mode = mode;
        Min = min;
        Max = max;
        Text = string.IsNullOrEmpty(text) ? null : text;
        AircraftTypes = NormalizeAircraftTypes(aircraftTypes);
        GraceSeconds = graceSeconds;
        Order = order;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string? NormalizeAircraftTypes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var codes = value.ToUpperInvariant()
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToList();

        var invalid = codes.FirstOrDefault(c => !IcaoTypePattern().IsMatch(c));
        if (invalid is not null) throw new DomainException($"\"{invalid}\" is not a valid aircraft ICAO code.");

        return string.Join(',', codes);
    }

    private static string Describe(ScoringMode mode) => mode switch
    {
        ScoringMode.PerOccurrence => "per occurrence",
        ScoringMode.PerSecond => "per second",
        ScoringMode.PerDay => "per day",
        _ => "once"
    };

    [GeneratedRegex("^[A-Z0-9]{2,4}$")]
    private static partial Regex IcaoTypePattern();
}