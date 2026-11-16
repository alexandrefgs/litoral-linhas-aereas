using Litoral.Application.Admin;
using Litoral.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Litoral.Infrastructure.Storage;

public sealed class LocalPhotoStorage(IHostEnvironment environment, IConfiguration configuration) : IPhotoStorage
{
    public const string RequestPath = "/uploads";

    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly string _root = ResolveRoot(environment, configuration);

    public static string ResolveRoot(IHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["Storage:UploadsPath"];
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "..", "..", "uploads")
            : Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured);

        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(full);
        return full;
    }

    public async Task<string> SaveAsync(string folder, PhotoUpload upload, CancellationToken ct)
    {
        if (upload.Length <= 0)
            throw new AppException(AppErrorKind.Validation, "The photo is empty.");
        if (upload.Length > MaxBytes)
            throw new AppException(AppErrorKind.Validation, "The photo must be 5 MB or smaller.");
        if (!Extensions.TryGetValue(upload.ContentType, out var extension))
            throw new AppException(AppErrorKind.Validation, "Use a JPG, PNG or WEBP photo.");

        var directory = Path.Combine(_root, folder);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.CreateVersion7():N}{extension}";
        await using (var file = File.Create(Path.Combine(directory, fileName)))
        {
            await upload.Content.CopyToAsync(file, ct);
        }

        return $"{RequestPath}/{folder}/{fileName}";
    }

    public void Delete(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith($"{RequestPath}/", StringComparison.Ordinal))
            return;

        var path = Path.GetFullPath(Path.Combine(_root, url[(RequestPath.Length + 1)..]));

        if (path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(path))
            File.Delete(path);
    }
}