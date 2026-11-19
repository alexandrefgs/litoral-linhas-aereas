using System.Net.Http;
using System.Windows.Threading;
using Litoral.Acars.Core.Api;
using Litoral.Acars.Core.Flight;
using Litoral.Acars.Core.Sim;
using Litoral.Acars.Settings;
using Litoral.Acars.Sim;

namespace Litoral.Acars.Ui;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan ReadInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SendInterval = TimeSpan.FromSeconds(30);

    private readonly FsuipcSimSource _sim = new();
    private readonly DispatcherTimer _readTimer = new() { Interval = ReadInterval };
    private readonly DispatcherTimer _sendTimer = new() { Interval = SendInterval };
    private DateTime _lastConnectAttempt = DateTime.MinValue;
    private bool _sending;
    private string? _key;
    private LitoralApiClient? _api;
    private AcarsFlight? _flight;

    private string _serverUrl;
    private bool _hasKey;
    private string _status = "";
    private bool _statusIsError;
    private string _simStatus = "Simulator: looking for FSUIPC…";
    private string _bookingText = "No flight loaded.";
    private string _selectedNetwork = "Vatsim";
    private string _phase = "-";
    private string _ias = "-";
    private string _altitude = "-";
    private string _groundSpeed = "-";
    private string _verticalSpeed = "-";
    private string _flaps = "-";
    private string _blockOff = "-";
    private string _takeoff = "-";
    private string _landing = "-";
    private string _blockOn = "-";
    private string _landingRate = "-";
    private string _sendStatus = "";
    private string _arrivalIcao = "";
    private string _comments = "";
    private bool _diversionWarning;
    private string _pirepText = "";

    public MainViewModel()
    {
        var settings = SettingsStore.Load();
        _serverUrl = settings.ServerUrl;
        _key = settings.Key;
        _hasKey = !string.IsNullOrWhiteSpace(_key);

        SaveSettingsCommand = new AsyncCommand(p => Guard(() => SaveSettings(p as string)));
        LoadBookingCommand = new AsyncCommand(_ => Guard(LoadBookingAsync), () => HasKey && !FlightStarted);
        StartFlightCommand = new AsyncCommand(_ => Guard(StartFlightAsync), () => CanStart);
        FilePirepCommand = new AsyncCommand(_ => Guard(FilePirepAsync), () => CanFile);
        DemoFlightCommand = new AsyncCommand(_ => Guard(DemoFlightAsync), () => IsLocalServer && _flight is not null && !FlightStarted);

        _readTimer.Tick += (_, _) => ReadSimulator();
        _sendTimer.Tick += async (_, _) => await SendPositionsAsync();
        _readTimer.Start();
        _sendTimer.Start();
    }

    public AsyncCommand SaveSettingsCommand { get; }
    public AsyncCommand LoadBookingCommand { get; }
    public AsyncCommand StartFlightCommand { get; }
    public AsyncCommand FilePirepCommand { get; }
    public AsyncCommand DemoFlightCommand { get; }

    public IReadOnlyList<string> Networks => AcarsFlight.Networks;

    public string ServerUrl { get => _serverUrl; set => Set(ref _serverUrl, value); }
    public bool HasKey { get => _hasKey; private set => Set(ref _hasKey, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public bool StatusIsError { get => _statusIsError; private set => Set(ref _statusIsError, value); }
    public string SimStatus { get => _simStatus; private set => Set(ref _simStatus, value); }
    public string BookingText { get => _bookingText; private set => Set(ref _bookingText, value); }
    public string SelectedNetwork { get => _selectedNetwork; set => Set(ref _selectedNetwork, value); }
    public string Phase { get => _phase; private set => Set(ref _phase, value); }
    public string Ias { get => _ias; private set => Set(ref _ias, value); }
    public string Altitude { get => _altitude; private set => Set(ref _altitude, value); }
    public string GroundSpeed { get => _groundSpeed; private set => Set(ref _groundSpeed, value); }
    public string VerticalSpeed { get => _verticalSpeed; private set => Set(ref _verticalSpeed, value); }
    public string Flaps { get => _flaps; private set => Set(ref _flaps, value); }
    public string BlockOff { get => _blockOff; private set => Set(ref _blockOff, value); }
    public string Takeoff { get => _takeoff; private set => Set(ref _takeoff, value); }
    public string Landing { get => _landing; private set => Set(ref _landing, value); }
    public string BlockOn { get => _blockOn; private set => Set(ref _blockOn, value); }
    public string LandingRate { get => _landingRate; private set => Set(ref _landingRate, value); }
    public string SendStatus { get => _sendStatus; private set => Set(ref _sendStatus, value); }
    public string ArrivalIcao { get => _arrivalIcao; set => Set(ref _arrivalIcao, value); }
    public string Comments { get => _comments; set => Set(ref _comments, value); }
    public bool DiversionWarning { get => _diversionWarning; private set => Set(ref _diversionWarning, value); }
    public string PirepText { get => _pirepText; private set => Set(ref _pirepText, value); }

    public bool FlightStarted => _flight?.Session is not null;
    public bool FlightInProgress => FlightStarted && _flight!.Pirep is null;
    public bool IsLocalServer => ServerAddress.TryParse(ServerUrl, out var uri, out _) && ServerAddress.IsLocal(uri);
    private bool CanStart => _flight is not null && !FlightStarted && _sim.IsConnected && _sim.IsSupported;
    private bool CanFile => FlightInProgress && _flight!.Recorder.CanComplete;

    private Task SaveSettings(string? newKey)
    {
        if (!ServerAddress.TryParse(ServerUrl, out var address, out var error))
        {
            ShowError(error);
            return Task.CompletedTask;
        }

        if (!string.IsNullOrWhiteSpace(newKey)) _key = newKey.Trim();
        if (string.IsNullOrWhiteSpace(_key))
        {
            ShowError("Paste the ACARS key created on your profile page.");
            return Task.CompletedTask;
        }

        ServerUrl = address.ToString();
        SettingsStore.Save(new AcarsSettings(ServerUrl, _key));
        _api = null;
        HasKey = true;
        Raise(nameof(IsLocalServer));
        ShowInfo("Settings saved. The key is stored encrypted for your Windows user.");
        return Task.CompletedTask;
    }

    private async Task LoadBookingAsync()
    {
        var api = Api();
        _flight = await AcarsFlight.LoadAsync(api);
        if (_flight is null)
        {
            BookingText = "No open flight. Book a flight on the website first.";
            ShowInfo("");
            return;
        }

        var b = _flight.Booking;
        BookingText = $"{b.Callsign}   {b.DepartureIcao} → {b.ArrivalIcao}   {b.Registration} ({b.AircraftTypeName})";
        ArrivalIcao = b.ArrivalIcao;
        PirepText = "";

        var active = await api.GetActiveFlightAsync();
        ShowInfo(active is null
            ? "Flight loaded. Get the aircraft ready at the gate, then press Start flight."
            : "This flight was already started. Starting again continues it, but nothing before now was recorded.");
    }

    private async Task StartFlightAsync()
    {
        if (_flight is null) return;

        await _flight.StartAsync(SelectedNetwork, _sim.Simulator);
        Raise(nameof(FlightStarted));
        ShowInfo($"Flight started on {SelectedNetwork}. Positions are sent every {SendInterval.TotalSeconds:0} seconds.");
    }

    private async Task FilePirepAsync()
    {
        if (_flight is null) return;

        var pirep = await _flight.CompleteAsync(ArrivalIcao, Comments);
        var s = pirep.Summary;
        PirepText = $"PIREP filed: {s.Callsign} to {s.ArrivalIcao}, {s.BlockMinutes} min, landing {s.LandingRateFpm} fpm, {s.ScorePoints} points, status {s.Status}"
            + (s.HasRejection ? " (has a rejection item, see the website)." : ".");
        SendStatus = "";
        ShowInfo("Flight complete. You can close the ACARS.");
        Raise(nameof(FlightStarted));
    }

    private async Task DemoFlightAsync()
    {
        if (_flight is null) return;

        await _flight.StartAsync("Offline", "Scripted demo");
        Raise(nameof(FlightStarted));

        var b = _flight.Booking;
        var options = new ScriptedFlightOptions(b.AircraftTypeIcao, b.DepartureLatitude, b.DepartureLongitude, b.ArrivalLatitude, b.ArrivalLongitude);
        foreach (var snapshot in ScriptedFlight.Generate(DateTime.UtcNow.AddMinutes(-40), options))
            _flight.OnSnapshot(snapshot);

        await _flight.FlushAsync();
        ShowFlight(_flight.Recorder.Last);
        ShowInfo("Demo flight recorded. Review it and press File PIREP.");
    }

    private void ReadSimulator()
    {
        if (!_sim.IsConnected)
        {
            if (DateTime.UtcNow - _lastConnectAttempt < ReconnectInterval) return;
            _lastConnectAttempt = DateTime.UtcNow;
            if (!_sim.TryConnect())
            {
                SimStatus = "Simulator: not found. Open MSFS and FSUIPC7.";
                return;
            }
        }

        var snapshot = _sim.Read(DateTime.UtcNow);
        if (snapshot is null)
        {
            SimStatus = "Simulator: connection lost. Trying again…";
            return;
        }

        SimStatus = _sim.IsSupported
            ? $"Simulator: {_sim.Simulator} — {_sim.AircraftTitle}"
            : $"Simulator: {_sim.Simulator} is not supported yet. Use MSFS 2020 or 2024.";

        if (FlightInProgress) _flight!.OnSnapshot(snapshot);
        ShowFlight(snapshot);
    }

    private void ShowFlight(SimSnapshot? s)
    {
        if (s is null) return;

        Ias = $"{s.IasKt} kt";
        Altitude = $"{s.AltitudeFt:N0} ft";
        GroundSpeed = $"{s.GroundSpeedKt} kt";
        VerticalSpeed = $"{s.VerticalSpeedFpm:+#;-#;0} fpm";

        if (_flight is not { } flight) return;

        var r = flight.Recorder;
        Flaps = FlapTable.ToDegrees(flight.Booking.AircraftTypeIcao, s.FlapsHandle, s.FlapDetents)?.ToString() ?? "?";
        Phase = FlightInProgress ? r.Phase.ToString() : "-";
        BlockOff = Time(r.BlockOffAt);
        Takeoff = Time(r.TakeoffAt);
        Landing = Time(r.LandingAt);
        BlockOn = Time(r.BlockOnAt);
        LandingRate = r.LandingRateFpm is int rate ? $"{rate} fpm" : "-";
        DiversionWarning = r.CanComplete && flight.LooksLikeDiversion;
    }

    private async Task SendPositionsAsync()
    {
        if (_sending || !FlightInProgress) return;

        _sending = true;
        try
        {
            var sent = await _flight!.FlushAsync();
            SendStatus = $"Last send {DateTime.Now:HH:mm:ss}: {sent} positions.";
        }
        catch (Exception ex) when (ex is LitoralApiException or HttpRequestException or TaskCanceledException)
        {
            SendStatus = $"Send failed at {DateTime.Now:HH:mm:ss} ({ex.Message}). {_flight!.Recorder.PendingPositions} positions kept, trying again.";
        }
        finally
        {
            _sending = false;
        }
    }

    private LitoralApiClient Api()
    {
        if (_api is not null) return _api;
        if (!ServerAddress.TryParse(ServerUrl, out var address, out var error)) throw new InvalidOperationException(error);
        if (string.IsNullOrWhiteSpace(_key)) throw new InvalidOperationException("Save your ACARS key first.");

        _api = new LitoralApiClient(LitoralApiClient.CreateHttp(address, _key));
        return _api;
    }

    private async Task Guard(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is LitoralApiException or HttpRequestException or TaskCanceledException or InvalidOperationException or ArgumentException or System.IO.IOException)
        {
            ShowError(ex is HttpRequestException ? $"Could not reach the server: {ex.Message}" : ex.Message);
        }
    }

    private void ShowInfo(string message)
    {
        StatusIsError = false;
        Status = message;
    }

    private void ShowError(string message)
    {
        StatusIsError = true;
        Status = message;
    }

    private static string Time(DateTime? utc) => utc is DateTime t ? $"{t:HH:mm:ss}Z" : "-";

    public void Dispose()
    {
        _readTimer.Stop();
        _sendTimer.Stop();
        _sim.Dispose();
    }
}