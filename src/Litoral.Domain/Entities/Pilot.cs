using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class Pilot : Entity
{
    public const int FirstPilotNumber = 1000;

    public const int MinimumAge = 16;
    public const string JumpseatUsedMessage = "You already used your jumpseat. Fly at least once to use it again.";
    public const string TransferUsedMessage = "You already moved an aircraft. Fly at least once to move another one.";

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
    public DateTime? LastCrewAssignedAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateOnly? BirthDate { get; private set; }
    public string? CountryCode { get; private set; }
    public string? State { get; private set; }
    public string? City { get; private set; }
    public string? VatsimId { get; private set; }
    public string? IvaoId { get; private set; }
    public string? DiscordId { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? CoverUrl { get; private set; }
    public string? SimAircraftTypes { get; private set; }
    public NameFormat NameFormat { get; private set; } = NameFormat.FirstInitialAndLast;
    public bool CanJumpseat { get; private set; } = true;
    public bool CanTransferAircraft { get; private set; } = true;
    public string? AcceptedTermsVersion { get; private set; }
    public DateTime? AcceptedTermsAt { get; private set; }

    public IReadOnlyCollection<PointTransaction> PointTransactions => _pointTransactions;

    public string DisplayId => $"{Rank.Code}{PilotNumber}";
    public string PublicName => FormatName(NameFormat);
    public IReadOnlyList<string> SimAircraftList =>
        string.IsNullOrWhiteSpace(SimAircraftTypes) ? [] : SimAircraftTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);
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

    public void AcceptTerms(string version)
    {
        AcceptedTermsVersion = version;
        AcceptedTermsAt = DateTime.UtcNow;
    }

    public void SetSimBriefUsername(string? username) =>
        SimBriefUsername = string.IsNullOrWhiteSpace(username) ? null : username.Trim();

        public int? AgeOn(DateOnly date)
    {
        if (BirthDate is not DateOnly birth) return null;
        var age = date.Year - birth.Year;
        if (date < birth.AddYears(age)) age--;
        return age;
    }

    public void UpdatePersonal(string firstName, string lastName, DateOnly? birthDate, string? countryCode, string? state, string? city)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("First and last name are required.");
        if (firstName.Trim().Length > 60 || lastName.Trim().Length > 60)
            throw new DomainException("First and last name must have up to 60 characters.");
        if (Clean(state)?.Length > 60 || Clean(city)?.Length > 80)
            throw new DomainException("State must have up to 60 characters and city up to 80.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (birthDate is DateOnly birth)
        {
            if (birth < today.AddYears(-100))
                throw new DomainException("Enter a valid birth date.");
            if (birth > today.AddYears(-MinimumAge))
                throw new DomainException($"You must be at least {MinimumAge} years old.");
        }

        var country = Clean(countryCode)?.ToUpperInvariant();
        if (country is not null && (country.Length != 2 || !country.All(char.IsAsciiLetter)))
            throw new DomainException("Use the 2-letter country code (e.g. BR).");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        BirthDate = birthDate;
        CountryCode = country;
        State = Clean(state);
        City = Clean(city);
    }

    public string FormatName(NameFormat format) => format switch
    {
        NameFormat.FirstName => FirstName,
        NameFormat.LastName => LastName,
        NameFormat.FullName => $"{FirstName} {LastName}",
        NameFormat.FirstAndLastInitial => $"{FirstName} {LastName[..1]}.",
        _ => $"{FirstName[..1]}. {LastName}"
    };

    public void SetNameFormat(NameFormat format)
    {
        if (!Enum.IsDefined(format))
            throw new DomainException("Invalid name format.");

        NameFormat = format;
    }

    public void SetNetworkIds(string? vatsimId, string? ivaoId, string? discordId)
    {
        VatsimId = Digits(vatsimId, "VATSIM ID", 4, 10);
        IvaoId = Digits(ivaoId, "IVAO ID", 4, 10);
        DiscordId = Digits(discordId, "Discord ID", 17, 20);
    }

    public void SetSimAircraft(IEnumerable<string> icaoCodes)
    {
        var codes = icaoCodes.Select(c => c.Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct().ToList();
        SimAircraftTypes = codes.Count == 0 ? null : string.Join(',', codes);
    }

    public string? SetPhoto(string? url)
    {
        var previous = PhotoUrl;
        PhotoUrl = url;
        return previous;
    }

    public string? SetCover(string? url)
    {
        var previous = CoverUrl;
        CoverUrl = url;
        return previous;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Digits(string? value, string label, int min, int max)
    {
        var clean = Clean(value);
        if (clean is null) return null;
        if (!clean.All(char.IsAsciiDigit) || clean.Length < min || clean.Length > max)
            throw new DomainException($"The {label} must have {min} to {max} digits.");
        return clean;
    }

    public void MoveTo(string icao) => CurrentLocationIcao = icao.ToUpperInvariant();

        public void JumpseatTo(Airport destination)
    {
        if (destination.Icao == CurrentLocationIcao)
            throw new DomainException($"You are already at {destination.Icao}.");
        if (!destination.IsAccessibleTo(Rank))
            throw new DomainException($"Your rank does not allow you to go to {destination.Icao}.");
        if (!CanJumpseat)
            throw new DomainException(JumpseatUsedMessage);

        MoveTo(destination.Icao);
        CanJumpseat = false;
    }

    public void UseAircraftTransfer()
    {
        if (!CanTransferAircraft)
            throw new DomainException(TransferUsedMessage);

        CanTransferAircraft = false;
    }

    public void RecordFlight()
    {
        CanJumpseat = true;
        CanTransferAircraft = true;
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

    public PointTransaction Spend(int amount, PointTransactionType type, string description)
    {
        if (amount <= 0)
            throw new DomainException("The amount must be greater than zero.");
        if (Points < amount)
            throw new DomainException($"You need {amount} points and you have {Points}.");

        return AddPoints(-amount, type, description);
    }

    internal void MarkCrewAssigned() => LastCrewAssignedAt = DateTime.UtcNow;

    public bool UpdateRank(IEnumerable<Rank> ranks, Airport currentAirport)
    {
        if (currentAirport.Icao != CurrentLocationIcao)
            throw new DomainException("The given airport is not the pilot's current location.");

        var ordered = ranks.OrderByDescending(r => r.Level).ToList();
        if (ordered.Count == 0)
            throw new DomainException("No ranks are configured.");

        var target = ordered.FirstOrDefault(r => r.IsMetBy(TotalFlightMinutes, Points)) ?? ordered[^1];

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