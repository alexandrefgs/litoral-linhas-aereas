using Litoral.Application.Admin;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Audit;

public sealed class AuditLogQuery(LitoralDbContext db) : IAuditLogQuery
{
    private const int MaxPageSize = 100;

        public async Task<AuditLogPageDto> ListAsync(string? category, string? search, bool criticalOnly, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, MaxPageSize);

        var query = db.AuditLogs.AsNoTracking();

        if (Enum.TryParse<AuditCategory>(category, true, out var parsed))
            query = query.Where(l => l.Category == parsed);

        if (criticalOnly)
            query = query.Where(AuditSeverity.IsCritical);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l =>
                l.Action.Contains(term)
                || l.ActorName.Contains(term)
                || (l.TargetName != null && l.TargetName.Contains(term))
                || (l.Details != null && l.Details.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.At)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new AuditLogPageDto(
            items.Select(l => new AuditLogDto(l.Id, l.At, l.Category.ToString(), AuditSeverity.Of(l.Action), l.Action, l.ActorName, l.TargetType, l.TargetName, l.Details, l.IpAddress)).ToList(),
            total,
            page,
            pageSize);
    }
}