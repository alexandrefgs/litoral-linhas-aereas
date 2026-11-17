using Litoral.Application.Common;
using Litoral.Application.Pilots;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Pilots;

public sealed class PointsStatementService(LitoralDbContext db) : IPointsStatementService
{
    private const int MaxPageSize = 100;

    private static readonly Dictionary<string, PointTransactionType[]> Groups = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Flights"] = [PointTransactionType.FlightScore, PointTransactionType.CrewDuty, PointTransactionType.SpecialSchedule],
        ["Bonuses"] = [PointTransactionType.MonthlyBonus, PointTransactionType.PilotOfTheMonth],
        ["Spending"] = [PointTransactionType.BookingCost, PointTransactionType.Jumpseat, PointTransactionType.AircraftTransfer],
        ["Penalties"] = [PointTransactionType.Penalty],
        ["Adjustments"] = [PointTransactionType.ManualAdjustment]
    };

    public async Task<PointsStatementDto> GetAsync(string userId, string? group, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, MaxPageSize);

        var pilot = await db.Pilots.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.Id, p.Points })
            .FirstOrDefaultAsync(ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

        var all = db.PointTransactions.AsNoTracking().Where(t => t.PilotId == pilot.Id);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalEarned = await all.SumAsync(t => t.Amount > 0 ? t.Amount : 0, ct);
        var totalSpent = await all.SumAsync(t => t.Amount < 0 ? -t.Amount : 0, ct);
        var monthEarned = await all.Where(t => t.CreatedAt >= monthStart).SumAsync(t => t.Amount > 0 ? t.Amount : 0, ct);
        var monthSpent = await all.Where(t => t.CreatedAt >= monthStart).SumAsync(t => t.Amount < 0 ? -t.Amount : 0, ct);

        var filtered = all;
        var groupTypes = group is not null && Groups.TryGetValue(group, out var types) ? types : null;
        if (groupTypes is not null)
            filtered = filtered.Where(t => groupTypes.Contains(t.Type));

        var ordered = filtered.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);
        var skip = (page - 1) * pageSize;

        var total = await filtered.CountAsync(ct);
        var items = await ordered.Skip(skip).Take(pageSize).ToListAsync(ct);

        int? balance = null;
        if (groupTypes is null)
        {
            var newer = skip == 0 ? 0 : await ordered.Take(skip).SumAsync(t => t.Amount, ct);
            balance = pilot.Points - newer;
        }

        var rows = new List<PointTransactionDto>(items.Count);
        foreach (var t in items)
        {
            rows.Add(new PointTransactionDto(t.Id, t.CreatedAt, t.Type.ToString(), GroupOf(t.Type), t.Description, t.Amount, balance));
            if (balance is not null) balance -= t.Amount;
        }

        return new PointsStatementDto(
            new PointsSummaryDto(pilot.Points, monthEarned, monthSpent, totalEarned, totalSpent),
            rows,
            total,
            page,
            pageSize);
    }

    private static string GroupOf(PointTransactionType type) =>
        Groups.FirstOrDefault(g => g.Value.Contains(type)).Key ?? "Adjustments";
}