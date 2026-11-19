using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class Tour : Entity
{
    public const int MaxTitleLength = Award.MaxNameLength;
    public const int MaxDescriptionLength = 2000;
    public const int MaxLegs = 30;
    public const int MaxBonusPoints = 100_000;

    private readonly List<TourLeg> _legs = [];

    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public DateTime? StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public int MinimumRankLevel { get; private set; } = 1;
    public int BonusPoints { get; private set; }
    public Guid AwardId { get; private set; }
    public Award Award { get; private set; } = null!;
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyCollection<TourLeg> Legs => _legs;
    public IReadOnlyList<TourLeg> OrderedLegs => _legs.OrderBy(l => l.Order).ToList();

    private Tour() { }

    public Tour(string title, string description, int minimumRankLevel, int bonusPoints, DateTime? startsAt, DateTime? endsAt, Award award, IReadOnlyList<Route> routes)
    {
        Update(title, description, minimumRankLevel, bonusPoints, startsAt, endsAt);
        Award = award;
        AwardId = award.Id;
        ReplaceLegs(routes);
    }

    public void Update(string title, string description, int minimumRankLevel, int bonusPoints, DateTime? startsAt, DateTime? endsAt)
    {
        var t = title?.Trim() ?? "";
        var d = description?.Trim() ?? "";

        if (t.Length == 0)
            throw new DomainException("The tour title is required.");
        if (t.Length > MaxTitleLength)
            throw new DomainException($"The tour title must have up to {MaxTitleLength} characters.");
        if (d.Length == 0)
            throw new DomainException("The tour description is required.");
        if (d.Length > MaxDescriptionLength)
            throw new DomainException($"The tour description must have up to {MaxDescriptionLength} characters.");
        if (bonusPoints is < 0 or > MaxBonusPoints)
            throw new DomainException($"The bonus must be between 0 and {MaxBonusPoints:N0} points.");
        if (minimumRankLevel < 1)
            throw new DomainException("Select the minimum rank.");
        if (startsAt is { } s && endsAt is { } e && e <= s)
            throw new DomainException("The tour must end after it starts.");

        Title = t;
        Description = d;
        MinimumRankLevel = minimumRankLevel;
        BonusPoints = bonusPoints;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public void ReplaceLegs(IReadOnlyList<Route> routes)
    {
        if (routes.Count is < 2 or > MaxLegs)
            throw new DomainException($"A tour needs between 2 and {MaxLegs} legs.");

        for (var i = 1; i < routes.Count; i++)
        {
            if (routes[i].DepartureAirportId != routes[i - 1].ArrivalAirportId)
                throw new DomainException($"Leg {i + 1} must depart from where leg {i} arrives.");
        }

        _legs.Clear();
        _legs.AddRange(routes.Select((r, i) => new TourLeg(this, i + 1, r)));
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

    public bool HasStarted(DateTime now) => StartsAt is null || now >= StartsAt;
    public bool HasEnded(DateTime now) => EndsAt is { } end && now >= end;
    public bool IsOpenAt(DateTime now) => IsPublished && HasStarted(now) && !HasEnded(now);
}

public sealed class TourLeg : Entity
{
    public Guid TourId { get; private set; }
    public Tour Tour { get; private set; } = null!;
    public int Order { get; private set; }
    public Guid RouteId { get; private set; }
    public Route Route { get; private set; } = null!;

    private TourLeg() { }

    internal TourLeg(Tour tour, int order, Route route)
    {
        Tour = tour;
        TourId = tour.Id;
        Order = order;
        Route = route;
        RouteId = route.Id;
    }
}

public sealed class TourProgress : Entity
{
    public Guid TourId { get; private set; }
    public Tour Tour { get; private set; } = null!;
    public Guid PilotId { get; private set; }
    public Pilot Pilot { get; private set; } = null!;
    public int CompletedLegs { get; private set; }
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastLegAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public bool IsCompleted => CompletedAt is not null;

    private TourProgress() { }

    public TourProgress(Tour tour, Pilot pilot)
    {
        Tour = tour;
        TourId = tour.Id;
        Pilot = pilot;
        PilotId = pilot.Id;
    }

    public bool CompleteLeg(int order, int totalLegs, DateTime now)
    {
        if (IsCompleted || order != CompletedLegs + 1)
            return false;

        CompletedLegs = order;
        LastLegAt = now;
        if (CompletedLegs >= totalLegs)
            CompletedAt = now;
        return true;
    }
}