namespace Litoral.Application.Admin;

public sealed record SystemStatusDto(
    DateTime GeneratedAt,
    ApiHealthDto Api,
    DatabaseHealthDto Database,
    TrafficDto Traffic,
    IReadOnlyList<IntegrationStatusDto> Integrations,
    IReadOnlyList<JobStatusDto> Jobs,
    IReadOnlyList<RecentErrorDto> Errors,
    OperationNumbersDto? Numbers,
    StorageDto Storage);

public sealed record ApiHealthDto(
    string Version,
    string Environment,
    string Runtime,
    DateTime StartedAt,
    long UptimeSeconds,
    long MemoryMb);

public sealed record DatabaseHealthDto(
    bool Online,
    int? LatencyMs,
    long? SizeBytes,
    int? PendingMigrations,
    string? Error);

public sealed record TrafficPointDto(DateTime Minute, int Requests, int Errors);

public sealed record TrafficDto(
    long RequestsLastHour,
    long ServerErrorsLastHour,
    int? AverageMsLastHour,
    long RequestsSinceStart,
    long ServerErrorsSinceStart,
    IReadOnlyList<TrafficPointDto> Minutes);

public sealed record IntegrationStatusDto(
    string Name,
    string Description,
    string State,
    DateTime? LastSuccessAt,
    DateTime? LastErrorAt,
    string? LastError,
    int? LastLatencyMs,
    long Calls,
    long Failures);

public sealed record JobStatusDto(
    string Name,
    int IntervalMinutes,
    string State,
    DateTime? LastRunAt,
    DateTime? LastSuccessAt,
    int? LastDurationMs,
    DateTime? LastErrorAt,
    string? LastError,
    DateTime? NextRunAt,
    long Runs,
    long Failures);

public sealed record RecentErrorDto(
    DateTime At,
    string Method,
    string Path,
    int Status,
    string Type,
    string Message,
    string TraceId);

public sealed record OperationNumbersDto(
    int FlightsInProgress,
    int LiveSessions,
    int OpenBookings,
    int PendingPireps,
    int AircraftInMaintenance,
    int AircraftAog,
    int ActivePilots,
    int PendingApplications,
    int OpenPrivacyRequests);

public sealed record StorageDto(long UploadsBytes, int UploadsFiles, long DownloadsBytes, int DownloadsFiles);

public interface ISystemMonitorService
{
    Task<SystemStatusDto> StatusAsync(CancellationToken ct);
}