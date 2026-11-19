using System.Diagnostics;
using Litoral.Application.Admin;

namespace Litoral.Infrastructure.Monitoring;

public sealed class SystemMonitor
{
    public const string Metar = "METAR";
    public const string SimBrief = "SimBrief";
    public const string AirportCatalog = "OurAirports";

    public static readonly IReadOnlyList<(string Name, string Description)> Integrations =
    [
        (Metar, "Weather for the briefing (METAR and TAF)"),
        (SimBrief, "OFP, loadsheet and dispatch"),
        (AirportCatalog, "Airport catalog import")
    ];

    private const int MaxErrors = 50;
    private const int MaxMessageLength = 300;
    private const int TrafficMinutes = 60;

    private readonly Lock _lock = new();
    private readonly Dictionary<string, JobState> _jobs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IntegrationState> _integrations = new(StringComparer.Ordinal);
    private readonly LinkedList<RecentErrorDto> _errors = new();
    private readonly TrafficBucket[] _traffic = new TrafficBucket[TrafficMinutes];
    private long _requests;
    private long _serverErrors;
    private (DateTime At, StorageDto Value)? _storage;

    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public SystemMonitor()
    {
        foreach (var (name, _) in Integrations)
            _integrations[name] = new IntegrationState();
    }

    public async Task TrackJobAsync(string name, TimeSpan interval, Func<Task> run)
    {
        JobState job;
        lock (_lock)
        {
            if (!_jobs.TryGetValue(name, out job!))
                _jobs[name] = job = new JobState(interval);
            job.Running = true;
            job.LastRunAt = DateTime.UtcNow;
            job.Runs++;
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            await run();
            lock (_lock)
            {
                job.LastSuccessAt = DateTime.UtcNow;
                job.LastDurationMs = Elapsed(started);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            lock (_lock)
            {
                job.LastErrorAt = DateTime.UtcNow;
                job.LastError = Trim($"{ex.GetType().Name}: {ex.Message}");
                job.LastDurationMs = Elapsed(started);
                job.Failures++;
            }
            throw;
        }
        finally
        {
            lock (_lock) job.Running = false;
        }
    }

    public void RecordIntegration(string name, bool success, int latencyMs, string? error)
    {
        lock (_lock)
        {
            if (!_integrations.TryGetValue(name, out var state))
                _integrations[name] = state = new IntegrationState();

            state.Calls++;
            state.LastLatencyMs = latencyMs;
            if (success)
            {
                state.LastSuccessAt = DateTime.UtcNow;
            }
            else
            {
                state.Failures++;
                state.LastErrorAt = DateTime.UtcNow;
                state.LastError = Trim(error ?? "Unknown error");
            }
        }
    }

    public void RecordRequest(int status, double elapsedMs)
    {
        var minute = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute;
        lock (_lock)
        {
            _requests++;
            if (status >= 500) _serverErrors++;

            ref var bucket = ref _traffic[minute % TrafficMinutes];
            if (bucket.Minute != minute) bucket = new TrafficBucket { Minute = minute };
            bucket.Requests++;
            bucket.TotalMs += elapsedMs;
            if (status >= 500) bucket.Errors++;
        }
    }

    public void RecordError(string method, string path, int status, Exception exception, string traceId)
    {
        var error = new RecentErrorDto(DateTime.UtcNow, method, path, status, exception.GetType().Name, Trim(exception.Message), traceId);
        lock (_lock)
        {
            _errors.AddFirst(error);
            while (_errors.Count > MaxErrors) _errors.RemoveLast();
        }
    }

    public StorageDto Storage(TimeSpan maxAge, Func<StorageDto> compute)
    {
        lock (_lock)
        {
            if (_storage is { } cached && DateTime.UtcNow - cached.At < maxAge) return cached.Value;
        }

        var value = compute();
        lock (_lock) _storage = (DateTime.UtcNow, value);
        return value;
    }

    public TrafficDto Traffic()
    {
        var now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute;
        lock (_lock)
        {
            var points = new List<TrafficPointDto>(TrafficMinutes);
            long requests = 0, errors = 0;
            double totalMs = 0;

            for (var minute = now - TrafficMinutes + 1; minute <= now; minute++)
            {
                var bucket = _traffic[minute % TrafficMinutes];
                var current = bucket.Minute == minute ? bucket : default;
                points.Add(new TrafficPointDto(new DateTime(minute * TimeSpan.TicksPerMinute, DateTimeKind.Utc), current.Requests, current.Errors));
                requests += current.Requests;
                errors += current.Errors;
                totalMs += current.TotalMs;
            }

            return new TrafficDto(requests, errors, requests > 0 ? (int)Math.Round(totalMs / requests) : null, _requests, _serverErrors, points);
        }
    }

    public IReadOnlyList<RecentErrorDto> Errors()
    {
        lock (_lock) return _errors.ToList();
    }

    public IReadOnlyList<IntegrationStatusDto> IntegrationStatus()
    {
        lock (_lock)
        {
            return _integrations.Select(pair =>
            {
                var s = pair.Value;
                var description = Integrations.FirstOrDefault(i => i.Name == pair.Key).Description ?? "";
                var state = s.Calls == 0 ? "Idle"
                    : s.LastErrorAt is { } error && (s.LastSuccessAt is null || error > s.LastSuccessAt) ? "Failing"
                    : "Ok";
                return new IntegrationStatusDto(pair.Key, description, state, s.LastSuccessAt, s.LastErrorAt, s.LastError, s.LastLatencyMs, s.Calls, s.Failures);
            }).ToList();
        }
    }

    public IReadOnlyList<JobStatusDto> JobStatus()
    {
        var now = DateTime.UtcNow;
        lock (_lock)
        {
            return _jobs.OrderBy(j => j.Key).Select(pair =>
            {
                var j = pair.Value;
                var lastFinished = new[] { j.LastSuccessAt, j.LastErrorAt }.Max();
                var state = j.Running ? "Running"
                    : j.LastRunAt is null ? "Waiting"
                    : j.LastErrorAt is { } error && (j.LastSuccessAt is null || error > j.LastSuccessAt) ? "Failing"
                    : lastFinished is { } done && now - done > j.Interval * 2 + TimeSpan.FromMinutes(2) ? "Late"
                    : "Ok";
                return new JobStatusDto(pair.Key, (int)j.Interval.TotalMinutes, state, j.LastRunAt, j.LastSuccessAt, j.LastDurationMs,
                    j.LastErrorAt, j.LastError, j.LastRunAt + j.Interval, j.Runs, j.Failures);
            }).ToList();
        }
    }

    private static int Elapsed(long started) => (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;

    private static string Trim(string text) => text.Length > MaxMessageLength ? text[..MaxMessageLength] + "…" : text;

    private sealed class JobState(TimeSpan interval)
    {
        public TimeSpan Interval { get; } = interval;
        public bool Running { get; set; }
        public DateTime? LastRunAt { get; set; }
        public DateTime? LastSuccessAt { get; set; }
        public DateTime? LastErrorAt { get; set; }
        public string? LastError { get; set; }
        public int? LastDurationMs { get; set; }
        public long Runs { get; set; }
        public long Failures { get; set; }
    }

    private sealed class IntegrationState
    {
        public DateTime? LastSuccessAt { get; set; }
        public DateTime? LastErrorAt { get; set; }
        public string? LastError { get; set; }
        public int? LastLatencyMs { get; set; }
        public long Calls { get; set; }
        public long Failures { get; set; }
    }

    private struct TrafficBucket
    {
        public long Minute;
        public int Requests;
        public int Errors;
        public double TotalMs;
    }
}