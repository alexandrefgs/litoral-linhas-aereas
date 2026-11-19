using System.Net.Http;
using System.Windows.Threading;
using Litoral.Acars.Core.Api;
using Litoral.Acars.Core.Map;

namespace Litoral.Acars.Ui;

public sealed class LiveMapViewModel : ObservableObject
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    private readonly Func<LitoralApiClient> _api;
    private readonly Func<string?> _myCallsign;
    private readonly DispatcherTimer _timer = new() { Interval = RefreshInterval };
    private string _summary = "Loading the map…";
    private bool _refreshing;

    public LiveMapViewModel(Func<LitoralApiClient> api, Func<string?> myCallsign)
    {
        _api = api;
        _myCallsign = myCallsign;
        RefreshCommand = new AsyncCommand(_ => RefreshAsync());
        _timer.Tick += async (_, _) => await RefreshAsync();
    }

    public event Action<string>? MessageReady;

    public AsyncCommand RefreshCommand { get; }
    public string? LastFlightsMessage { get; private set; }
    public string Summary { get => _summary; private set => Set(ref _summary, value); }

    public async Task RefreshAsync()
    {
        if (_refreshing) return;
        _refreshing = true;
        _timer.Start();

        try
        {
            var flights = await _api().GetLiveFlightsAsync();
            Summary = flights.Count switch
            {
                0 => $"No Litoral flights in progress · updated {DateTime.Now:HH:mm}",
                1 => $"1 flight in progress · updated {DateTime.Now:HH:mm}",
                var n => $"{n} flights in progress · updated {DateTime.Now:HH:mm}"
            };
            LastFlightsMessage = LiveMapMessages.Flights(flights, _myCallsign());
            MessageReady?.Invoke(LastFlightsMessage);
        }
        catch (Exception ex) when (ex is LitoralApiException or HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            Summary = ex.Message;
        }
        finally
        {
            _refreshing = false;
        }
    }

    public void SetTheme(bool dark) => MessageReady?.Invoke(LiveMapMessages.Theme(dark));

    public void Stop() => _timer.Stop();
}