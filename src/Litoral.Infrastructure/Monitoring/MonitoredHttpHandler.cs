using System.Diagnostics;

namespace Litoral.Infrastructure.Monitoring;

public sealed class MonitoredHttpHandler(SystemMonitor monitor, string integration) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var response = await base.SendAsync(request, ct);
            var code = (int)response.StatusCode;
            var failed = code >= 500 || code is 401 or 403 or 429;
            monitor.RecordIntegration(integration, !failed, Elapsed(started), failed ? $"HTTP {code} {response.ReasonPhrase}" : null);
            return response;
        }
        catch (Exception ex)
        {
            var message = ex is OperationCanceledException ? "Timed out or cancelled" : ex.Message;
            monitor.RecordIntegration(integration, false, Elapsed(started), message);
            throw;
        }
    }

    private static int Elapsed(long started) => (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}