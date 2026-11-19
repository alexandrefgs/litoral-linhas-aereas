using Litoral.Application.Bookings;
using Litoral.Application.Common;
using Litoral.Application.Flights;
using Litoral.Domain.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Flights;
using Litoral.Infrastructure.Flights;

namespace Litoral.Infrastructure.Bookings;

public sealed class BriefingService(BookingSupport support, IMetarProvider metars, ISimBriefProvider simBrief, CrewCards crewCards) : IBriefingService
{
    public async Task<BriefingDto> GetAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var booking = await support.FindOpenBookingAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "You don't have an open flight.");

        var route = booking.Route;
        var dispatch = "https://dispatch.simbrief.com/options/custom"
            + $"?airline={Airline.Icao}"
            + $"&fltnum={route.FlightNumber}"
            + $"&callsign={Uri.EscapeDataString(booking.Callsign)}"
            + $"&type={Uri.EscapeDataString(booking.Aircraft.AircraftType.IcaoCode)}"
            + $"&reg={Uri.EscapeDataString(booking.Aircraft.Registration)}"
            + $"&orig={route.DepartureAirport.Icao}"
            + $"&dest={route.ArrivalAirport.Icao}";

        var weather = await metars.GetAsync([route.DepartureAirport.Icao, route.ArrivalAirport.Icao], ct);
        var (captain, firstOfficer) = await crewCards.BuildAsync(pilot, booking.CrewPilot, ct);
        var bases = await support.GetBaseIcaosAsync(ct);

        return new BriefingDto(
            BookingSupport.ToDto(booking),
            pilot.SimBriefUsername,
            dispatch,
            weather,
            captain,
            firstOfficer,
            bases.ToList(),
            simBrief.CanGenerate,
            FlightPlanRemarks.Required(pilot.DisplayId));
    }

    public async Task<IReadOnlyList<MetarDto>> GetWeatherAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var booking = await support.FindOpenBookingAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "You don't have an open flight.");

        return await metars.GetAsync([booking.Route.DepartureAirport.Icao, booking.Route.ArrivalAirport.Icao], ct);
    }

    public async Task<SimBriefGenerateDto> GenerateSimBriefAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(pilot.SimBriefUsername))
            throw new AppException(AppErrorKind.Validation, "Set your SimBrief username in your profile first.");

        var booking = await support.FindOpenBookingAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "You don't have an open flight.");

        var (captain, firstOfficer) = await crewCards.BuildAsync(pilot, booking.CrewPilot, ct);
        var route = booking.Route;

        var url = simBrief.BuildGenerateUrl(new SimBriefRequest(
            StaticId(booking),
            route.DepartureAirport.Icao,
            route.ArrivalAirport.Icao,
            booking.Aircraft.AircraftType.IcaoCode,
            booking.Aircraft.Registration,
            route.FlightNumber.ToString(),
            booking.Callsign,
            CrewName(captain),
            CrewName(firstOfficer),
            FlightPlanRemarks.Required(pilot.DisplayId)));

        return new SimBriefGenerateDto(url);
    }

    public async Task<SimBriefOfpDto> GetSimBriefAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(pilot.SimBriefUsername))
            throw new AppException(AppErrorKind.Validation, "Set your SimBrief username in your profile first.");

        var booking = await support.FindOpenBookingAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "You don't have an open flight.");

        var ofp = await simBrief.GetLatestAsync(pilot.SimBriefUsername, StaticId(booking), booking.Route.DepartureAirport.Icao, booking.Route.ArrivalAirport.Icao, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "No SimBrief flight plan was found for your username.");

        return ofp with
        {
            VatsimPrefileUrl = FlightPlanRemarks.ApplyToPrefileLink(ofp.VatsimPrefileUrl, pilot.DisplayId),
            IvaoPrefileUrl = FlightPlanRemarks.ApplyToPrefileLink(ofp.IvaoPrefileUrl, pilot.DisplayId)
        };
    }

    public async Task<byte[]> GetSimBriefPdfAsync(string userId, CancellationToken ct)
    {
        var ofp = await GetSimBriefAsync(userId, ct);

        if (ofp.PdfUrl is null)
            throw new AppException(AppErrorKind.NotFound, "Your SimBrief OFP doesn't have a PDF. Generate a new one.");

        return await simBrief.DownloadPdfAsync(ofp.PdfUrl, ct);
    }

    private static string StaticId(Booking booking) => $"{Airline.Icao}_{booking.Id:N}";

    private static string CrewName(FlightCrewMemberDto member) =>
        member.DisplayId is null ? member.Name : $"{member.DisplayId} - {member.Name}";
}