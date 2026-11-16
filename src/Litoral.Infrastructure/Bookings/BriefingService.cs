using Litoral.Application.Bookings;
using Litoral.Application.Common;
using Litoral.Domain.Common;

namespace Litoral.Infrastructure.Bookings;

public sealed class BriefingService(BookingSupport support, IMetarProvider metars, ISimBriefProvider simBrief) : IBriefingService
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

        return new BriefingDto(BookingSupport.ToDto(booking), pilot.SimBriefUsername, dispatch, weather);
    }

    public async Task<SimBriefOfpDto> GetSimBriefAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(pilot.SimBriefUsername))
            throw new AppException(AppErrorKind.Validation, "Set your SimBrief username in your profile first.");

        var booking = await support.FindOpenBookingAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "You don't have an open flight.");

        return await simBrief.GetLatestAsync(pilot.SimBriefUsername, booking.Route.DepartureAirport.Icao, booking.Route.ArrivalAirport.Icao, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "No SimBrief flight plan was found for your username.");
    }
}