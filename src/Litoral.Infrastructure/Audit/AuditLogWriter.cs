using Litoral.Application.Admin;
using Litoral.Domain.Entities;
using Litoral.Infrastructure.Persistence;

namespace Litoral.Infrastructure.Audit;

public sealed class AuditLogWriter(LitoralDbContext db, AuditContext audit) : IAuditLog
{
    public async Task WriteAsync(AuditEntry entry, CancellationToken ct)
    {
        var (userId, name) = await audit.ActorAsync(db, ct);
        db.AuditLogs.Add(new AuditLog(entry.Category, entry.Action, userId, name, entry.TargetType, entry.TargetId, entry.TargetName, entry.Details, audit.IpAddress));
        await db.SaveChangesAsync(ct);
    }
}