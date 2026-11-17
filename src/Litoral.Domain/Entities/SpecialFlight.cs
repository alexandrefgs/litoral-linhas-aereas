using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class SpecialFlight : Entity
{
    public const int MaxLegs = 2;
    public const int MaxTitleLength = 80;
    public const int MaxDescriptionLength = 1000;

    private readonly List<SpecialFlightLeg> _legs = [];

    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public SpecialFlightType Type { get; private set; }
    public int Difficulty { get; private set; }
    public Guid AircraftId { get; private set; }
    public Aircraft Aircraft { get; private set; } = null!;
    public int MinimumRankLevel { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public SpecialFlightStatus Status { get; private set; } = SpecialFlightStatus.Open;
    public Guid? PilotId { get; private set; }
    public Pilot? Pilot { get; private set; }
    public DateTime? TakenAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? PhotoCredit { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyCollection<SpecialFlightLeg> Legs => _legs;
    public IReadOnlyList<SpecialFlightLeg> OrderedLegs => _legs.OrderBy(l => l.Order).ToList();
    public SpecialFlightLeg? NextLeg => OrderedLegs.FirstOrDefault(l => l.CompletedAt is null);
    public bool IsLocked => Status != SpecialFlightStatus.Open || PilotId is not null;

    private SpecialFlight() { }

    public SpecialFlight(
        string title,
        string? description,
        SpecialFlightType type,
        int difficulty,
        Aircraft aircraft,
        int minimumRankLevel,
        DateTime startsAt,
        DateTime endsAt,
        IEnumerable<Route> legs)
    {
        Apply(title, description, type, difficulty, aircraft, minimumRankLevel, startsAt, endsAt);
        SetLegs(legs);
    }

    public void Update(
        string title,
        string? description,
        SpecialFlightType type,
        int difficulty,
        Aircraft aircraft,
        int minimumRankLevel,
        DateTime startsAt,
        DateTime endsAt,
        IEnumerable<Route> legs)
    {
        if (IsLocked)
            throw new DomainException("This special flight was already taken and can't be edited.");

        Apply(title, description, type, difficulty, aircraft, minimumRankLevel, startsAt, endsAt);
        SetLegs(legs);
    }

    public bool IsAvailableAt(DateTime now) =>
        Status == SpecialFlightStatus.Open && now >= StartsAt && now < EndsAt;

    public void Take(Pilot pilot, DateTime now)
    {
        if (Status != SpecialFlightStatus.Open || PilotId is not null)
            throw new DomainException("This special flight is no longer available.");
        if (now < StartsAt)
            throw new DomainException("This special flight is not available yet.");
        if (now >= EndsAt)
            throw new DomainException("This special flight has ended.");
        if (pilot.Rank.Level < MinimumRankLevel)
            throw new DomainException("Your rank does not allow this special flight.");

        var first = OrderedLegs[0];
        if (pilot.CurrentLocationIcao != first.Route.DepartureAirport.Icao)
            throw new DomainException($"You must be at {first.Route.DepartureAirport.Icao} to take this special flight.");

        Pilot = pilot;
        PilotId = pilot.Id;
        TakenAt = now;
        Status = SpecialFlightStatus.Taken;
    }

    public void AttachBooking(SpecialFlightLeg leg, Booking booking)
    {
        if (Status != SpecialFlightStatus.Taken || booking.PilotId != PilotId)
            throw new DomainException("This special flight belongs to another pilot.");
        if (NextLeg?.Id != leg.Id)
            throw new DomainException("This is not the next leg of the special flight.");

        leg.AttachBooking(booking);
    }

    public void CompleteLeg(Guid bookingId, DateTime now)
    {
        var leg = _legs.FirstOrDefault(l => l.BookingId == bookingId)
            ?? throw new DomainException("Leg not found for this booking.");

        leg.Complete(now);

        if (_legs.All(l => l.CompletedAt is not null))
        {
            Status = SpecialFlightStatus.Completed;
            ClosedAt = now;
        }
    }

    public void ReleaseLeg(Guid bookingId, DateTime now)
    {
        var leg = _legs.FirstOrDefault(l => l.BookingId == bookingId);
        if (leg is null || leg.CompletedAt is not null) return;

        leg.DetachBooking();

        if (_legs.Any(l => l.CompletedAt is not null))
        {
            Status = SpecialFlightStatus.Expired;
            ClosedAt = now;
            return;
        }

        Pilot = null;
        PilotId = null;
        TakenAt = null;
        Status = now < EndsAt ? SpecialFlightStatus.Open : SpecialFlightStatus.Expired;
        if (Status == SpecialFlightStatus.Expired) ClosedAt = now;
    }

    public void Expire(DateTime now)
    {
        if (Status is SpecialFlightStatus.Completed or SpecialFlightStatus.Expired or SpecialFlightStatus.Cancelled) return;

        Status = SpecialFlightStatus.Expired;
        ClosedAt = now;
    }

    public void Cancel(DateTime now)
    {
        if (Status is SpecialFlightStatus.Completed or SpecialFlightStatus.Expired or SpecialFlightStatus.Cancelled)
            throw new DomainException("This special flight is already closed.");

        Status = SpecialFlightStatus.Cancelled;
        ClosedAt = now;
    }

    public string? SetPhoto(string? url, string? credit)
    {
        var previous = PhotoUrl;
        PhotoUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        SetPhotoCredit(credit);
        return previous;
    }

    public void SetPhotoCredit(string? credit)
    {
        var clean = PhotoUrl is null || string.IsNullOrWhiteSpace(credit) ? null : credit.Trim();
        if (clean?.Length > 150)
            throw new DomainException("Photo credit must have up to 150 characters.");

        PhotoCredit = clean;
    }

    private void Apply(
        string title,
        string? description,
        SpecialFlightType type,
        int difficulty,
        Aircraft aircraft,
        int minimumRankLevel,
        DateTime startsAt,
        DateTime endsAt)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > MaxTitleLength)
            throw new DomainException($"Title is required and must have up to {MaxTitleLength} characters.");
        if (description?.Trim().Length > MaxDescriptionLength)
            throw new DomainException($"Description must have up to {MaxDescriptionLength} characters.");
        if (!Enum.IsDefined(type))
            throw new DomainException("Invalid special flight type.");
        if (difficulty is < 1 or > 5)
            throw new DomainException("Difficulty must be between 1 and 5.");
        if (minimumRankLevel < 1)
            throw new DomainException("Minimum rank is required.");
        if (endsAt <= startsAt)
            throw new DomainException("The end must be after the start.");

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Type = type;
        Difficulty = difficulty;
        Aircraft = aircraft;
        AircraftId = aircraft.Id;
        MinimumRankLevel = minimumRankLevel;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    private void SetLegs(IEnumerable<Route> routes)
    {
        var list = routes.ToList();

        if (list.Count is < 1 or > MaxLegs)
            throw new DomainException($"A special flight must have 1 to {MaxLegs} legs.");
        if (list.Any(r => !r.IsSpecial))
            throw new DomainException("Special flight legs must use special routes.");
        if (list.Any(r => !r.AllowsAircraftType(Aircraft.AircraftTypeId)))
            throw new DomainException($"Aircraft {Aircraft.Registration} can't fly every leg.");
        if (list[0].DepartureAirportId != Aircraft.CurrentAirportId)
            throw new DomainException($"Aircraft {Aircraft.Registration} is not at {list[0].DepartureAirport.Icao}.");

        for (var i = 1; i < list.Count; i++)
            if (list[i].DepartureAirportId != list[i - 1].ArrivalAirportId)
                throw new DomainException("Each leg must depart from where the previous one arrives.");

        _legs.Clear();
        _legs.AddRange(list.Select((route, index) => new SpecialFlightLeg(this, index + 1, route)));
    }
}

public sealed class SpecialFlightLeg : Entity
{
    public Guid SpecialFlightId { get; private set; }
    public SpecialFlight SpecialFlight { get; private set; } = null!;
    public int Order { get; private set; }
    public Guid RouteId { get; private set; }
    public Route Route { get; private set; } = null!;
    public Guid? BookingId { get; private set; }
    public Booking? Booking { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private SpecialFlightLeg() { }

    internal SpecialFlightLeg(SpecialFlight flight, int order, Route route)
    {
        SpecialFlight = flight;
        SpecialFlightId = flight.Id;
        Order = order;
        Route = route;
        RouteId = route.Id;
    }

    internal void AttachBooking(Booking booking)
    {
        if (BookingId is not null && Booking?.IsOpen == true)
            throw new DomainException("This leg is already booked.");
        if (booking.RouteId != RouteId)
            throw new DomainException("The booking is not for this leg.");

        Booking = booking;
        BookingId = booking.Id;
    }

    internal void DetachBooking()
    {
        Booking = null;
        BookingId = null;
    }

    internal void Complete(DateTime now)
    {
        if (CompletedAt is not null)
            throw new DomainException("This leg is already completed.");

        CompletedAt = now;
    }
}