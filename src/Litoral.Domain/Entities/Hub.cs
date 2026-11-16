using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class Hub : Entity
{
    public Guid AirportId { get; private set; }
    public Airport Airport { get; private set; } = null!;

    private Hub() { }

    public Hub(Airport airport)
    {
        if (airport.MinimumRankLevel > 1)
            throw new DomainException("Hubs precisam ser acessíveis a todos os pilotos (patente mínima FO).");

        Airport = airport;
        AirportId = airport.Id;
    }
}