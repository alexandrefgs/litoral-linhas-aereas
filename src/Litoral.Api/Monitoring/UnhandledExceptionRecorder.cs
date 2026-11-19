using System.Diagnostics;
using Litoral.Infrastructure.Monitoring;
using Microsoft.AspNetCore.Diagnostics;

namespace Litoral.Api.Monitoring;

public sealed class UnhandledExceptionRecorder(SystemMonitor monitor) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        monitor.RecordError(context.Request.Method, context.Request.Path, StatusCodes.Status500InternalServerError, exception, traceId);
        return ValueTask.FromResult(false);
    }
}