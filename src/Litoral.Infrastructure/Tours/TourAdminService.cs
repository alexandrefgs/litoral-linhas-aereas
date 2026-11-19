using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Tours;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Tours;

public sealed class TourAdminService(LitoralDbContext db, TourSupport support, IPhotoStorage photos, Notifier notifier) : ITourAdminService
{
    private const string ImageFolder = "tours";

    private sealed record ResolvedInput(int RankLevel, List<AircraftType> Types, List<Airport> Stops, DateTime? StartsAt, DateTime? EndsAt);

    public async Task<TourOptionsDto> OptionsAsync(CancellationToken ct)
    {
        var airports = await db.Airports.AsNoTracking()
            .OrderBy(a => a.Icao)
            .Select(a => new TourOptionAirportDto(a.Icao, a.Name, a.City, a.MinimumRankLevel))
            .ToListAsync(ct);

        var ranks = await db.Ranks.AsNoTracking()
            .OrderBy(r => r.Level)
            .Select(r => new TourOptionRankDto(r.Code, r.Name, r.Level))
            .ToListAsync(ct);

        var types = await db.AircraftTypes.AsNoTracking()
            .OrderBy(t => t.IsCargo).ThenBy(t => t.IcaoCode)
            .Select(t => new TourOptionTypeDto(t.IcaoCode, t.Name, t.MaxRangeNm, t.IsCargo))
            .ToListAsync(ct);

        return new TourOptionsDto(airports, ranks, types);
    }

    public async Task<IReadOnlyList<AdminTourDto>> ListAsync(CancellationToken ct)
    {
        var tours = await support.Query().AsNoTracking().OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
        var ids = tours.Select(t => t.Id).ToList();

        var progress = await db.TourProgresses.AsNoTracking()
            .Where(p => ids.Contains(p.TourId))
            .GroupBy(p => p.TourId)
            .Select(g => new { g.Key, Started = g.Count(), Completed = g.Count(p => p.CompletedAt != null) })
            .ToDictionaryAsync(g => g.Key, ct);

        var locked = await LockedTourIdsAsync(ids, ct);
        var ranks = await support.RanksByLevelAsync(ct);
        var now = DateTime.UtcNow;

        return tours.Select(t => ToDto(t, progress.GetValueOrDefault(t.Id)?.Started ?? 0, progress.GetValueOrDefault(t.Id)?.Completed ?? 0, locked.Contains(t.Id), ranks, now)).ToList();
    }

    public async Task<AdminTourDto> CreateAsync(TourRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppException(AppErrorKind.Validation, "The tour title is required.");

        var input = await ResolveAsync(request, ct);
        var routes = await support.CreateRoutesAsync(input.Stops, input.Types, [], ct);

        var award = new Award(request.Title, TourSupport.AwardDescription(request.Title.Trim()), AwardKind.Tour);
        db.Awards.Add(award);

        var tour = new Tour(request.Title, request.Description, input.RankLevel, request.BonusPoints, input.StartsAt, input.EndsAt, award, routes);
        db.Tours.Add(tour);

        if (tour.SetPublished(request.IsPublished, DateTime.UtcNow))
            await NotifyAsync(tour, ct);

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(tour.Id, ct);
    }

