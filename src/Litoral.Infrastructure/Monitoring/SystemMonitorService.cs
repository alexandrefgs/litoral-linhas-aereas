using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Litoral.Application.Admin;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Litoral.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Litoral.Infrastructure.Monitoring;

public sealed class SystemMonitorService(LitoralDbContext db, SystemMonitor monitor, IHostEnvironment environment, IConfiguration configuration)
    : ISystemMonitorService
{
    private static readonly TimeSpan LiveTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StorageCache = TimeSpan.FromMinutes(5);

    public async Task<SystemStatusDto> StatusAsync(CancellationToken ct)
    {
        var database = await DatabaseAsync(ct);
        var numbers = database.Online ? await NumbersAsync(ct) : null;

        return new SystemStatusDto(
            DateTime.UtcNow,
            Api(),
            database,
            monitor.Traffic(),
            monitor.IntegrationStatus(),
            monitor.JobStatus(),
            monitor.Errors(),
            numbers,
            monitor.Storage(StorageCache, Storage));
    }

    private ApiHealthDto Api()
    {
        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
        var parts = version.Split('+', 2);
        var label = parts.Length == 2 ? $"{parts[0]} ({parts[1][..Math.Min(7, parts[1].Length)]})" : version;

        using var process = Process.GetCurrentProcess();
        return new ApiHealthDto(
            label,
            environment.EnvironmentName,
            RuntimeInformation.FrameworkDescription,
            monitor.StartedAt,
            (long)(DateTime.UtcNow - monitor.StartedAt).TotalSeconds,
            process.WorkingSet64 / (1024 * 1024));
    }

    private async Task<DatabaseHealthDto> DatabaseAsync(CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            if (!await db.Database.CanConnectAsync(ct))
                return new DatabaseHealthDto(false, null, null, null, "Can't connect to the database.");

            var latency = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var pending = (await db.Database.GetPendingMigrationsAsync(ct)).Count();

            long? size = null;
            try
            {
                size = await db.Database
                    .SqlQueryRaw<long>("SELECT CAST(SUM(CAST(size AS bigint)) * 8192 AS bigint) AS Value FROM sys.database_files")
                    .SingleAsync(ct);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
            }

            return new DatabaseHealthDto(true, latency, size, pending, null);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            return new DatabaseHealthDto(false, null, null, null, ex.Message);
        }
    }

    private async Task<OperationNumbersDto> NumbersAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var liveSince = now - LiveTimeout;

        return new OperationNumbersDto(
            await db.Bookings.CountAsync(b => b.Status == BookingStatus.InProgress, ct),
            await db.FlightSessions.CountAsync(s => s.Status == FlightSessionStatus.Active && s.LastReportAt >= liveSince, ct),
            await db.Bookings.CountAsync(b => b.Status == BookingStatus.Booked, ct),
            await db.Pireps.CountAsync(p => p.Status == PirepStatus.Pending, ct),
            await db.Aircraft.CountAsync(a => a.Status == AircraftStatus.Maintenance, ct),
            await db.AircraftMelItems.Where(m => m.ClosedAt == null && m.DueAt <= now).Select(m => m.AircraftId).Distinct().CountAsync(ct),
            await db.Pilots.CountAsync(p => p.IsActive, ct),
            await db.PilotApplications.CountAsync(ct),
            await db.PrivacyRequests.CountAsync(r => r.Status == PrivacyRequestStatus.Open, ct));
    }

    private StorageDto Storage()
    {
        var (uploadsBytes, uploadsFiles) = Measure(LocalPhotoStorage.ResolveRoot(environment, configuration));
        var (downloadsBytes, downloadsFiles) = Measure(LocalDownloadStorage.ResolveRoot(environment, configuration));
        return new StorageDto(uploadsBytes, uploadsFiles, downloadsBytes, downloadsFiles);
    }

    private static (long Bytes, int Files) Measure(string root)
    {
        try
        {
            var files = new DirectoryInfo(root).EnumerateFiles("*", SearchOption.AllDirectories).ToList();
            return (files.Sum(f => f.Length), files.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (0, 0);
        }
    }
}