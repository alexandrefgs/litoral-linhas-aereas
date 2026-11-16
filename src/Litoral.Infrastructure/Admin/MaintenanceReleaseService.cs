using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Litoral.Infrastructure.Admin;

public sealed class MaintenanceReleaseService(IServiceScopeFactory scopes, ILogger<MaintenanceReleaseService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await ReleaseDueAsync(stoppingToken);
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
        var now = DateTime.UtcNow;

        var due = await db.Aircraft
            .Where(a => a.Status == AircraftStatus.Maintenance && a.MaintenanceUntil != null && a.MaintenanceUntil <= now)
            .ToListAsync(ct);

        if (due.Count == 0) return;

        foreach (var aircraft in due)
            aircraft.ReturnFromMaintenance();

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Released {Count} aircraft from maintenance.", due.Count);
    }
}