using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class Rank : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int Level { get; private set; }
    public int MinFlightHours { get; private set; }

    private Rank() { }

    public Rank(string code, string name, int level, int minFlightHours)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Código da patente é obrigatório.");
        if (minFlightHours < 0)
            throw new DomainException("Horas mínimas não podem ser negativas.");

        Code = code.ToUpperInvariant();
        Name = name;
        Level = level;
        MinFlightHours = minFlightHours;
    }
}