using Litoral.Application.Common;
using Litoral.Application.Downloads;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Downloads;

public sealed class DownloadAdminService(LitoralDbContext db, IDownloadStorage storage, AuditContext audit) : IDownloadAdminService
{
    public async Task<IReadOnlyList<AdminDownloadDto>> ListAsync(CancellationToken ct) =>
        await Project(db.DownloadItems.AsNoTracking().OrderBy(d => d.Category).ThenBy(d => d.AircraftType).ThenBy(d => d.Title)).ToListAsync(ct);

    public async Task<AdminDownloadDto> CreateAsync(DownloadInput input, FileUpload? file, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        var item = new DownloadItem(input.Title, input.Description, Category(input.Category), input.AircraftType, input.Version, actor);

        if (file is null && string.IsNullOrWhiteSpace(input.ExternalUrl))
            throw new AppException(AppErrorKind.Validation, "Upload a file or enter a link.");

        string? saved = null;
        if (file is not null)
        {
            saved = await storage.SaveAsync(file, ct);
            item.UseFile(saved, file.FileName, file.ContentType, file.Length, actor);
        }
        else
        {
            item.UseLink(input.ExternalUrl!, actor);
        }

        db.DownloadItems.Add(item);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            storage.Delete(saved);
            throw;
        }

        return await ToDtoAsync(item.Id, ct);
    }

    public async Task<AdminDownloadDto> UpdateAsync(Guid id, DownloadInput input, FileUpload? file, CancellationToken ct)
    {
        var item = await FindAsync(id, ct);
        var actor = await ActorAsync(ct);
        item.Update(input.Title, input.Description, Category(input.Category), input.AircraftType, input.Version, actor);

        string? saved = null;
        string? previous = null;

        if (file is not null)
        {
            saved = await storage.SaveAsync(file, ct);
            previous = item.UseFile(saved, file.FileName, file.ContentType, file.Length, actor);
        }
        else if (!string.IsNullOrWhiteSpace(input.ExternalUrl))
        {
            previous = item.UseLink(input.ExternalUrl, actor);
        }
        else if (!item.IsFile)
        {
            throw new AppException(AppErrorKind.Validation, "Upload a file or enter a link.");
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            storage.Delete(saved);
            throw;
        }

        storage.Delete(previous);
        return await ToDtoAsync(item.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var item = await FindAsync(id, ct);
        var stored = item.StoredName;
        db.DownloadItems.Remove(item);
        await db.SaveChangesAsync(ct);
        storage.Delete(stored);
    }

    private IQueryable<AdminDownloadDto> Project(IQueryable<DownloadItem> items) =>
        items.Select(d => new AdminDownloadDto(
            d.Id,
            d.Title,
            d.Description,
            d.Category.ToString(),
            d.AircraftType,
            d.Version,
            d.StoredName != null,
            d.FileName,
            d.SizeBytes,
            d.ExternalUrl,
            d.Downloads,
            d.UpdatedBy,
            d.UpdatedAt));

    private async Task<AdminDownloadDto> ToDtoAsync(Guid id, CancellationToken ct) =>
        await Project(db.DownloadItems.AsNoTracking().Where(d => d.Id == id)).FirstAsync(ct);

    private async Task<DownloadItem> FindAsync(Guid id, CancellationToken ct) =>
        await db.DownloadItems.FirstOrDefaultAsync(d => d.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Download not found.");

    private async Task<string> ActorAsync(CancellationToken ct) =>
        (await audit.ActorAsync(db, ct)).Name;

    private static DownloadCategory Category(string value) =>
        Enum.TryParse<DownloadCategory>(value, true, out var category) && Enum.IsDefined(category)
            ? category
            : throw new AppException(AppErrorKind.Validation, "Choose a category.");
}