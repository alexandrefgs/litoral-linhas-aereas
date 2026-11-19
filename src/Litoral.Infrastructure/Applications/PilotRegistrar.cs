using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Applications;

public sealed class PilotRegistrar(LitoralDbContext db)
{
    public const string DefaultHubIcao = "SBGR";

    public Task<bool> IsPassengerHubAsync(string icao, CancellationToken ct) =>
        db.Hubs.AnyAsync(h => h.Airport.Icao == icao && (h.Airport.Operations & OperationType.Passenger) == OperationType.Passenger, ct);

    public async Task<Pilot> CreateAsync(ApplicationUser user, string firstName, string lastName, string hubIcao, string termsVersion, CancellationToken ct)
    {
        var initialRank = await db.Ranks.OrderBy(r => r.Level).FirstAsync(ct);
        var pilotNumber = await NextPilotNumberAsync(ct);

        var pilot = new Pilot(user.Id, pilotNumber, firstName.Trim(), lastName.Trim(), initialRank, hubIcao);
        pilot.AcceptTerms(termsVersion);
        db.Pilots.Add(pilot);
        await db.SaveChangesAsync(ct);

        return pilot;
    }

    public async Task<string?> NetworkConflictAsync(string? vatsimId, string? ivaoId, CancellationToken ct)
    {
        if (vatsimId is not null && await db.Pilots.AnyAsync(p => p.VatsimId == vatsimId, ct))
            return "This VATSIM ID is already used by a Litoral pilot.";
        if (ivaoId is not null && await db.Pilots.AnyAsync(p => p.IvaoId == ivaoId, ct))
            return "This IVAO ID is already used by a Litoral pilot.";
        return null;
    }

    private async Task<int> NextPilotNumberAsync(CancellationToken ct)
    {
        var numbers = await db.Pilots.OrderBy(p => p.PilotNumber).Select(p => p.PilotNumber).ToListAsync(ct);

        var next = Pilot.FirstPilotNumber;
        foreach (var number in numbers)
        {
            if (number != next) break;
            next++;
        }

        return next;
    }
}