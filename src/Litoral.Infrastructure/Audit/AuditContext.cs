using Litoral.Domain.Entities;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Audit;

public sealed class AuditContext(IHttpContextAccessor accessor)
{
    private static readonly string[] AuditedPaths = ["/api/admin", "/api/pilots/me", "/api/auth/register"];

    private (string? UserId, string Name)? _actor;

    public bool InScope =>
        accessor.HttpContext?.Request.Path.Value is string path
        && AuditedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserId => accessor.HttpContext?.User.FindFirst("sub")?.Value;

    public async Task<(string? UserId, string Name)> ActorAsync(LitoralDbContext db, CancellationToken ct)
    {
        if (_actor is { } cached) return cached;

        if (accessor.HttpContext is null) return (null, "System");

        var userId = UserId;
        if (userId is null) return (null, "Visitor");

        var pilot = await db.Pilots.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.PilotNumber, Rank = p.Rank.Code, p.FirstName, p.LastName })
            .FirstOrDefaultAsync(ct);

        _actor = (userId, pilot is null ? "Unknown user" : $"{pilot.Rank}{pilot.PilotNumber} · {pilot.FirstName} {pilot.LastName}");
        return _actor.Value;
    }

    public static string PilotLabel(Pilot pilot) =>
        $"{(pilot.Rank is null ? pilot.PilotNumber.ToString() : pilot.DisplayId)} · {pilot.FirstName} {pilot.LastName}";
}