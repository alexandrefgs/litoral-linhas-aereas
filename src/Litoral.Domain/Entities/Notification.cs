using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class Notification : Entity
{
    public const int MaxTitleLength = 120;
    public const int MaxMessageLength = 500;
    public const int MaxLinkLength = 200;

    public Guid PilotId { get; private set; }
    public Pilot Pilot { get; private set; } = null!;
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string? Link { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; private set; }

    public bool IsRead => ReadAt is not null;

    private Notification() { }

    public Notification(Guid pilotId, NotificationType type, string title, string message, string? link)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Notification title is required.");

        PilotId = pilotId;
        Type = type;
        Title = Cut(title.Trim(), MaxTitleLength);
        Message = Cut((message ?? "").Trim(), MaxMessageLength);
        Link = string.IsNullOrWhiteSpace(link) ? null : Cut(link.Trim(), MaxLinkLength);
    }

    public void MarkRead() => ReadAt ??= DateTime.UtcNow;

    private static string Cut(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
}