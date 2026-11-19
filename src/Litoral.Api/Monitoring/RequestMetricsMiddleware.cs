using System.Diagnostics;
using Litoral.Infrastructure.Monitoring;

namespace Litoral.Api.Monitoring;

public sealed class RequestMetricsMiddleware(RequestDelegate next, SystemMonitor monitor)
{
    private const string MonitorPath = "/api/admin/system";

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api") || path.StartsWithSegments(MonitorPath))
        {
            await next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            monitor.RecordRequest(context.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}