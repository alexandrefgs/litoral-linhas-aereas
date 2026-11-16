using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class MonthlyRoster : Entity
{
    public const int MaxFlightsPerDay = 3;
    public const int MinFlights = 2;
    public const int BonusPerFlight = 100;

    private readonly List<RosterLeg> _legs = [];

    public Guid PilotId { get; private set; }
    public Pilot Pilot { get; private set; } = null!;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int FlightsPerDay { get; private set; }
    public string BaseIcao { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyCollection<RosterLeg> Legs => _legs;
    public RosterLeg? NextLeg => _legs.Where(l => l.CompletedAt is null).OrderBy(l => l.Order).FirstOrDefault();

    private MonthlyRoster() { }

    public MonthlyRoster(Pilot pilot, int year, int month, int flightsPerDay, IReadOnlyList<IReadOnlyList<Route>> sequences)
    {
        if (flightsPerDay is < 1 or > MaxFlightsPerDay)
            throw new DomainException($"Choose between 1 and {MaxFlightsPerDay} flights per day.");
        if (sequences.Sum(s => s.Count) < MinFlights)
            throw new DomainException($"A roster needs at least {MinFlights} flights.");

        Pilot = pilot;
        PilotId = pilot.Id;
        Year = year;
        Month = month;
        FlightsPerDay = flightsPerDay;
        BaseIcao = pilot.BaseIcao;

        var order = 1;
        for (var s = 0; s < sequences.Count; s++)
        {
            foreach (var route in sequences[s])
                _legs.Add(new RosterLeg(this, order++, s + 1, route));
        }
    }

    public int Bonus => _legs.Count * BonusPerFlight;

    public bool CompleteLeg(Guid bookingId)
    {
        var leg = _legs.FirstOrDefault(l => l.BookingId == bookingId && l.CompletedAt is null);
        if (leg is null) return false;

        leg.Complete();

        if (_legs.All(l => l.CompletedAt is not null))
            CompletedAt = DateTime.UtcNow;

        return true;
    }
}

public sealed class RosterLeg : Entity
{
    public Guid RosterId { get; private set; }
    public MonthlyRoster Roster { get; private set; } = null!;
    public int Order { get; private set; }
    public int Sequence { get; private set; }
    public Guid RouteId { get; private set; }
    public Route Route { get; private set; } = null!;
    public Guid? BookingId { get; private set; }
    public Booking? Booking { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private RosterLeg() { }

    internal RosterLeg(MonthlyRoster roster, int order, int sequence, Route route)
    {
        Roster = roster;
        RosterId = roster.Id;
        Order = order;
        Sequence = sequence;
        Route = route;
        RouteId = route.Id;
    }

    public void AttachBooking(Booking booking)
    {
        if (CompletedAt is not null)
            throw new DomainException("This flight of the roster is already completed.");

        Booking = booking;
        BookingId = booking.Id;
    }

    internal void Complete() => CompletedAt = DateTime.UtcNow;
}