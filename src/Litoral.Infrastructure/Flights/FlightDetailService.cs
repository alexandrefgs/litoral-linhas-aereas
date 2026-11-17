using Litoral.Application.Common;
using Litoral.Application.Flights;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Scoring;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Flights;

public sealed class FlightDetailService(LitoralDbContext db, UserManager<ApplicationUser> userManager, CrewCards crewCards) : IFlightDetailService
{
    private const int MaxTrackPoints = 1500;
    private const int MaxRecent = 12;

    public async Task<FlightDetailDto> GetAsync(string viewerUserId, Guid id, CancellationToken ct)
    {
        var pirep = await db.Pireps.AsNoTracking()
            .Include(p => p.Pilot).ThenInclude(p => p.Rank)
            .Include(p => p.Booking).ThenInclude(b => b.CrewPilot!).ThenInclude(c => c.Rank)
            .Include(p => p.Booking).ThenInclude(b => b.Route)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Flight not found.");

        var icaos = new[] { pirep.DepartureIcao, pirep.ArrivalIcao };
        var airports = await db.Airports.AsNoTracking().Where(a => icaos.Contains(a.Icao)).ToListAsync(ct);

        var aircraft = await db.Aircraft.AsNoTracking()
            .Include(a => a.AircraftType)
            .FirstOrDefaultAsync(a => a.Registration == pirep.Registration, ct);

        var simulator = pirep.FlightSessionId is Guid sessionId
            ? await db.FlightSessions.AsNoTracking().Where(s => s.Id == sessionId).Select(s => s.Simulator).FirstOrDefaultAsync(ct)
            : null;

        var (captain, firstOfficer) = await crewCards.BuildAsync(pirep.Pilot, pirep.Booking.CrewPilot, ct);
        var canSeePrivate = pirep.Pilot.UserId == viewerUserId || await IsStaffAsync(viewerUserId);

        return new FlightDetailDto(
            pirep.Id,
            pirep.Callsign,
            pirep.Status.ToString(),
            pirep.Source.ToString(),
            pirep.Booking.Type.ToString(),
            pirep.Network.ToString(),
            simulator,
            AirportDto(airports, pirep.DepartureIcao),
            AirportDto(airports, pirep.ArrivalIcao),
            pirep.PlannedArrivalIcao,
            captain,
            firstOfficer,
            aircraft is null
                ? new FlightAircraftDto(pirep.Registration, pirep.AircraftTypeIcao, pirep.AircraftTypeIcao, null, null, null, pirep.Booking.Route.IsCargo)
                : new FlightAircraftDto(aircraft.Registration, aircraft.AircraftType.IcaoCode, aircraft.AircraftType.Name, aircraft.Variant, aircraft.PhotoUrl, aircraft.PhotoCredit, aircraft.AircraftType.IsCargo),
            pirep.BlockOffAt,
            pirep.TakeoffAt,
            pirep.LandingAt,
            pirep.BlockOnAt,
            pirep.BlockMinutes,
            pirep.AirMinutes,
            pirep.LandingRateFpm,
            pirep.FuelUsedKg,
            pirep.FuelAtPushbackKg,
            pirep.DistanceNm,
            pirep.FlightRoute,
            pirep.CruiseAltitudeFt,
            pirep.OnlinePercent,
            pirep.ScorePoints,
            pirep.Conformity,
            pirep.HasRejection,
            pirep.SubmittedAt,
            pirep.ReviewedAt,
            canSeePrivate ? pirep.Comments : null,
            canSeePrivate ? pirep.ReviewNote : null,
            canSeePrivate,
            pirep.Items
                .Where(i => !i.IsVoided)
                .OrderBy(i => i.Outcome == ScoringOutcome.Reject ? 0 : 1)
                .ThenByDescending(i => i.Points)
                .Select(i => new PirepScoreItemDto(i.RuleId, i.Category.ToString(), i.Check.ToString(), i.Description, i.Outcome.ToString(), i.Points, i.Quantity, i.IsManual, i.IsVoided))
                .ToList(),
            pirep.Events
                .OrderBy(e => e.At)
                .Select(e => new PirepEventDto(e.Type.ToString(), e.At, e.DurationSeconds, e.Value, e.Detail))
                .ToList(),
            await TrackAsync(pirep.FlightSessionId, ct));
    }

    public async Task<RecentFlightsDto> RecentAsync(int take, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthTotal = await db.Pireps.CountAsync(p => p.Status != PirepStatus.Rejected && p.SubmittedAt >= monthStart, ct);

        var pireps = await db.Pireps.AsNoTracking()
            .Include(p => p.Pilot).ThenInclude(p => p.Rank)
            .Include(p => p.Booking).ThenInclude(b => b.Route)
            .Where(p => p.Status != PirepStatus.Rejected)
            .OrderByDescending(p => p.SubmittedAt)
            .Take(Math.Clamp(take, 1, MaxRecent))
            .ToListAsync(ct);

        var registrations = pireps.Select(p => p.Registration).Distinct().ToList();
        var aircraft = await db.Aircraft.AsNoTracking()
            .Include(a => a.AircraftType)
            .Where(a => registrations.Contains(a.Registration))
            .ToDictionaryAsync(a => a.Registration, ct);

        var departures = pireps.Select(p => p.DepartureIcao).Distinct().ToList();
        var countries = await db.Airports.AsNoTracking()
            .Where(a => departures.Contains(a.Icao))
            .ToDictionaryAsync(a => a.Icao, a => a.Country, ct);

        var flights = pireps.Select(p =>
        {
            aircraft.TryGetValue(p.Registration, out var plane);
            return new RecentFlightDto(
                p.Id,
                p.Callsign,
                p.DepartureIcao,
                p.ArrivalIcao,
                countries.GetValueOrDefault(p.DepartureIcao, ""),
                p.Registration,
                plane?.AircraftType.Name ?? p.AircraftTypeIcao,
                plane?.Variant,
                plane?.PhotoUrl,
                p.Booking.Route.IsCargo,
                p.BlockMinutes,
                p.Network.ToString(),
                p.Status.ToString(),
                p.Pilot.PilotNumber,
                p.Pilot.PublicName,
                p.Pilot.Rank.Code,
                p.Pilot.PhotoUrl,
                p.SubmittedAt);
        }).ToList();

        return new RecentFlightsDto(monthTotal, flights);
    }

    private static FlightAirportDto AirportDto(List<Airport> airports, string icao)
    {
        var airport = airports.FirstOrDefault(a => a.Icao == icao);
        return airport is null
            ? new FlightAirportDto(icao, null, icao, "", "", 0, 0, null)
            : new FlightAirportDto(airport.Icao, airport.IataCode, airport.Name, airport.City, airport.Country, airport.Latitude, airport.Longitude, airport.PhotoUrl);
    }

    private async Task<IReadOnlyList<TrackPointDto>> TrackAsync(Guid? sessionId, CancellationToken ct)
    {
        if (sessionId is not Guid id) return [];

        var points = await db.FlightTrackPoints.AsNoTracking()
            .Where(p => p.SessionId == id)
            .OrderBy(p => p.At)
            .Select(p => new TrackPointDto(p.At, p.Latitude, p.Longitude, p.AltitudeFt, p.GroundSpeedKt))
            .ToListAsync(ct);

        var step = Math.Max(1, (int)Math.Ceiling(points.Count / (double)MaxTrackPoints));
        return points.Where((_, i) => i % step == 0 || i == points.Count - 1).ToList();
    }

    private async Task<bool> IsStaffAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && (await userManager.GetRolesAsync(user)).Count > 0;
    }
}