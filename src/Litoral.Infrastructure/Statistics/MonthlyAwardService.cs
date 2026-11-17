using Litoral.Application.Admin;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Litoral.Infrastructure.Statistics;

public sealed class MonthlyAwardService(IServiceScopeFactory scopes, ILogger<MonthlyAwardService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan ReviewGrace = TimeSpan.FromDays(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await AwardPreviousMonthAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to pay the pilots of the month.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task AwardPreviousMonthAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LitoralDbContext>();
        var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLog>();

        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var start = thisMonth.AddMonths(-1);

        if (await db.MonthlyAwards.AnyAsync(a => a.Year == start.Year && a.Month == start.Month, ct))
            return;

        var pendingLeft = await db.Pireps.AnyAsync(p => p.Status == PirepStatus.Pending && p.SubmittedAt >= start && p.SubmittedAt < thisMonth, ct);
        if (pendingLeft && now < thisMonth + ReviewGrace)
            return;

        var totals = await db.Pireps.AsNoTracking()
            .Where(p => p.Status == PirepStatus.Approved && p.SubmittedAt >= start && p.SubmittedAt < thisMonth && p.Pilot.IsActive)
            .GroupBy(p => p.PilotId)
            .Select(g => new { PilotId = g.Key, Minutes = g.Sum(p => p.BlockMinutes), Flights = g.Count() })
            .ToListAsync(ct);

        if (totals.Count == 0) return;

        var podiums = new[]
        {
            (Category: AwardCategory.Hours, Winners: totals.OrderByDescending(t => t.Minutes).ThenByDescending(t => t.Flights).Select(t => (t.PilotId, Value: t.Minutes))),
            (Category: AwardCategory.Flights, Winners: totals.OrderByDescending(t => t.Flights).ThenByDescending(t => t.Minutes).Select(t => (t.PilotId, Value: t.Flights)))
        };

        var ids = totals.Select(t => t.PilotId).ToList();
        var pilots = await db.Pilots.Include(p => p.Rank).Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var ranks = await db.Ranks.ToListAsync(ct);
        var label = $"{start.Month:D2}/{start.Year}";

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        foreach (var (category, winners) in podiums)
        {
            var position = 0;
            foreach (var (pilotId, value) in winners.Take(MonthlyAward.BonusByPosition.Length))
            {
                position++;
                var pilot = pilots[pilotId];
                var award = new MonthlyAward(start.Year, start.Month, category, position, pilot, value);
                db.MonthlyAwards.Add(award);

                var valueText = category == AwardCategory.Hours ? $"{value / 60}h{value % 60:D2}" : $"{value} flights";
                pilot.AddPoints(award.Points, PointTransactionType.PilotOfTheMonth,
                    $"Pilot of the month {label}: {Ordinal(position)} in {category.ToString().ToLowerInvariant()} ({valueText})");

                var airport = await db.Airports.FirstOrDefaultAsync(a => a.Icao == pilot.CurrentLocationIcao, ct);
                if (airport is not null) pilot.UpdateRank(ranks, airport);

                await auditLog.WriteAsync(new AuditEntry(
                    AuditCategory.Awards,
                    "Pilot of the month bonus",
                    "Pilot",
                    pilot.Id.ToString(),
                    AuditContext.PilotLabel(pilot),
                    $"Month: {label}\nPodium: {category}\nPosition: {Ordinal(position)}\nValue: {valueText}\nBonus: +{award.Points}"), ct);
            }
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation("Paid the pilots of the month for {Month}.", label);
    }

    private static string Ordinal(int position) => position switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{position}th"
    };
}