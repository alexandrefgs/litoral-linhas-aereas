using Litoral.Acars.Core.Api;
using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Core.Flight;

public sealed class AcarsFlight
{
    public const string ClientName = "Litoral ACARS 0.1";
    public const double DiversionRadiusNm = 5;
    public static readonly IReadOnlyList<string> Networks = ["Offline", "Vatsim", "Ivao"];

    private readonly LitoralApiClient _api;

    private AcarsFlight(LitoralApiClient api, AcarsBooking booking)
    {
        _api = api;
        Booking = booking;
        Recorder = new FlightRecorder(booking.AircraftTypeIcao);
        Log = new FlightLog(booking.AircraftTypeIcao);
    }

    public AcarsBooking Booking { get; }
    public FlightRecorder Recorder { get; }
    public FlightLog Log { get; }
    public FlightSession? Session { get; private set; }
    public PirepResult? Pirep { get; private set; }

    public bool LooksLikeDiversion =>
        Recorder.DistanceFromNm(Booking.ArrivalLatitude, Booking.ArrivalLongitude) is double nm && nm > DiversionRadiusNm;

    private static string NetworkName(string network) => network.ToLowerInvariant() switch
    {
        "vatsim" => "VATSIM",
        "ivao" => "IVAO",
        _ => "Offline"
    };

    public static async Task<AcarsFlight?> LoadAsync(LitoralApiClient api, CancellationToken ct = default) =>
        await api.GetBookingAsync(ct) is { } booking ? new AcarsFlight(api, booking) : null;

    public async Task<FlightSession> StartAsync(string network, string? simulator, CancellationToken ct = default)
    {
        if (!Networks.Contains(network, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Choose Offline, VATSIM or IVAO.", nameof(network));

        Session = await _api.StartFlightAsync(new StartFlightRequest(network, simulator, ClientName), ct);
        Log.Info(DateTime.UtcNow, $"Flight {Booking.Callsign} {Booking.DepartureIcao} → {Booking.ArrivalIcao} started · {Booking.Registration} · {NetworkName(network)}"
            + (string.IsNullOrWhiteSpace(simulator) ? "" : $" · {simulator}"));
        return Session;
    }

    public async Task<FlightSession> ChangeNetworkAsync(string network, CancellationToken ct = default)
    {
        if (Session is null || Pirep is not null) throw new InvalidOperationException("Start the flight first.");
        if (!Networks.Contains(network, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Choose Offline, VATSIM or IVAO.", nameof(network));
        if (string.Equals(Session.Network, network, StringComparison.OrdinalIgnoreCase)) return Session;

        var previous = Session.Network;
        Session = await _api.ChangeNetworkAsync(network, ct);
        Log.Info(DateTime.UtcNow, $"Network changed from {NetworkName(previous)} to {NetworkName(Session.Network)}");
        return Session;
    }

    public void OnSnapshot(SimSnapshot snapshot)
    {
        if (Recorder.Last is { } last && snapshot.At <= last.At) return;

        Recorder.Update(snapshot);
        Log.Observe(snapshot, Recorder);
    }

    public async Task<int> FlushAsync(CancellationToken ct = default)
    {
        if (Session is null || Pirep is not null) return 0;

        var sent = 0;
        while (Recorder.PendingPositions > 0)
        {
            var batch = Recorder.PeekPositions();
            await _api.SendPositionsAsync(batch, ct);
            Recorder.RemovePositions(batch.Count);
            sent += batch.Count;
        }
        return sent;
    }

    public async Task<PirepResult> CompleteAsync(string? arrivalIcao, string? comments, CancellationToken ct = default)
    {
        if (Session is null) throw new InvalidOperationException("Start the flight first.");
        if (Pirep is not null) return Pirep;

        var arrival = string.IsNullOrWhiteSpace(arrivalIcao) ? Booking.ArrivalIcao : arrivalIcao.Trim().ToUpperInvariant();
        var diversion = !string.Equals(arrival, Booking.ArrivalIcao, StringComparison.OrdinalIgnoreCase);
        var request = Recorder.BuildCompletion(arrival, diversion, comments);

        await FlushAsync(ct);
        Pirep = await _api.CompleteFlightAsync(request, ct);
        var s = Pirep.Summary;
        Log.Info(DateTime.UtcNow, $"PIREP filed · {s.BlockMinutes} min · landing {(s.LandingRateFpm is int fpm ? $"{fpm} fpm" : "-")} · {s.ScorePoints} points · {s.Status}");
        return Pirep;
    }
}