using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class MonthlyAward : Entity
{
    public static readonly int[] BonusByPosition = [800, 600, 400];

    public int Year { get; private set; }
    public int Month { get; private set; }
    public AwardCategory Category { get; private set; }
    public int Position { get; private set; }
    public Guid? PilotId { get; private set; }
    public Pilot? Pilot { get; private set; }
    public int PilotNumber { get; private set; }
    public string PilotName { get; private set; } = null!;
    public int Value { get; private set; }
    public int Points { get; private set; }
    public DateTime AwardedAt { get; private set; } = DateTime.UtcNow;

    private MonthlyAward() { }

    public MonthlyAward(int year, int month, AwardCategory category, int position, Pilot pilot, int value)
    {
        if (position < 1 || position > BonusByPosition.Length)
            throw new DomainException("Invalid award position.");

        Year = year;
        Month = month;
        Category = category;
        Position = position;
        Pilot = pilot;
        PilotId = pilot.Id;
        PilotNumber = pilot.PilotNumber;
        PilotName = pilot.PublicName;
        Value = value;
        Points = BonusByPosition[position - 1];
    }
}