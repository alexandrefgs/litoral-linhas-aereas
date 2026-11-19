using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class RegistrationBlock : Entity
{
    public const int BlockDays = 30;

    public string EmailHash { get; private set; } = null!;
    public DateTime BlockedUntil { get; private set; }

    private RegistrationBlock() { }

    public RegistrationBlock(string emailHash)
    {
        EmailHash = emailHash;
        BlockedUntil = DateTime.UtcNow.AddDays(BlockDays);
    }
}