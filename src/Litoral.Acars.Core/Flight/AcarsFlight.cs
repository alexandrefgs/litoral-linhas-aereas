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
    }

    public AcarsBooking Booking { get; }
    public FlightRecorder Recorder { get; }
    public FlightSession? Session { get; private set; }
    public PirepResult? Pirep { get; private set; }

    public bool LooksLikeDiversion =>
        Recorder.DistanceFromNm(Booking.ArrivalLatitude, Booking.ArrivalLongitude) is double nm && nm > DiversionRadiusNm;

    public static async Task<AcarsFlight?> LoadAsync(LitoralApiClient api, CancellationToken ct = default) =>
        await api.GetBookingAsync(ct) is { } booking ? new AcarsFlight(api, booking) : null;

    public async Task<FlightSession> StartAsync(string network, string? simulator, CancellationToken ct = default)
    {
        if (!Networks.Contains(network, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Choose Offline, VATSIM or IVAO.", nameof(network));

        Session = await _api.StartFlightAsync(new StartFlightRequest(network, simulator, ClientName), ct);
        return Session;
    }

    public void OnSnapshot(SimSnapshot snapshot) => Recorder.Update(snapshot);

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
        return Pirep;
    }
}