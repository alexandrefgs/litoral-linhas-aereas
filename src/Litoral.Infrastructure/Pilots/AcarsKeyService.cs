using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Pilots;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Pilots;

public sealed class AcarsKeyService(LitoralDbContext db, IAuditLog auditLog) : IAcarsKeyService
{
    public async Task<IReadOnlyList<AcarsKeyDto>> ListAsync(string userId, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);
        return await db.AcarsKeys.AsNoTracking()
            .Where(k => k.PilotId == pilot.Id && k.RevokedAt == null)
            .OrderBy(k => k.CreatedAt)
            .Select(k => new AcarsKeyDto(k.Id, k.Name, k.Prefix, k.CreatedAt, k.LastUsedAt))
            .ToListAsync(ct);
    }

    public async Task<CreatedAcarsKeyDto> CreateAsync(string userId, CreateAcarsKeyRequest request, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);

        if (await db.AcarsKeys.CountAsync(k => k.PilotId == pilot.Id && k.RevokedAt == null, ct) >= AcarsKey.MaxPerPilot)
            throw new AppException(AppErrorKind.Conflict, $"You can have up to {AcarsKey.MaxPerPilot} ACARS keys. Revoke one before creating another.");

        var (key, secret) = AcarsKey.Create(pilot, request.Name);
        db.AcarsKeys.Add(key);
        await db.SaveChangesAsync(ct);
        await auditLog.WriteAsync(Entry("ACARS key created", pilot, key), ct);

        return new CreatedAcarsKeyDto(ToDto(key), secret);
    }

    public async Task RevokeAsync(string userId, Guid id, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);
        var key = await db.AcarsKeys.FirstOrDefaultAsync(k => k.Id == id && k.PilotId == pilot.Id && k.RevokedAt == null, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "ACARS key not found.");

        key.Revoke(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        await auditLog.WriteAsync(Entry("ACARS key revoked", pilot, key), ct);
    }

    public static Task<int> RevokeAllAsync(LitoralDbContext db, Guid pilotId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return db.AcarsKeys
            .Where(k => k.PilotId == pilotId && k.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.RevokedAt, now), ct);
    }

    private async Task<Pilot> FindPilotAsync(string userId, CancellationToken ct) =>
        await db.Pilots.FirstOrDefaultAsync(p => p.UserId == userId, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

    private static AcarsKeyDto ToDto(AcarsKey k) => new(k.Id, k.Name, k.Prefix, k.CreatedAt, k.LastUsedAt);

    private static AuditEntry Entry(string action, Pilot pilot, AcarsKey key) =>
        new(AuditCategory.Account, action, "Pilot", pilot.Id.ToString(), AuditContext.PilotLabel(pilot), $"Key: {key.Name} ({key.Prefix}…)");
}