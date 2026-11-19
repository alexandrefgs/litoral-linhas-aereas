using Litoral.Application.Admin;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Admin;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Litoral.Infrastructure.Monitoring;

namespace Litoral.Infrastructure.Pilots;

public sealed class InactivityService(IServiceScopeFactory scopes, SystemMonitor monitor, ILogger<InactivityService> logger) : BackgroundService
{
    public const int WarningDay = 30;
    public const int PenaltyStartDay = 41;
    public const int DeactivationDay = 90;
    public const int PenaltyPerDay = 50;

    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await monitor.TrackJobAsync("Pilot inactivity", Interval, () => CheckAsync(stoppingToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to check pilot inactivity.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LitoralDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<Notifier>();
        var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLog>();
        var staff = await scope.ServiceProvider.GetRequiredService<StaffRoles>().ByUserAsync(ct);

        var now = DateTime.UtcNow;
        var warnBefore = now.AddDays(-WarningDay);

        var lastFlights = await db.Pireps.AsNoTracking()
            .GroupBy(p => p.PilotId)
            .Select(g => new { PilotId = g.Key, Last = g.Max(p => p.SubmittedAt) })
            .ToDictionaryAsync(g => g.PilotId, g => g.Last, ct);

        var reactivations = await db.AuditLogs.AsNoTracking()
            .Where(l => l.Action == "Pilot reactivated" && l.TargetId != null)
            .GroupBy(l => l.TargetId!)
            .Select(g => new { PilotId = g.Key, Last = g.Max(l => l.At) })
            .ToDictionaryAsync(g => g.PilotId, g => g.Last, ct);

        var flying = await db.Bookings.AsNoTracking()
            .Where(b => b.Status == BookingStatus.InProgress)
            .Select(b => b.PilotId)
            .ToListAsync(ct);

        var candidates = await db.Pilots.Include(p => p.Rank)
            .Where(p => p.IsActive && p.CreatedAt <= warnBefore && !flying.Contains(p.Id))
            .ToListAsync(ct);

        var ranks = await db.Ranks.ToListAsync(ct);
        var processed = 0;

        foreach (var pilot in candidates)
        {
            if (staff.ContainsKey(pilot.UserId)) continue;

            var since = new[]
            {
                pilot.CreatedAt,
                lastFlights.GetValueOrDefault(pilot.Id),
                reactivations.GetValueOrDefault(pilot.Id.ToString())
            }.Max();

            var idleDays = (int)(now - since).TotalDays;
            if (idleDays < WarningDay) continue;

            if (await ApplyAsync(db, notifier, auditLog, ranks, pilot, since, idleDays, ct))
                processed++;
        }

        if (processed > 0)
            logger.LogInformation("Applied inactivity rules to {Count} pilots.", processed);
    }

    private static async Task<bool> ApplyAsync(
        LitoralDbContext db, Notifier notifier, IAuditLog auditLog, List<Rank> ranks,
        Pilot pilot, DateTime since, int idleDays, CancellationToken ct)
    {
        var changed = false;
        var logs = new List<AuditEntry>();

        var warned = await db.Notifications.AnyAsync(n =>
            n.PilotId == pilot.Id && n.Type == NotificationType.InactivityWarning && n.CreatedAt >= since, ct);

        if (!warned)
        {
            notifier.Send(pilot.Id, NotificationType.InactivityWarning,
                $"No flights for {idleDays} days",
                $"Fly at least once every {PenaltyStartDay - 1} days. From day {PenaltyStartDay} without flying you lose {PenaltyPerDay} points per day, and on day {DeactivationDay} the account is deactivated.",
                "/book");
            changed = true;
        }

        if (idleDays >= PenaltyStartDay)
        {
            var due = Math.Min(idleDays, DeactivationDay - 1) - PenaltyStartDay + 1;
            var charged = await db.PointTransactions
                .Where(t => t.PilotId == pilot.Id && t.Type == PointTransactionType.Inactivity && t.CreatedAt >= since)
                .SumAsync(t => -t.Amount, ct) / PenaltyPerDay;
            var days = due - charged;

            if (days > 0)
            {
                pilot.AddPoints(-PenaltyPerDay * days, PointTransactionType.Inactivity,
                    days == 1
                        ? $"Inactivity: day {idleDays} without flying"
                        : $"Inactivity: {days} days without flying (up to day {idleDays})");

                if (charged == 0)
                {
                    notifier.Send(pilot.Id, NotificationType.InactivityPenalty,
                        "Inactivity penalty started",
                        $"You haven't flown for {idleDays} days. You are losing {PenaltyPerDay} points per day until your next flight. On day {DeactivationDay} the account is deactivated.",
                        "/points");
                    logs.Add(new AuditEntry(AuditCategory.Pilots, "Inactivity penalty started", "Pilot", pilot.Id.ToString(), AuditContext.PilotLabel(pilot),
                        $"Days without flying: {idleDays}\nPenalty: −{PenaltyPerDay} per day").AsSystem());
                }

                changed = true;
            }
        }

        if (idleDays >= DeactivationDay)
        {
            pilot.Deactivate();
            notifier.Send(pilot.Id, NotificationType.AccountDeactivated,
                "Account deactivated",
                $"Your account was deactivated after {DeactivationDay} days without flying. Contact the staff to have it reactivated.");
            logs.Add(new AuditEntry(AuditCategory.Pilots, "Pilot deactivated", "Pilot", pilot.Id.ToString(), AuditContext.PilotLabel(pilot),
                $"Reason: {idleDays} days without flying").AsSystem());
            changed = true;
        }

        if (!changed) return false;

        var previousLevel = pilot.Rank.Level;
        var airport = await db.Airports.FirstOrDefaultAsync(a => a.Icao == pilot.CurrentLocationIcao, ct);
        if (airport is not null && pilot.UpdateRank(ranks, airport))
            notifier.RankChanged(pilot, previousLevel);

        await db.SaveChangesAsync(ct);

        foreach (var entry in logs)
            await auditLog.WriteAsync(entry, ct);

        return true;
    }
}