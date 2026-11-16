using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class Rank : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int Level { get; private set; }
    public int MinFlightHours { get; private set; }
    public int MinPoints { get; private set; }

    private Rank() { }

    public Rank(string code, string name, int level, int minFlightHours, int minPoints)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Rank code is required.");

        Code = code.ToUpperInvariant();
        Name = name;
        Level = level;
        SetRequirements(minFlightHours, minPoints);
    }

    public void SetRequirements(int minFlightHours, int minPoints)
    {
        if (minFlightHours < 0 || minPoints < 0)
            throw new DomainException("Rank requirements cannot be negative.");

        MinFlightHours = minFlightHours;
        MinPoints = minPoints;
    }

    public bool IsMetBy(int flightMinutes, int points) =>
        flightMinutes >= MinFlightHours * 60 && points >= MinPoints;
}