using Litoral.Application.Awards;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Awards;

public sealed class AwardService(LitoralDbContext db) : IAwardService
{
    public async Task<IReadOnlyList<PilotAwardDto>> ForPilotAsync(int pilotNumber, CancellationToken ct) =>
        await db.PilotAwards
            .AsNoTracking()
            .Where(a => a.Pilot.PilotNumber == pilotNumber)
            .OrderByDescending(a => a.GrantedAt)
            .Select(a => new PilotAwardDto(
                a.AwardId,
                a.Award.Name,
                a.Award.Description,
                a.Award.Kind.ToString(),
                a.Award.ImageUrl,
                a.Note,
                a.GrantedAt))
            .ToListAsync(ct);
}