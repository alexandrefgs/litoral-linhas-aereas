using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Litoral.Infrastructure.Operations;
using Litoral.Infrastructure.Monitoring;

namespace Litoral.Infrastructure.Admin;

public sealed class MaintenanceReleaseService(IServiceScopeFactory scopes, SystemMonitor monitor, ILogger<MaintenanceReleaseService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await monitor.TrackJobAsync("Maintenance release", Interval, () => ReleaseDueAsync(stoppingToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to release aircraft from maintenance.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReleaseDueAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LitoralDbContext>();
        var maintenance = scope.ServiceProvider.GetRequiredService<MaintenanceFerryPlanner>();
        var now = DateTime.UtcNow;

        var due = await db.Aircraft
            .Include(a => a.AircraftType)
            .Where(a => a.Status == AircraftStatus.Maintenance && a.MaintenanceUntil != null && a.MaintenanceUntil <= now)
            .ToListAsync(ct);

        if (due.Count == 0) return;

        foreach (var aircraft in due)
        {
            var completed = aircraft.ReturnFromMaintenance();
            await maintenance.AfterMaintenanceAsync(aircraft, completed, ct);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Released {Count} aircraft from maintenance.", due.Count);
    }
}