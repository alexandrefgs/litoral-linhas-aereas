using System.Globalization;
using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Events;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Events;

public sealed class EventAdminService(LitoralDbContext db, IPhotoStorage photos, Notifier notifier) : IEventAdminService
{
    private const string ImageFolder = "events";

    public async Task<IReadOnlyList<AdminEventDto>> ListAsync(CancellationToken ct)
    {
        var events = await db.AirlineEvents.AsNoTracking().OrderByDescending(e => e.StartsAt).ToListAsync(ct);
        var stats = await StatsAsync(events.Select(e => e.Id).ToList(), ct);
        var now = DateTime.UtcNow;
        return events.Select(e => ToDto(e, stats.GetValueOrDefault(e.Id), now)).ToList();
    }

    public async Task<AdminEventDto> CreateAsync(EventRequest request, CancellationToken ct)
    {
        var airports = await ResolveAirportsAsync(request.Airports, ct);
        var item = new AirlineEvent(request.Title, request.Description, EventSupport.Utc(request.StartsAt), EventSupport.Utc(request.EndsAt), airports, request.BonusPoints);
        db.AirlineEvents.Add(item);

        if (item.SetPublished(request.IsPublished, DateTime.UtcNow))
            await NotifyAsync(item, ct);

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(item.Id, ct);
    }

    public async Task<AdminEventDto> UpdateAsync(Guid id, EventRequest request, CancellationToken ct)
    {
        var item = await FindAsync(id, ct);
        var airports = await ResolveAirportsAsync(request.Airports, ct);
        item.Update(request.Title, request.Description, EventSupport.Utc(request.StartsAt), EventSupport.Utc(request.EndsAt), airports, request.BonusPoints);

        if (item.SetPublished(request.IsPublished, DateTime.UtcNow))
            await NotifyAsync(item, ct);

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(item.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var item = await FindAsync(id, ct);

        if (await db.AirlineEventFlights.AnyAsync(f => f.EventId == id, ct))
            throw new AppException(AppErrorKind.Conflict, "Pilots already earned points in this event. Unpublish it instead of deleting.");

        var image = item.ImageUrl;
        db.AirlineEvents.Remove(item);
        await db.SaveChangesAsync(ct);
        photos.Delete(image);
    }

    public async Task<AdminEventDto> UploadImageAsync(Guid id, PhotoUpload upload, CancellationToken ct)
    {
        var item = await FindAsync(id, ct);
        var url = await photos.SaveAsync(ImageFolder, upload, ct);
        var previous = item.SetImage(url);
        await db.SaveChangesAsync(ct);
        photos.Delete(previous);
        return await ToDtoAsync(item.Id, ct);
    }

    public async Task<AdminEventDto> RemoveImageAsync(Guid id, CancellationToken ct)
    {
        var item = await FindAsync(id, ct);
        var previous = item.SetImage(null);
        await db.SaveChangesAsync(ct);
        photos.Delete(previous);
        return await ToDtoAsync(item.Id, ct);
    }

    private async Task<List<string>> ResolveAirportsAsync(IReadOnlyList<string>? airports, CancellationToken ct)
    {
        var codes = (airports ?? []).Select(a => (a ?? "").Trim().ToUpperInvariant()).Where(a => a.Length > 0).Distinct().ToList();
        if (codes.Any(c => c.Length != 4))
            throw new AppException(AppErrorKind.Validation, "Use 4-letter ICAO codes for the airports.");

        var known = await db.Airports.Where(a => codes.Contains(a.Icao)).Select(a => a.Icao).ToListAsync(ct);
        var missing = codes.Except(known).ToList();
        if (missing.Count > 0)
            throw new AppException(AppErrorKind.Validation, $"Airport not registered: {string.Join(", ", missing)}.");

        return codes;
    }

    private async Task NotifyAsync(AirlineEvent item, CancellationToken ct)
    {
        var pilots = await db.Pilots.Where(p => p.IsActive).Select(p => p.Id).ToListAsync(ct);
        var where = item.AirportList.Count == 0 ? "any airport" : string.Join(", ", item.AirportList);
        notifier.SendMany(pilots, NotificationType.Event, $"New event: {item.Title}",
            $"{item.StartsAt.ToString("dd MMM HH:mm", CultureInfo.InvariantCulture)}Z · {where}{(item.BonusPoints > 0 ? $" · +{item.BonusPoints:N0} points per flight" : "")}",
            "/tours?tab=events");
    }

    private sealed record Stats(int Flights, int Pilots);

    private async Task<Dictionary<Guid, Stats>> StatsAsync(List<Guid> ids, CancellationToken ct) =>
        await db.AirlineEventFlights.AsNoTracking()
            .Where(f => ids.Contains(f.EventId))
            .GroupBy(f => f.EventId)
            .Select(g => new { g.Key, Flights = g.Count(), Pilots = g.Select(f => f.PilotId).Distinct().Count() })
            .ToDictionaryAsync(g => g.Key, g => new Stats(g.Flights, g.Pilots), ct);

    private async Task<AirlineEvent> FindAsync(Guid id, CancellationToken ct) =>
        await db.AirlineEvents.FirstOrDefaultAsync(e => e.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Event not found.");

    private async Task<AdminEventDto> ToDtoAsync(Guid id, CancellationToken ct)
    {
        var item = await db.AirlineEvents.AsNoTracking().FirstAsync(e => e.Id == id, ct);
        var stats = await StatsAsync([id], ct);
        return ToDto(item, stats.GetValueOrDefault(id), DateTime.UtcNow);
    }

    private static AdminEventDto ToDto(AirlineEvent e, Stats? stats, DateTime now) =>
        new(e.Id, e.Title, e.Description, e.ImageUrl, e.StartsAt, e.EndsAt, EventSupport.State(e, now), e.IsPublished,
            e.AirportList, e.BonusPoints, stats?.Flights ?? 0, stats?.Pilots ?? 0);
}