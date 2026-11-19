namespace Litoral.Application.Downloads;

public sealed record DownloadDto(
    Guid Id,
    string Title,
    string? Description,
    string Category,
    string? AircraftType,
    string? Version,
    bool IsFile,
    string? FileName,
    long SizeBytes,
    string? ExternalUrl,
    int Downloads,
    DateTime UpdatedAt);

public sealed record AdminDownloadDto(
    Guid Id,
    string Title,
    string? Description,
    string Category,
    string? AircraftType,
    string? Version,
    bool IsFile,
    string? FileName,
    long SizeBytes,
    string? ExternalUrl,
    int Downloads,
    string UpdatedBy,
    DateTime UpdatedAt);

public sealed record DownloadInput(
    string Title,
    string? Description,
    string Category,
    string? AircraftType,
    string? Version,
    string? ExternalUrl);

public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Length);

public sealed record DownloadFile(string Path, string FileName, string ContentType);

public interface IDownloadService
{
    Task<IReadOnlyList<DownloadDto>> ListAsync(CancellationToken ct);
    Task<DownloadFile> OpenAsync(Guid id, CancellationToken ct);
    Task<string> OpenLinkAsync(Guid id, CancellationToken ct);
}

public interface IDownloadAdminService
{
    Task<IReadOnlyList<AdminDownloadDto>> ListAsync(CancellationToken ct);
    Task<AdminDownloadDto> CreateAsync(DownloadInput input, FileUpload? file, CancellationToken ct);
    Task<AdminDownloadDto> UpdateAsync(Guid id, DownloadInput input, FileUpload? file, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface IDownloadStorage
{
    Task<string> SaveAsync(FileUpload upload, CancellationToken ct);
    string? PathOf(string storedName);
    void Delete(string? storedName);
}