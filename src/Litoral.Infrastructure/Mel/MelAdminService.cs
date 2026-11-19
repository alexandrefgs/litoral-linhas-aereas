using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Mel;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Mel;

public sealed class MelAdminService(LitoralDbContext db, IAuditLog auditLog, AuditContext audit) : IMelAdminService
{
    public async Task<IReadOnlyList<MelItemDto>> CatalogAsync(CancellationToken ct)
    {
        var items = await db.MelItems.AsNoTracking().OrderBy(m => m.Kind).ThenBy(m => m.Code).ToListAsync(ct);
        var counts = await db.AircraftMelItems.AsNoTracking()
            .GroupBy(a => a.MelItemId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        return items.Select(m => MelSupport.ToDto(m, counts.GetValueOrDefault(m.Id))).ToList();
    }

    public async Task<MelItemDto> CreateAsync(MelItemRequest request, CancellationToken ct)
    {
        var item = new MelItem(request.Code, Parse<MelKind>(request.Kind, "type"), request.Title, Parse<MelCategory>(request.Category, "category"),
            request.CategoryADays, request.Restriction, request.Procedure, Parse<MelVerification>(request.Verification, "verification"),
            ParseCheck(request.AcarsCheck), request.CheckValue, request.CheckText, request.FuelPenaltyPercent,
            await TypesAsync(request.AircraftTypes, ct), request.AutoEligible, request.IsActive);

        await EnsureUniqueCodeAsync(item.Code, null, ct);
        db.MelItems.Add(item);
        await db.SaveChangesAsync(ct);
        await auditLog.WriteAsync(Entry("MEL item created", item), ct);
        return MelSupport.ToDto(item, 0);
    }

    public async Task<MelItemDto> UpdateAsync(Guid id, MelItemRequest request, CancellationToken ct)
    {
        var item = await FindItemAsync(id, ct);
        item.Update(request.Code, Parse<MelKind>(request.Kind, "type"), request.Title, Parse<MelCategory>(request.Category, "category"),
            request.CategoryADays, request.Restriction, request.Procedure, Parse<MelVerification>(request.Verification, "verification"),
            ParseCheck(request.AcarsCheck), request.CheckValue, request.CheckText, request.FuelPenaltyPercent,
            await TypesAsync(request.AircraftTypes, ct), request.AutoEligible, request.IsActive);

        await EnsureUniqueCodeAsync(item.Code, item.Id, ct);
        await db.SaveChangesAsync(ct);
        await auditLog.WriteAsync(Entry("MEL item updated", item), ct);

        var count = await db.AircraftMelItems.CountAsync(a => a.MelItemId == item.Id, ct);
        return MelSupport.ToDto(item, count);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var item = await FindItemAsync(id, ct);
        if (await db.AircraftMelItems.AnyAsync(a => a.MelItemId == id, ct))
            throw new AppException(AppErrorKind.Conflict, "This item was already opened on an aircraft. Deactivate it instead of deleting.");

        db.MelItems.Remove(item);
        await db.SaveChangesAsync(ct);
        await auditLog.WriteAsync(Entry("MEL item deleted", item), ct);
    }

    public async Task<MelOverviewDto> OverviewAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var open = await db.AircraftMelItems.AsNoTracking()
            .Include(a => a.MelItem)
            .Include(a => a.Aircraft).ThenInclude(x => x.AircraftType)
            .Include(a => a.Aircraft).ThenInclude(x => x.CurrentAirport)
            .Where(a => a.ClosedAt == null)
            .OrderBy(a => a.DueAt)
            .ToListAsync(ct);

        var aircraft = await db.Aircraft.AsNoTracking()
            .Where(a => a.Status != AircraftStatus.Inactive)
            .OrderBy(a => a.Registration)
            .Select(a => new MelAircraftOptionDto(a.Id, a.Registration, a.AircraftType.IcaoCode, a.CurrentAirport.Icao, a.Status.ToString()))
            .ToListAsync(ct);

        var types = await db.AircraftTypes.AsNoTracking().Select(t => t.IcaoCode).Distinct().OrderBy(c => c).ToListAsync(ct);

        return new MelOverviewDto(open.Select(a => MelSupport.ToDto(a, now)).ToList(), aircraft, types);
    }

