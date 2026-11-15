using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class Hub : Entity
{
    public Guid AirportId { get; private set; }
    public Airport Airport { get; private set; } = null!;
    public OperationType Operations { get; private set; }

    private Hub() { }

    public Hub(Airport airport, OperationType operations)
    {
        if (operations == 0)
            throw new DomainException("Hub precisa ter ao menos um tipo de operação.");

        Airport = airport;
        AirportId = airport.Id;
        Operations = operations;
    }

    public bool Supports(OperationType operation) => Operations.HasFlag(operation);
}