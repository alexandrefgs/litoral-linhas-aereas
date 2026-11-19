using Litoral.Application.Admin;
using Litoral.Application.Awards;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Awards;

public sealed class AwardAdminService(
    LitoralDbContext db,
    IPhotoStorage photos,
    Notifier notifier,
    IAuditLog auditLog,
    AuditContext audit) : IAwardAdminService
{
    private const string ImageFolder = "awards";

    public async Task<IReadOnlyList<AdminAwardDto>> ListAsync(CancellationToken ct) =>
        await Project(db.Awards.AsNoTracking().OrderBy(a => a.Kind).ThenBy(a => a.Name)).ToListAsync(ct);

    public async Task<AdminAwardDto> CreateAsync(AwardRequest request, CancellationToken ct)
    {
        var award = new Award(request.Name, request.Description, AwardKind.Manual);
        if (!request.IsActive)
            award.Update(request.Name, request.Description, false);

        await EnsureUniqueNameAsync(award.Name, null, ct);
        db.Awards.Add(award);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(award.Id, ct);
    }

    public async Task<AdminAwardDto> UpdateAsync(Guid id, AwardRequest request, CancellationToken ct)
    {
        var award = await FindAsync(id, ct);
        award.Update(request.Name, request.Description, request.IsActive);
        await EnsureUniqueNameAsync(award.Name, award.Id, ct);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(award.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var award = await FindAsync(id, ct);

        if (award.Kind != AwardKind.Manual)
            throw new AppException(AppErrorKind.Validation, "Tour awards are removed together with their tour.");

        var holders = await db.PilotAwards.CountAsync(a => a.AwardId == id, ct);
        if (holders > 0)
            throw new AppException(AppErrorKind.Conflict, $"{holders} pilot(s) hold this award. Revoke it from them or deactivate the award instead.");

        var image = award.ImageUrl;
        db.Awards.Remove(award);
        await db.SaveChangesAsync(ct);
        photos.Delete(image);
    }

    public async Task<AdminAwardDto> UploadImageAsync(Guid id, PhotoUpload upload, CancellationToken ct)
    {
        var award = await FindAsync(id, ct);
        var url = await photos.SaveAsync(ImageFolder, upload, ct);
        var previous = award.SetImage(url);
        await db.SaveChangesAsync(ct);
        photos.Delete(previous);
        return await ToDtoAsync(award.Id, ct);
    }

    public async Task<AdminAwardDto> RemoveImageAsync(Guid id, CancellationToken ct)
    {
        var award = await FindAsync(id, ct);
        var previous = award.SetImage(null);
        await db.SaveChangesAsync(ct);
        photos.Delete(previous);
        return await ToDtoAsync(award.Id, ct);
    }

    public async Task<IReadOnlyList<AwardHolderDto>> HoldersAsync(Guid id, CancellationToken ct)
    {
        if (!await db.Awards.AnyAsync(a => a.Id == id, ct))
            throw new AppException(AppErrorKind.NotFound, "Award not found.");

        var rows = await db.PilotAwards
            .AsNoTracking()
            .Include(a => a.Pilot).ThenInclude(p => p.Rank)
            .Where(a => a.AwardId == id)
            .OrderByDescending(a => a.GrantedAt)
            .ToListAsync(ct);

        return rows.Select(a => new AwardHolderDto(
                a.PilotId,
                a.Pilot.PilotNumber,
                a.Pilot.DisplayId,
                a.Pilot.PublicName,
                a.Pilot.PhotoUrl,
                a.Note,
                a.GrantedBy,
                a.GrantedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<AwardPilotOptionDto>> PilotsAsync(CancellationToken ct)
    {
        var pilots = await db.Pilots
            .AsNoTracking()
            .Include(p => p.Rank)
            .Where(p => p.IsActive)
            .OrderBy(p => p.PilotNumber)
            .ToListAsync(ct);

        return pilots.Select(p => new AwardPilotOptionDto(p.Id, p.PilotNumber, p.DisplayId, p.PublicName, p.PhotoUrl)).ToList();
    }

    public async Task<IReadOnlyList<AwardHolderDto>> GrantAsync(Guid id, GrantAwardRequest request, CancellationToken ct)
    {
        var award = await FindAsync(id, ct);

        if (award.Kind != AwardKind.Manual)
            throw new AppException(AppErrorKind.Validation, "Tour awards are granted automatically when the tour is completed.");
        if (!award.IsActive)
            throw new AppException(AppErrorKind.Validation, "Activate the award before granting it.");

        var pilot = await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.Id == request.PilotId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

        if (!pilot.IsActive)
            throw new AppException(AppErrorKind.Validation, $"{pilot.DisplayId} is inactive.");
        if (await db.PilotAwards.AnyAsync(a => a.AwardId == id && a.PilotId == pilot.Id, ct))
            throw new AppException(AppErrorKind.Conflict, $"{pilot.DisplayId} already has this award.");

        var (_, actorName) = await audit.ActorAsync(db, ct);
        var grant = new PilotAward(pilot, award, request.Note, actorName);
        db.PilotAwards.Add(grant);

        notifier.Send(pilot.Id, NotificationType.Award, $"New award: {award.Name}",
            grant.Note is null ? award.Description : $"{award.Description}\n{grant.Note}",
            $"/profile/{pilot.PilotNumber}?tab=awards");

        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Awards, "Award granted", "Pilot", pilot.Id.ToString(), AuditContext.PilotLabel(pilot),
            $"Award: {award.Name}" + (grant.Note is null ? "" : $"\nNote: {grant.Note}")), ct);

        return await HoldersAsync(id, ct);
    }

    public async Task<IReadOnlyList<AwardHolderDto>> RevokeAsync(Guid id, Guid pilotId, CancellationToken ct)
    {
        var grant = await db.PilotAwards
            .Include(a => a.Award)
            .Include(a => a.Pilot).ThenInclude(p => p.Rank)
            .FirstOrDefaultAsync(a => a.AwardId == id && a.PilotId == pilotId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "This pilot does not have this award.");

        if (grant.Award.Kind != AwardKind.Manual)
            throw new AppException(AppErrorKind.Validation, "Tour awards can't be revoked by hand.");

        db.PilotAwards.Remove(grant);
        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Awards, "Award revoked", "Pilot", grant.PilotId.ToString(), AuditContext.PilotLabel(grant.Pilot),
            $"Award: {grant.Award.Name}"), ct);

        return await HoldersAsync(id, ct);
    }

    private IQueryable<AdminAwardDto> Project(IQueryable<Award> awards) =>
        awards.Select(a => new AdminAwardDto(
            a.Id,
            a.Name,
            a.Description,
            a.Kind.ToString(),
            a.ImageUrl,
            a.IsActive,
            db.PilotAwards.Count(p => p.AwardId == a.Id),
            a.CreatedAt));

    private async Task<AdminAwardDto> ToDtoAsync(Guid id, CancellationToken ct) =>
        await Project(db.Awards.AsNoTracking().Where(a => a.Id == id)).FirstAsync(ct);

    private async Task<Award> FindAsync(Guid id, CancellationToken ct) =>
        await db.Awards.FirstOrDefaultAsync(a => a.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Award not found.");

    private async Task EnsureUniqueNameAsync(string name, Guid? exceptId, CancellationToken ct)
    {
        if (await db.Awards.AnyAsync(a => a.Name == name && a.Id != exceptId, ct))
            throw new AppException(AppErrorKind.Conflict, "There is already an award with this name.");
    }
}