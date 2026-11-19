using Litoral.Acars.Core.Api;

namespace Litoral.Acars.Core.Flight;

public sealed class EpisodeTracker(string type)
{
    private DateTime? _startedAt;
    private decimal? _peak;

    public string Type { get; } = type;
    public bool IsActive => _startedAt is not null;

    public FlightEventReport? Update(bool active, DateTime at, decimal? value = null)
    {
        if (active)
        {
            _startedAt ??= at;
            if (value is decimal v && (_peak is null || Math.Abs(v) > Math.Abs(_peak.Value))) _peak = v;
            return null;
        }

        return Close(at);
    }

    public FlightEventReport? Close(DateTime at)
    {
        if (_startedAt is not DateTime start) return null;

        var report = new FlightEventReport(Type, start, Math.Max(0, (int)Math.Round((at - start).TotalSeconds)), _peak, null);
        _startedAt = null;
        _peak = null;
        return report;
    }
}