    public async Task<MelOverviewDto> OpenAsync(OpenMelRequest request, CancellationToken ct)
    {
        var aircraft = await db.Aircraft.Include(a => a.AircraftType).FirstOrDefaultAsync(a => a.Id == request.AircraftId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Aircraft not found.");
        var item = await FindItemAsync(request.MelItemId, ct);

        if (!item.IsActive)
            throw new AppException(AppErrorKind.Validation, "This item is inactive.");
        if (!item.AppliesTo(aircraft.AircraftType.IcaoCode))
            throw new AppException(AppErrorKind.Validation, $"This item does not apply to the {aircraft.AircraftType.IcaoCode}.");
        if (await db.AircraftMelItems.AnyAsync(a => a.AircraftId == aircraft.Id && a.MelItemId == item.Id && a.ClosedAt == null, ct))
            throw new AppException(AppErrorKind.Conflict, $"{aircraft.Registration} already has this item open.");

        var actor = (await audit.ActorAsync(db, ct)).Name;
        db.AircraftMelItems.Add(new AircraftMelItem(aircraft, item, MelSource.Staff, actor, request.Note));
        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Fleet, "MEL item opened", "Aircraft", aircraft.Id.ToString(), aircraft.Registration,
            $"{MelSupport.Label(item)}\nCategory {item.Category} · {item.Days} days" + (string.IsNullOrWhiteSpace(request.Note) ? "" : $"\nNote: {request.Note.Trim()}")), ct);

        return await OverviewAsync(ct);
    }

    public async Task<MelOverviewDto> CloseAsync(Guid id, CancellationToken ct)
    {
        var open = await db.AircraftMelItems
            .Include(a => a.MelItem)
            .Include(a => a.Aircraft)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "MEL item not found.");

        if (!open.IsOpen)
            throw new AppException(AppErrorKind.Conflict, "This item is already closed.");

        var actor = (await audit.ActorAsync(db, ct)).Name;
        open.Close(actor, "Closed by the staff", DateTime.UtcNow);
        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Fleet, "MEL item closed", "Aircraft", open.AircraftId.ToString(), open.Aircraft.Registration,
            MelSupport.Label(open.MelItem)), ct);

        return await OverviewAsync(ct);
    }

    private async Task<List<string>> TypesAsync(IReadOnlyList<string>? codes, CancellationToken ct)
    {
        var list = (codes ?? []).Select(c => (c ?? "").Trim().ToUpperInvariant()).Where(c => c.Length > 0).Distinct().ToList();
        if (list.Count == 0) return list;

        var known = await db.AircraftTypes.Where(t => list.Contains(t.IcaoCode)).Select(t => t.IcaoCode).Distinct().ToListAsync(ct);
        var missing = list.Except(known).ToList();
        if (missing.Count > 0)
            throw new AppException(AppErrorKind.Validation, $"Aircraft type not found: {string.Join(", ", missing)}.");
        return list;
    }

    private async Task EnsureUniqueCodeAsync(string code, Guid? exceptId, CancellationToken ct)
    {
        if (await db.MelItems.AnyAsync(m => m.Code == code && m.Id != exceptId, ct))
            throw new AppException(AppErrorKind.Conflict, $"There is already an item with the code {code}.");
    }

    private async Task<MelItem> FindItemAsync(Guid id, CancellationToken ct) =>
        await db.MelItems.FirstOrDefaultAsync(m => m.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "MEL item not found.");

    private static AuditEntry Entry(string action, MelItem item) =>
        new(AuditCategory.Fleet, action, "MEL item", item.Id.ToString(), MelSupport.Label(item),
            $"Category {item.Category} · {item.Days} days\nVerification: {item.Verification}{(item.AutoEligible ? "\nOpened by the system" : "")}");

    private static T Parse<T>(string? value, string name) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new AppException(AppErrorKind.Validation, $"Choose a valid {name}.");

    private static MelAcarsCheck ParseCheck(string? value) =>
        string.IsNullOrWhiteSpace(value) ? MelAcarsCheck.None : Parse<MelAcarsCheck>(value, "ACARS check");
}