using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class PointTransaction : Entity
{
    public Guid PilotId { get; private set; }
    public Pilot Pilot { get; private set; } = null!;
    public int Amount { get; private set; }
    public PointTransactionType Type { get; private set; }
    public string Description { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private PointTransaction() { }

    internal PointTransaction(Pilot pilot, int amount, PointTransactionType type, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Descrição do lançamento é obrigatória.");

        Pilot = pilot;
        PilotId = pilot.Id;
        Amount = amount;
        Type = type;
        Description = description.Trim();
    }
}