using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class Pilot : Entity
{
    public const int FirstPilotNumber = 1000;

    private readonly List<PointTransaction> _pointTransactions = [];

    public string UserId { get; private set; } = null!;
    public int PilotNumber { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Guid RankId { get; private set; }
    public Rank Rank { get; private set; } = null!;
    public string BaseIcao { get; private set; } = null!;
    public string CurrentLocationIcao { get; private set; } = null!;
    public string? SimBriefUsername { get; private set; }
    public int TotalFlightMinutes { get; private set; }
    public int Points { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public IReadOnlyCollection<PointTransaction> PointTransactions => _pointTransactions;

    public string DisplayId => $"{Rank.Code}{PilotNumber}";
    public string NetworkCallsign => $"{Airline.Icao}9{PilotNumber % 1000:D3}";

    private Pilot() { }

    public Pilot(string userId, int pilotNumber, string firstName, string lastName, Rank rank, string baseIcao)
    {
        if (pilotNumber < FirstPilotNumber)
            throw new DomainException($"Pilot number must start at {FirstPilotNumber}.");

        UserId = userId;
        PilotNumber = pilotNumber;
        FirstName = firstName;
        LastName = lastName;
        Rank = rank;
        RankId = rank.Id;
        BaseIcao = baseIcao.ToUpperInvariant();
        CurrentLocationIcao = BaseIcao;
    }

    public void SetSimBriefUsername(string? username) =>
        SimBriefUsername = string.IsNullOrWhiteSpace(username) ? null : username.Trim();

    public void MoveTo(string icao) => CurrentLocationIcao = icao.ToUpperInvariant();

    public void JumpseatTo(Airport destination)
    {
        if (destination.Icao == CurrentLocationIcao)
            throw new DomainException($"You are already at {destination.Icao}.");
        if (!destination.IsAccessibleTo(Rank))
            throw new DomainException($"Your rank does not allow you to go to {destination.Icao}.");

        MoveTo(destination.Icao);
    }

    public void AddFlightTime(int minutes)
    {
        if (minutes <= 0)
            throw new DomainException("Flight time must be greater than zero.");

        TotalFlightMinutes += minutes;
    }

    public PointTransaction AddPoints(int amount, PointTransactionType type, string description)
    {
        var transaction = new PointTransaction(this, amount, type, description);
        _pointTransactions.Add(transaction);
        Points += amount;
        return transaction;
    }

    public bool UpdateRank(IEnumerable<Rank> ranks, Airport currentAirport)
    {
        if (currentAirport.Icao != CurrentLocationIcao)
            throw new DomainException("The given airport is not the pilot's current location.");

        var target = ranks
            .OrderByDescending(r => r.Level)
            .FirstOrDefault(r => r.IsMetBy(TotalFlightMinutes, Points))
            ?? throw new DomainException("No rank available for the pilot.");

        if (target.Id == RankId)
            return false;

        Rank = target;
        RankId = target.Id;

        if (!currentAirport.IsAccessibleTo(target))
            MoveTo(BaseIcao);

        return true;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}