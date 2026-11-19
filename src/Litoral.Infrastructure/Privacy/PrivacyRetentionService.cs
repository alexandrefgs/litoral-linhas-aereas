using Litoral.Application.Admin;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Litoral.Infrastructure.Privacy;

public sealed class PrivacyRetentionService(
    IServiceScopeFactory scopes,
    IOptions<PrivacyOptions> options,
    ILogger<PrivacyRetentionService> logger) : BackgroundService
{
    public const string AnonymizedName = "Anonymized";

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await ApplyRetentionAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to apply the data retention rules.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ApplyRetentionAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LitoralDbContext>();
        var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLog>();

        var days = Math.Max(options.Value.AuditLogRetentionDays, 30);
        var cutoff = DateTime.UtcNow.AddDays(-days);
        const string system = AuditEntry.SystemActor;
        const string visitor = "Visitor";

        var logs = await db.AuditLogs
            .Where(l => l.At < cutoff
                && (l.ActorUserId != null
                    || l.IpAddress != null
                    || (l.TargetType == "Pilot" && l.TargetId != null)
                    || (l.ActorName != system && l.ActorName != visitor && l.ActorName != AnonymizedName)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.ActorUserId, (string?)null)
                .SetProperty(l => l.ActorName, l => l.ActorName == system || l.ActorName == visitor ? l.ActorName : AnonymizedName)
                .SetProperty(l => l.IpAddress, (string?)null)
                .SetProperty(l => l.TargetId, l => l.TargetType == "Pilot" ? null : l.TargetId)
                .SetProperty(l => l.TargetName, l => l.TargetType == "Pilot" ? null : l.TargetName)
                .SetProperty(l => l.Details, l => l.TargetType == "Pilot" ? null : l.Details), ct);

        var requests = await db.PrivacyRequests
            .Where(r => r.Status != PrivacyRequestStatus.Open && r.ResolvedAt < cutoff)
            .ExecuteDeleteAsync(ct);

        if (logs == 0 && requests == 0) return;

        logger.LogInformation("Data retention: {Logs} audit logs anonymized and {Requests} privacy requests deleted.", logs, requests);
        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Privacy, "Retention applied", Details:
            $"Older than {days} days\nAudit logs anonymized: {logs}\nPrivacy requests deleted: {requests}").AsSystem(), ct);
    }
}