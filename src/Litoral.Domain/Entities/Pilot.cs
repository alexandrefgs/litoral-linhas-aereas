using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class Pilot : Entity
{
    public const int FirstPilotNumber = 1000;

    public string UserId { get; private set; } = null!;
    public int PilotNumber { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Guid RankId { get; private set; }
    public Rank Rank { get; private set; } = null!;
    public string CurrentLocationIcao { get; private set; } = null!;
    public string? SimBriefUsername { get; private set; }
    public int TotalFlightMinutes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public string DisplayId => $"{Rank.Code}{PilotNumber}";
    public string NetworkCallsign => $"{Airline.Icao}9{PilotNumber % 1000:D3}";

    private Pilot() { }

    public Pilot(string userId, int pilotNumber, string firstName, string lastName, Rank rank, string currentLocationIcao)
    {
        if (pilotNumber < FirstPilotNumber)
            throw new DomainException($"Número do piloto deve começar em {FirstPilotNumber}.");

        UserId = userId;
        PilotNumber = pilotNumber;
        FirstName = firstName;
        LastName = lastName;
        Rank = rank;
        RankId = rank.Id;
        CurrentLocationIcao = currentLocationIcao.ToUpperInvariant();
    }

    public void SetSimBriefUsername(string? username) =>
        SimBriefUsername = string.IsNullOrWhiteSpace(username) ? null : username.Trim();

    public void MoveTo(string icao) => CurrentLocationIcao = icao.ToUpperInvariant();

    public void AddFlightTime(int minutes)
    {
        if (minutes <= 0)
            throw new DomainException("Tempo de voo deve ser maior que zero.");

        TotalFlightMinutes += minutes;
    }

    public void Promote(Rank rank)
    {
        if (rank.Level <= Rank.Level)
            throw new DomainException("Nova patente deve ser superior à atual.");

        Rank = rank;
        RankId = rank.Id;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}