using Litoral.Application.Common;
using Litoral.Application.Downloads;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Litoral.Infrastructure.Storage;

public sealed class LocalDownloadStorage(IHostEnvironment environment, IConfiguration configuration) : IDownloadStorage
{
    public const long MaxBytes = 20 * 1024 * 1024;

    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".zip", ".7z", ".rar", ".txt", ".png", ".jpg", ".jpeg"
    };

    private readonly string _root = ResolveRoot(environment, configuration);

    private static string ResolveRoot(IHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["Storage:DownloadsPath"];
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "..", "..", "downloads")
            : Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured);

        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(full);
        return full;
    }

    public async Task<string> SaveAsync(FileUpload upload, CancellationToken ct)
    {
        if (upload.Length <= 0)
            throw new AppException(AppErrorKind.Validation, "The file is empty.");
        if (upload.Length > MaxBytes)
            throw new AppException(AppErrorKind.Validation, "The file must be 20 MB or smaller. Use a link for bigger files.");

        var extension = Path.GetExtension(upload.FileName);
        if (!Extensions.Contains(extension))
            throw new AppException(AppErrorKind.Validation, "Use a PDF, ZIP, 7Z, RAR, TXT, PNG or JPG file.");

        var storedName = $"{Guid.CreateVersion7():N}{extension.ToLowerInvariant()}";
        await using (var file = File.Create(Path.Combine(_root, storedName)))
        {
            await upload.Content.CopyToAsync(file, ct);
        }

        return storedName;
    }

    public string? PathOf(string storedName)
    {
        var path = Path.GetFullPath(Path.Combine(_root, Path.GetFileName(storedName)));
        return path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(path) ? path : null;
    }

    public void Delete(string? storedName)
    {
        if (string.IsNullOrWhiteSpace(storedName)) return;
        if (PathOf(storedName) is { } path)
            File.Delete(path);
    }
}