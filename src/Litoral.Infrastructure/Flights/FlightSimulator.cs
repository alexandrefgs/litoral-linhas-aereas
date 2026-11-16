using Litoral.Application.Common;
using Litoral.Application.Flights;
using Litoral.Infrastructure.Bookings;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Flights;

public sealed class FlightSimulator(LitoralDbContext db, BookingSupport support, IFlightTrackingService tracking) : IFlightSimulator
{
    private const int TaxiOutMinutes = 12;
    private const int TaxiInMinutes = 5;

    public async Task<PirepDto> SimulateAsync(string userId, SimulateFlightRequest request, CancellationToken ct)
    {
        var scenario = (request.Scenario ?? "clean").Trim().ToLowerInvariant();
        if (scenario is not ("clean" or "violations" or "rejected"))
            throw new AppException(AppErrorKind.Validation, "Choose a scenario: clean, violations or rejected.");

        var pilot = await support.GetPilotAsync(userId, ct);
        var booking = await support.FindOpenBookingAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Book a flight before simulating it.");

        var from = booking.Route.DepartureAirport;
        var to = booking.Route.ArrivalAirport;
        var airMinutes = Math.Max(20, booking.Route.EstimatedMinutes);
        var cruiseFt = airMinutes >= 90 ? 37000 : airMinutes >= 50 ? 33000 : 24000;

        var blockOff = DateTime.UtcNow.AddMinutes(-(TaxiOutMinutes + airMinutes + TaxiInMinutes + 1));
        var takeoff = blockOff.AddMinutes(TaxiOutMinutes);
        var landing = takeoff.AddMinutes(airMinutes);
        var blockOn = landing.AddMinutes(TaxiInMinutes);

        await tracking.StartAsync(userId, new StartFlightRequest("Vatsim", "MSFS 2024", "Litoral simulator"), ct);

        var positions = new List<PositionReport>();

        PositionReport At(DateTime at, double progress, int alt, int agl, int ias, int gs, int vs, int flaps, bool gear, bool ground, string phase)
        {
            var lat = from.Latitude + (to.Latitude - from.Latitude) * progress;
            var lon = from.Longitude + (to.Longitude - from.Longitude) * progress;
            var heading = (int)((Math.Atan2(to.Longitude - from.Longitude, to.Latitude - from.Latitude) * 180 / Math.PI + 360) % 360);
            return new PositionReport(at, lat, lon, alt, agl, ias, gs, heading, vs, flaps, gear, ground, phase);
        }

        var taxiSpeed = scenario == "violations" ? 36 : 18;
        for (var s = 0; s < TaxiOutMinutes * 60; s += 30)
            positions.Add(At(blockOff.AddSeconds(s), 0, 20, 0, taxiSpeed, taxiSpeed, 0, 5, true, true, "TaxiOut"));

        positions.Add(At(takeoff, 0, 20, 0, 150, 150, 0, 5, true, true, "Takeoff"));
        positions.Add(At(takeoff.AddSeconds(30), 0.005, 800, 780, 165, 170, 2500, 5, false, false, "Climb"));

        var flapsOneSpeed = scenario == "rejected" ? 262 : 220;
        positions.Add(At(takeoff.AddSeconds(60), 0.01, 2500, 2480, flapsOneSpeed, flapsOneSpeed + 10, 2500, 1, false, false, "Climb"));
        positions.Add(At(takeoff.AddSeconds(75), 0.012, 3200, 3180, flapsOneSpeed, flapsOneSpeed + 10, 2500, 1, false, false, "Climb"));

        for (var m = 2; m < airMinutes - 1; m++)
        {
            var progress = (double)m / airMinutes;
            var climb = Math.Min(1, m / 20.0);
            var descent = Math.Min(1, (airMinutes - m) / 25.0);
            var alt = (int)(cruiseFt * Math.Min(climb, descent));
            var phase = climb < 1 ? "Climb" : descent < 1 ? "Descent" : "Cruise";
            positions.Add(At(takeoff.AddMinutes(m), progress, alt, alt, alt > 10000 ? 290 : 250, alt > 10000 ? 450 : 260, phase == "Cruise" ? 0 : 1500, 0, false, false, phase));
        }

        positions.Add(At(landing.AddSeconds(-120), 0.99, 1500, 1480, 150, 145, -700, 30, true, false, "Approach"));
        positions.Add(At(landing.AddSeconds(-30), 0.998, 200, 180, 142, 138, -700, 30, true, false, "Approach"));
        positions.Add(At(landing, 1, 20, 0, 138, 135, 0, 30, true, true, "Landing"));

        for (var s = 60; s <= TaxiInMinutes * 60; s += 30)
            positions.Add(At(landing.AddSeconds(s), 1, 20, 0, 15, 15, 0, 0, true, true, "TaxiIn"));

        foreach (var chunk in positions.Chunk(200))
            await tracking.ReportPositionsAsync(userId, new PositionBatchRequest(chunk), ct);

        var events = scenario switch
        {
            "violations" => new List<FlightEventReport>
            {
                new("Pause", takeoff.AddMinutes(airMinutes / 2), 45, null, "Simulator paused"),
                new("Overspeed", takeoff.AddMinutes(airMinutes / 3), 9, 345, "Overspeed above VMO")
            },
            "rejected" => new List<FlightEventReport>
            {
                new("Slew", takeoff.AddMinutes(airMinutes / 2), 20, null, "Slew mode used")
            },
            _ => new List<FlightEventReport>()
        };

        var landingRate = scenario switch { "violations" => -430, "rejected" => -640, _ => -128 };

        return await tracking.CompleteAsync(userId, new CompleteFlightRequest(
            to.Icao,
            blockOff,
            takeoff,
            landing,
            blockOn,
            landingRate,
            9800,
            (decimal)(airMinutes * 42),
            "kg",
            booking.Route.DistanceNm,
            $"{from.Icao} DCT {to.Icao}",
            cruiseFt,
            100,
            2,
            TaxiInMinutes * 60,
            false,
            scenario == "rejected" ? 11.6m : 8.2m,
            148,
            5,
            30,
            1200,
            1500,
            false,
            false,
            $"Simulated flight ({scenario}).",
            events), ct);
    }
}