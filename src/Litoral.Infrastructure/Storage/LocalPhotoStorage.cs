using Litoral.Application.Admin;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Hosting;

namespace Litoral.Infrastructure.Storage;

public sealed class LocalPhotoStorage(IWebHostEnvironment environment) : IPhotoStorage
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private const string UploadsRoot = "uploads";

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public async Task<string> SaveAsync(string folder, PhotoUpload upload, CancellationToken ct)
    {
        if (upload.Length <= 0)
            throw new AppException(AppErrorKind.Validation, "The photo is empty.");
        if (upload.Length > MaxBytes)
            throw new AppException(AppErrorKind.Validation, "The photo must be 5 MB or smaller.");
        if (!Extensions.TryGetValue(upload.ContentType, out var extension))
            throw new AppException(AppErrorKind.Validation, "Use a JPG, PNG or WEBP photo.");

        var directory = Path.Combine(WebRoot(), UploadsRoot, folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.CreateVersion7():N}{extension}";
        await using (var file = File.Create(Path.Combine(directory, fileName)))
        {
            await upload.Content.CopyToAsync(file, ct);
        }

        return $"/{UploadsRoot}/{folder}/{fileName}";
    }

    public void Delete(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith($"/{UploadsRoot}/", StringComparison.Ordinal))
            return;

        var root = Path.GetFullPath(Path.Combine(WebRoot(), UploadsRoot));
        var path = Path.GetFullPath(Path.Combine(WebRoot(), url.TrimStart('/')));

        if (path.StartsWith(root, StringComparison.Ordinal) && File.Exists(path))
            File.Delete(path);
    }

    private string WebRoot() =>
        string.IsNullOrEmpty(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
}