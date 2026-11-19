using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Windows;
using Litoral.Acars.Core.Api;
using Litoral.Acars.Core.Flight;

namespace Litoral.Acars.Ui;

public sealed class BriefingViewModel : ObservableObject
{
    public const string VatsimPrefile = "https://my.vatsim.net/pilots/flightplan";
    public const string IvaoPrefile = "https://fpl.ivao.aero/";

    private readonly Func<LitoralApiClient> _api;
    private AcarsBriefing? _briefing;
    private AcarsOfp? _ofp;
    private string _message = "Load your flight to see the briefing.";
    private string _ofpMessage = "";
    private bool _copied;

    public BriefingViewModel(Func<LitoralApiClient> api)
    {
        _api = api;
        RefreshCommand = new AsyncCommand(_ => LoadAsync());
        CopyRemarksCommand = new AsyncCommand(_ => CopyRemarksAsync(), () => HasBriefing);
        OpenVatsimCommand = new AsyncCommand(_ => Open(Ofp?.VatsimPrefileUrl ?? VatsimPrefile));
        OpenIvaoCommand = new AsyncCommand(_ => Open(Ofp?.IvaoPrefileUrl ?? IvaoPrefile));
    }

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand CopyRemarksCommand { get; }
    public AsyncCommand OpenVatsimCommand { get; }
    public AsyncCommand OpenIvaoCommand { get; }

    public bool Loaded { get; private set; }

    public AcarsBriefing? Briefing
    {
        get => _briefing;
        private set
        {
            if (!Set(ref _briefing, value)) return;
            Raise(nameof(HasBriefing));
            Raise(nameof(DistanceText));
            Raise(nameof(CaptainText));
            Raise(nameof(FirstOfficerText));
            Raise(nameof(DepartureWeather));
            Raise(nameof(ArrivalWeather));
            Raise(nameof(DepartureWind));
            Raise(nameof(ArrivalWind));
        }
    }

    public AcarsOfp? Ofp
    {
        get => _ofp;
        private set
        {
            if (!Set(ref _ofp, value)) return;
            Raise(nameof(HasOfp));
            Raise(nameof(CruiseText));
            Raise(nameof(EnrouteText));
            Raise(nameof(BlockFuelText));
            Raise(nameof(TripFuelText));
            Raise(nameof(ZfwText));
            Raise(nameof(TowText));
            Raise(nameof(LwText));
            Raise(nameof(RunwaysText));
        }
    }

    public bool HasBriefing => Briefing is not null;
    public bool HasOfp => Ofp is { MatchesBooking: true };
    public string Message { get => _message; private set => Set(ref _message, value); }
    public string OfpMessage { get => _ofpMessage; private set => Set(ref _ofpMessage, value); }
    public bool Copied { get => _copied; private set => Set(ref _copied, value); }

    public string DistanceText => Briefing is { } b ? $"{b.Booking.DistanceNm} NM · {BriefingFormat.Duration(b.Booking.EstimatedMinutes)}" : "";
    public string CaptainText => Crew(Briefing?.Captain);
    public string FirstOfficerText => Crew(Briefing?.FirstOfficer);
    public AirportWeather? DepartureWeather => BriefingFormat.WeatherFor(Briefing, Briefing?.Booking.Departure.Icao);
    public AirportWeather? ArrivalWeather => BriefingFormat.WeatherFor(Briefing, Briefing?.Booking.Arrival.Icao);
    public string DepartureWind => BriefingFormat.Wind(DepartureWeather);
    public string ArrivalWind => BriefingFormat.Wind(ArrivalWeather);

    public string CruiseText => BriefingFormat.Cruise(Ofp?.CruiseAltitude);
    public string EnrouteText => BriefingFormat.Duration(Ofp?.EnrouteMinutes);
    public string BlockFuelText => BriefingFormat.Weight(Ofp?.BlockFuel, Ofp?.FuelUnits);
    public string TripFuelText => BriefingFormat.Weight(Ofp?.Loadsheet.TripFuel, Ofp?.FuelUnits);
    public string ZfwText => BriefingFormat.Weight(Ofp?.Loadsheet.ZeroFuelWeight, Ofp?.FuelUnits);
    public string TowText => BriefingFormat.Weight(Ofp?.Loadsheet.TakeoffWeight, Ofp?.FuelUnits);
    public string LwText => BriefingFormat.Weight(Ofp?.Loadsheet.LandingWeight, Ofp?.FuelUnits);
    public string RunwaysText => Ofp is null ? "—" : $"{Ofp.DepartureRunway ?? "—"} / {Ofp.ArrivalRunway ?? "—"}";

    public async Task LoadAsync()
    {
        Loaded = true;
        try
        {
            var api = _api();
            Message = "Loading the briefing…";
            Briefing = await api.GetBriefingAsync();
            Message = "";
        }
        catch (LitoralApiException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            Briefing = null;
            Ofp = null;
            Message = "You don't have an open flight. Book a flight on the website first.";
            return;
        }
        catch (Exception ex) when (ex is LitoralApiException or HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            Briefing = null;
            Ofp = null;
            Message = ex.Message;
            return;
        }

        try
        {
            OfpMessage = "Loading the SimBrief OFP…";
            Ofp = await _api().GetOfpAsync();
            OfpMessage = HasOfp ? "" : $"Your latest SimBrief OFP is for {Ofp.Origin} → {Ofp.Destination}. Generate one for this flight in the Briefing on the website.";
        }
        catch (Exception ex) when (ex is LitoralApiException or HttpRequestException or TaskCanceledException)
        {
            Ofp = null;
            OfpMessage = ex.Message;
        }
    }

    private async Task CopyRemarksAsync()
    {
        if (Briefing is null) return;

        try
        {
            Clipboard.SetText(Briefing.FlightPlanRemarks);
            Copied = true;
            await Task.Delay(2000);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException)
        {
        }
        finally
        {
            Copied = false;
        }
    }

    private static Task Open(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        return Task.CompletedTask;
    }

    private static string Crew(CrewMember? member) =>
        member is null ? "—" : member.DisplayId is null ? member.Name : $"{member.DisplayId} · {member.Name}";
}