using Litoral.Application.Common;
using Litoral.Application.Pilots;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Pilots;

public sealed class ProfileService(LitoralDbContext db, UserManager<ApplicationUser> userManager) : IProfileService
{
    private const int TopRoutesLimit = 10;
    private const int MaxPageSize = 100;

    public async Task<ProfileDto> GetAsync(string viewerUserId, int pilotNumber, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(pilotNumber, ct);

        var isOwner = pilot.UserId == viewerUserId;
        var canSeePrivate = isOwner || await IsStaffAsync(viewerUserId);

        ProfilePrivateDto? privateData = null;
        if (canSeePrivate)
        {
            var user = await userManager.FindByIdAsync(pilot.UserId);
            privateData = new ProfilePrivateDto(user?.Email ?? "", pilot.BirthDate, pilot.DiscordId);
        }

        var flights = Flown(pilot.Id);

        var monthly = await flights
            .GroupBy(p => new { p.BlockOnAt.Year, p.BlockOnAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Flights = g.Count(), Minutes = g.Sum(p => p.BlockMinutes) })
            .ToListAsync(ct);

        var activity = monthly
            .GroupBy(m => m.Year)
            .OrderByDescending(g => g.Key)
            .Select(g => new ActivityYearDto(
                g.Key,
                g.Sum(m => m.Flights),
                g.Sum(m => m.Minutes),
                Enumerable.Range(1, 12)
                    .Select(month => g.FirstOrDefault(m => m.Month == month) is { } m
                        ? new ActivityMonthDto(month, m.Flights, m.Minutes)
                        : new ActivityMonthDto(month, 0, 0))
                    .ToList()))
            .ToList();

        var topRoutes = await flights
            .GroupBy(p => new { p.DepartureIcao, p.ArrivalIcao })
            .Select(g => new { g.Key.DepartureIcao, g.Key.ArrivalIcao, Flights = g.Count(), Last = g.Max(p => p.BlockOnAt) })
            .OrderByDescending(r => r.Flights)
            .ThenByDescending(r => r.Last)
            .Take(TopRoutesLimit)
            .ToListAsync(ct);

        var airports = await AirportsAsync(topRoutes.SelectMany(r => new[] { r.DepartureIcao, r.ArrivalIcao }), ct);

        var simCodes = pilot.SimAircraftList;
        var simAircraft = simCodes.Count == 0
            ? new List<ProfileAircraftDto>()
            : await db.AircraftTypes.AsNoTracking()
                .Where(t => simCodes.Contains(t.IcaoCode))
                .OrderBy(t => t.Name)
                .Select(t => new ProfileAircraftDto(t.IcaoCode, t.Name, db.Aircraft.Count(a => a.AircraftTypeId == t.Id)))
                .ToListAsync(ct);

        return new ProfileDto(
            pilot.PilotNumber,
            pilot.DisplayId,
            pilot.FirstName,
            pilot.LastName,
            pilot.PublicName,
            pilot.Rank.Code,
            pilot.Rank.Name,
            pilot.IsActive,
            pilot.BaseIcao,
            pilot.CurrentLocationIcao,
            pilot.CountryCode,
            pilot.State,
            pilot.City,
            pilot.VatsimId,
            pilot.IvaoId,
            pilot.AgeOn(DateOnly.FromDateTime(DateTime.UtcNow)),
            pilot.CreatedAt,
            pilot.TotalFlightMinutes,
            activity.Sum(y => y.Flights),
            pilot.Points,
            pilot.PhotoUrl,
            pilot.CoverUrl,
            isOwner,
            privateData,
            simAircraft,
            activity,
            topRoutes
                .Select(r => new TopRouteDto(AirportOf(airports, r.DepartureIcao), AirportOf(airports, r.ArrivalIcao), r.Flights))
                .ToList());
    }

    public async Task<FlightMapDto> FlightMapAsync(int pilotNumber, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(pilotNumber, ct);

        var routes = await Flown(pilot.Id)
            .GroupBy(p => new { p.DepartureIcao, p.ArrivalIcao })
            .Select(g => new FlightMapRouteDto(g.Key.DepartureIcao, g.Key.ArrivalIcao, g.Count()))
            .ToListAsync(ct);

        var airports = await AirportsAsync(routes.SelectMany(r => new[] { r.DepartureIcao, r.ArrivalIcao }), ct);

        return new FlightMapDto(
            airports.Values.OrderBy(a => a.Icao).ToList(),
            routes.Where(r => airports.ContainsKey(r.DepartureIcao) && airports.ContainsKey(r.ArrivalIcao)).ToList());
    }

    public async Task<PagedResult<ProfileFlightDto>> FlightsAsync(int pilotNumber, ProfileFlightsQuery query, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(pilotNumber, ct);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 5, MaxPageSize);
        var flights = Flown(pilot.Id);

        var term = query.Search?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(term))
        {
            flights = flights.Where(p =>
                p.Callsign.Contains(term) ||
                p.DepartureIcao.Contains(term) ||
                p.ArrivalIcao.Contains(term) ||
                p.Registration.Contains(term) ||
                p.AircraftTypeIcao.Contains(term));
        }

        var total = await flights.CountAsync(ct);

        var ordered = query.Sort == "time"
            ? flights.OrderByDescending(p => p.BlockMinutes).ThenByDescending(p => p.BlockOnAt)
            : flights.OrderByDescending(p => p.BlockOnAt);

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProfileFlightDto(
                p.Id, p.Callsign, p.DepartureIcao, p.ArrivalIcao, p.AircraftTypeIcao,
                p.Registration, p.BlockMinutes, p.ScorePoints, p.Status == PirepStatus.Rejected, p.BlockOnAt))
            .ToListAsync(ct);

        return new PagedResult<ProfileFlightDto>(items, total, page, pageSize);
    }

    private IQueryable<Pirep> Flown(Guid pilotId) =>
        db.Pireps.AsNoTracking().Where(p => p.PilotId == pilotId && p.Status != PirepStatus.Pending);

    private async Task<Pilot> FindPilotAsync(int pilotNumber, CancellationToken ct) =>
        await db.Pilots.AsNoTracking().Include(p => p.Rank).FirstOrDefaultAsync(p => p.PilotNumber == pilotNumber, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

    private async Task<bool> IsStaffAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && (await userManager.GetRolesAsync(user)).Count > 0;
    }

    private async Task<Dictionary<string, ProfileAirportDto>> AirportsAsync(IEnumerable<string> icaos, CancellationToken ct)
    {
        var codes = icaos.Distinct().ToList();
        if (codes.Count == 0) return new Dictionary<string, ProfileAirportDto>();

        return await db.Airports.AsNoTracking()
            .Where(a => codes.Contains(a.Icao))
            .Select(a => new ProfileAirportDto(a.Icao, a.Name, a.City, a.Country, a.Latitude, a.Longitude))
            .ToDictionaryAsync(a => a.Icao, ct);
    }

    private static ProfileAirportDto AirportOf(Dictionary<string, ProfileAirportDto> airports, string icao) =>
        airports.TryGetValue(icao, out var airport) ? airport : new ProfileAirportDto(icao, icao, "", "", 0, 0);
}