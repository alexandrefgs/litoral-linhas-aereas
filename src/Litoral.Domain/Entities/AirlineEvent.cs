using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class AirlineEvent : Entity
{
    public const int MaxTitleLength = 100;
    public const int MaxDescriptionLength = 2000;
    public const int MaxAirports = 20;
    public const int MaxBonusPoints = 10_000;

    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public string Airports { get; private set; } = "";
    public int BonusPoints { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<string> AirportList => Airports.Split(',', StringSplitOptions.RemoveEmptyEntries);

    private AirlineEvent() { }

    public AirlineEvent(string title, string description, DateTime startsAt, DateTime endsAt, IEnumerable<string> airports, int bonusPoints)
    {
        Update(title, description, startsAt, endsAt, airports, bonusPoints);
    }

    public void Update(string title, string description, DateTime startsAt, DateTime endsAt, IEnumerable<string> airports, int bonusPoints)
    {
        var t = title?.Trim() ?? "";
        var d = description?.Trim() ?? "";
        var list = airports.Select(a => (a ?? "").Trim().ToUpperInvariant()).Where(a => a.Length > 0).Distinct().ToList();

        if (t.Length == 0)
            throw new DomainException("The event title is required.");
        if (t.Length > MaxTitleLength)
            throw new DomainException($"The event title must have up to {MaxTitleLength} characters.");
        if (d.Length == 0)
            throw new DomainException("The event description is required.");
        if (d.Length > MaxDescriptionLength)
            throw new DomainException($"The event description must have up to {MaxDescriptionLength} characters.");
        if (endsAt <= startsAt)
            throw new DomainException("The event must end after it starts.");
        if (list.Count > MaxAirports)
            throw new DomainException($"An event can have up to {MaxAirports} airports.");
        if (list.Any(a => a.Length != 4))
            throw new DomainException("Use 4-letter ICAO codes for the airports.");
        if (bonusPoints is < 0 or > MaxBonusPoints)
            throw new DomainException($"The bonus must be between 0 and {MaxBonusPoints:N0} points.");

        Title = t;
        Description = d;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Airports = string.Join(',', list);
        BonusPoints = bonusPoints;
    }

    public string? SetImage(string? url)
    {
        var previous = ImageUrl;
        ImageUrl = url;
        return previous;
    }

    public bool SetPublished(bool published, DateTime now)
    {
        var firstTime = published && PublishedAt is null;
        IsPublished = published;
        if (firstTime) PublishedAt = now;
        return firstTime;
    }

    public bool Covers(string departureIcao, string arrivalIcao, DateTime at)
    {
        if (!IsPublished || at < StartsAt || at >= EndsAt) return false;
        var airports = AirportList;
        return airports.Count == 0 || airports.Contains(departureIcao) || airports.Contains(arrivalIcao);
    }
}

public sealed class AirlineEventFlight : Entity
{
    public Guid EventId { get; private set; }
    public AirlineEvent Event { get; private set; } = null!;
    public Guid PilotId { get; private set; }
    public Pilot Pilot { get; private set; } = null!;
    public Guid PirepId { get; private set; }
    public int Points { get; private set; }
    public DateTime At { get; private set; } = DateTime.UtcNow;

    private AirlineEventFlight() { }

    public AirlineEventFlight(AirlineEvent airlineEvent, Pilot pilot, Guid pirepId, int points)
    {
        Event = airlineEvent;
        EventId = airlineEvent.Id;
        Pilot = pilot;
        PilotId = pilot.Id;
        PirepId = pirepId;
        Points = points;
    }
}