    public async Task<AdminTourDto> UpdateAsync(Guid id, TourRequest request, CancellationToken ct)
    {
        var tour = await FindAsync(id, ct);
        var input = await ResolveAsync(request, ct);

        tour.Update(request.Title, request.Description, input.RankLevel, request.BonusPoints, input.StartsAt, input.EndsAt);
        tour.Award.Update(tour.Title, TourSupport.AwardDescription(tour.Title), tour.Award.IsActive);

        var sameLegs = SameLegs(tour, input);
        if (!sameLegs)
        {
            if ((await LockedTourIdsAsync([tour.Id], ct)).Count > 0)
                throw new AppException(AppErrorKind.Conflict, "Pilots already booked legs of this tour, so the legs and aircraft can't change anymore.");

            var oldRoutes = tour.Legs.Select(l => l.Route).ToList();
            var routes = await support.CreateRoutesAsync(input.Stops, input.Types, oldRoutes.Select(r => r.FlightNumber), ct);
            tour.ReplaceLegs(routes);
            db.Routes.RemoveRange(oldRoutes);
        }

        if (tour.SetPublished(request.IsPublished, DateTime.UtcNow))
            await NotifyAsync(tour, ct);

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(tour.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var tour = await FindAsync(id, ct);

        if ((await LockedTourIdsAsync([tour.Id], ct)).Count > 0)
            throw new AppException(AppErrorKind.Conflict, "Pilots already flew this tour. Unpublish it instead of deleting.");

        var routes = tour.Legs.Select(l => l.Route).ToList();
        var image = tour.ImageUrl;
        var awardImage = tour.Award.ImageUrl;

        db.Tours.Remove(tour);
        db.Routes.RemoveRange(routes);
        db.Awards.Remove(tour.Award);
        await db.SaveChangesAsync(ct);

        photos.Delete(image);
        photos.Delete(awardImage);
    }

    public async Task<AdminTourDto> UploadImageAsync(Guid id, PhotoUpload upload, CancellationToken ct)
    {
        var tour = await FindAsync(id, ct);
        var url = await photos.SaveAsync(ImageFolder, upload, ct);
        var previous = tour.SetImage(url);
        await db.SaveChangesAsync(ct);
        photos.Delete(previous);
        return await ToDtoAsync(tour.Id, ct);
    }

    public async Task<AdminTourDto> RemoveImageAsync(Guid id, CancellationToken ct)
    {
        var tour = await FindAsync(id, ct);
        var previous = tour.SetImage(null);
        await db.SaveChangesAsync(ct);
        photos.Delete(previous);
        return await ToDtoAsync(tour.Id, ct);
    }

    private async Task<ResolvedInput> ResolveAsync(TourRequest request, CancellationToken ct)
    {
        var stops = (request.Stops ?? []).Select(s => (s ?? "").Trim().ToUpperInvariant()).ToList();
        if (stops.Count is < 3 or > Tour.MaxLegs + 1 || stops.Any(s => s.Length != 4))
            throw new AppException(AppErrorKind.Validation, $"Enter the departure and between 2 and {Tour.MaxLegs} destinations.");
        for (var i = 1; i < stops.Count; i++)
            if (stops[i] == stops[i - 1])
                throw new AppException(AppErrorKind.Validation, $"Stop {i + 1} is the same airport as stop {i}.");

        var codes = (request.AircraftTypes ?? []).Select(c => (c ?? "").Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct().ToList();
        if (codes.Count == 0)
            throw new AppException(AppErrorKind.Validation, "Select at least one aircraft type.");

        var types = await db.AircraftTypes.Where(t => t.IsCargo == request.IsCargo && codes.Contains(t.IcaoCode)).ToListAsync(ct);
        var missingTypes = codes.Except(types.Select(t => t.IcaoCode)).ToList();
        if (missingTypes.Count > 0)
            throw new AppException(AppErrorKind.Validation, $"{(request.IsCargo ? "Cargo" : "Passenger")} aircraft type not found: {string.Join(", ", missingTypes)}.");

        var airports = await db.Airports.Where(a => stops.Contains(a.Icao)).ToListAsync(ct);
        var missingAirports = stops.Except(airports.Select(a => a.Icao)).Distinct().ToList();
        if (missingAirports.Count > 0)
            throw new AppException(AppErrorKind.Validation, $"Airport not registered: {string.Join(", ", missingAirports)}.");

        var rank = await db.Ranks.AsNoTracking().FirstOrDefaultAsync(r => r.Code == (request.MinimumRank ?? "").Trim().ToUpper(), ct)
            ?? throw new AppException(AppErrorKind.Validation, "Select the minimum rank.");

        var restricted = airports.Where(a => a.MinimumRankLevel > rank.Level).Select(a => a.Icao).ToList();
        if (restricted.Count > 0)
            throw new AppException(AppErrorKind.Validation, $"{string.Join(", ", restricted)} require a higher rank than {rank.Name}. Raise the minimum rank of the tour.");

        var byIcao = airports.ToDictionary(a => a.Icao);
        return new ResolvedInput(rank.Level, types, stops.Select(s => byIcao[s]).ToList(), Utc(request.StartsAt), Utc(request.EndsAt));
    }

    private static bool SameLegs(Tour tour, ResolvedInput input)
    {
        var legs = tour.OrderedLegs;
        if (legs.Count != input.Stops.Count - 1) return false;

        var current = legs.Select(l => l.Route.DepartureAirport.Icao).Append(legs[^1].Route.ArrivalAirport.Icao);
        if (!current.SequenceEqual(input.Stops.Select(s => s.Icao))) return false;

        return TourSupport.AircraftTypeIds(tour).SetEquals(input.Types.Select(t => t.Id));
    }

    private async Task<HashSet<Guid>> LockedTourIdsAsync(IReadOnlyCollection<Guid> tourIds, CancellationToken ct) =>
        (await db.Bookings.AsNoTracking()
            .Where(b => b.TourLegId != null && tourIds.Contains(b.TourLeg!.TourId))
            .Select(b => b.TourLeg!.TourId)
            .Distinct()
            .ToListAsync(ct))
        .ToHashSet();

    private async Task NotifyAsync(Tour tour, CancellationToken ct)
    {
        var pilots = await db.Pilots
            .Where(p => p.IsActive && p.Rank.Level >= tour.MinimumRankLevel)
            .Select(p => p.Id)
            .ToListAsync(ct);

        notifier.SendMany(pilots, NotificationType.Tour, $"New tour: {tour.Title}",
            $"{tour.Legs.Count} legs{(tour.BonusPoints > 0 ? $" · +{tour.BonusPoints:N0} points" : "")} and the {tour.Award.Name} award.",
            "/tours");
    }

    private async Task<Tour> FindAsync(Guid id, CancellationToken ct) =>
        await support.Query().FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Tour not found.");

    private async Task<AdminTourDto> ToDtoAsync(Guid id, CancellationToken ct)
    {
        var tour = await support.Query().AsNoTracking().FirstAsync(t => t.Id == id, ct);
        var started = await db.TourProgresses.CountAsync(p => p.TourId == id, ct);
        var completed = await db.TourProgresses.CountAsync(p => p.TourId == id && p.CompletedAt != null, ct);
        var locked = (await LockedTourIdsAsync([id], ct)).Count > 0;
        return ToDto(tour, started, completed, locked, await support.RanksByLevelAsync(ct), DateTime.UtcNow);
    }

    private static AdminTourDto ToDto(Tour tour, int started, int completed, bool locked, IReadOnlyDictionary<int, Rank> ranks, DateTime now) =>
        new(
            tour.Id,
            tour.Title,
            tour.Description,
            tour.ImageUrl,
            tour.StartsAt,
            tour.EndsAt,
            TourSupport.State(tour, now),
            tour.IsPublished,
            tour.MinimumRankLevel,
            ranks.GetValueOrDefault(tour.MinimumRankLevel)?.Name ?? $"Level {tour.MinimumRankLevel}",
            TourSupport.IsCargo(tour),
            TourSupport.AircraftTypes(tour),
            tour.BonusPoints,
            TourSupport.Award(tour),
            TourSupport.Legs(tour),
            started,
            completed,
            locked);

    private static DateTime? Utc(DateTime? value) => value is not DateTime v ? null
        : v.Kind == DateTimeKind.Utc ? v
        : DateTime.SpecifyKind(v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v, DateTimeKind.Utc);
}