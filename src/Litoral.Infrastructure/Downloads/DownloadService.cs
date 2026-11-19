using Litoral.Application.Common;
using Litoral.Application.Downloads;
using Litoral.Domain.Entities;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Downloads;

public sealed class DownloadService(LitoralDbContext db, IDownloadStorage storage) : IDownloadService
{
    public async Task<IReadOnlyList<DownloadDto>> ListAsync(CancellationToken ct) =>
        await db.DownloadItems
            .AsNoTracking()
            .OrderBy(d => d.Category).ThenBy(d => d.AircraftType).ThenBy(d => d.Title)
            .Select(d => new DownloadDto(
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
                d.UpdatedAt))
            .ToListAsync(ct);

    public async Task<DownloadFile> OpenAsync(Guid id, CancellationToken ct)
    {
        var item = await FindAsync(id, ct);

        if (item.StoredName is null)
            throw new AppException(AppErrorKind.Validation, "This download is an external link.");

        var path = storage.PathOf(item.StoredName)
            ?? throw new AppException(AppErrorKind.NotFound, "The file is not available. Let the staff know.");

        item.CountDownload();
        await db.SaveChangesAsync(ct);

        return new DownloadFile(path, item.FileName ?? "file", item.ContentType ?? "application/octet-stream");
    }

    public async Task<string> OpenLinkAsync(Guid id, CancellationToken ct)
    {
        var item = await FindAsync(id, ct);

        if (item.ExternalUrl is null)
            throw new AppException(AppErrorKind.Validation, "This download is a file.");

        item.CountDownload();
        await db.SaveChangesAsync(ct);
        return item.ExternalUrl;
    }

    private async Task<DownloadItem> FindAsync(Guid id, CancellationToken ct) =>
        await db.DownloadItems.FirstOrDefaultAsync(d => d.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Download not found.");
}