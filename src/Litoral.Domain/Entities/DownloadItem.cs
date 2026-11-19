using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class DownloadItem : Entity
{
    public const int MaxTitleLength = 120;
    public const int MaxDescriptionLength = 500;
    public const int MaxVersionLength = 30;
    public const int MaxUrlLength = 500;
    public const int MaxFileNameLength = 200;

    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public DownloadCategory Category { get; private set; }
    public string? AircraftType { get; private set; }
    public string? Version { get; private set; }
    public string? ExternalUrl { get; private set; }
    public string? StoredName { get; private set; }
    public string? FileName { get; private set; }
    public string? ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public int Downloads { get; private set; }
    public string UpdatedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public bool IsFile => StoredName is not null;

    private DownloadItem() { }

    public DownloadItem(string title, string? description, DownloadCategory category, string? aircraftType, string? version, string updatedBy)
    {
        Update(title, description, category, aircraftType, version, updatedBy);
    }

    public void Update(string title, string? description, DownloadCategory category, string? aircraftType, string? version, string updatedBy)
    {
        var t = title?.Trim() ?? "";
        var d = Clean(description);
        var v = Clean(version);
        var type = Clean(aircraftType)?.ToUpperInvariant();

        if (t.Length == 0)
            throw new DomainException("The title is required.");
        if (t.Length > MaxTitleLength)
            throw new DomainException($"The title must have up to {MaxTitleLength} characters.");
        if (d?.Length > MaxDescriptionLength)
            throw new DomainException($"The description must have up to {MaxDescriptionLength} characters.");
        if (v?.Length > MaxVersionLength)
            throw new DomainException($"The version must have up to {MaxVersionLength} characters.");
        if (type?.Length > 4)
            throw new DomainException("Use the ICAO code of the aircraft type, e.g. B738.");
        if (!Enum.IsDefined(category))
            throw new DomainException("Invalid category.");

        Title = t;
        Description = d;
        Category = category;
        AircraftType = type;
        Version = v;
        Touch(updatedBy);
    }

    public string? UseLink(string url, string updatedBy)
    {
        var u = url?.Trim() ?? "";
        if (u.Length > MaxUrlLength)
            throw new DomainException($"The link must have up to {MaxUrlLength} characters.");
        if (!Uri.TryCreate(u, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new DomainException("Enter a valid https:// link.");

        var previous = StoredName;
        ExternalUrl = uri.ToString();
        StoredName = null;
        FileName = null;
        ContentType = null;
        SizeBytes = 0;
        Touch(updatedBy);
        return previous;
    }

    public string? UseFile(string storedName, string fileName, string contentType, long sizeBytes, string updatedBy)
    {
        var name = Path.GetFileName(fileName ?? "").Trim();
        if (name.Length == 0)
            name = "file";
        if (name.Length > MaxFileNameLength)
            name = name[^MaxFileNameLength..];

        var previous = StoredName;
        StoredName = storedName;
        FileName = name;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        ExternalUrl = null;
        Touch(updatedBy);
        return previous;
    }

    public void CountDownload() => Downloads++;

    private void Touch(string updatedBy)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}