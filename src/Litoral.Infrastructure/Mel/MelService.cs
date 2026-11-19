using Litoral.Application.Mel;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Mel;

public sealed class MelService(LitoralDbContext db) : IMelService
{
    private const int MaxAircraft = 100;

    public async Task<IReadOnlyList<AircraftMelDto>> ForAircraftAsync(IReadOnlyCollection<Guid> aircraftIds, CancellationToken ct)
    {
        var ids = aircraftIds.Distinct().Take(MaxAircraft).ToList();
        if (ids.Count == 0) return [];

        var items = await db.AircraftMelItems.AsNoTracking()
            .Include(a => a.MelItem)
            .Where(a => ids.Contains(a.AircraftId) && a.ClosedAt == null)
            .OrderBy(a => a.DueAt)
            .ToListAsync(ct);

        return items
            .GroupBy(a => a.AircraftId)
            .Select(g => new AircraftMelDto(g.Key, g.Select(MelSupport.ToPilotDto).ToList()))
            .ToList();
    }
}