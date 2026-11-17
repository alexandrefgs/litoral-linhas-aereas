using Litoral.Domain.Common;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Entities;

public sealed class AuditLog : Entity
{
    public const int MaxActionLength = 80;
    public const int MaxNameLength = 150;
    public const int MaxDetailsLength = 4000;

    public DateTime At { get; private set; } = DateTime.UtcNow;
    public AuditCategory Category { get; private set; }
    public string Action { get; private set; } = null!;
    public string? ActorUserId { get; private set; }
    public string ActorName { get; private set; } = null!;
    public string? TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string? TargetName { get; private set; }
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }

    private AuditLog() { }

    public AuditLog(
        AuditCategory category,
        string action,
        string? actorUserId,
        string actorName,
        string? targetType,
        string? targetId,
        string? targetName,
        string? details,
        string? ipAddress)
    {
        Category = category;
        Action = Cut(action, MaxActionLength)!;
        ActorUserId = actorUserId;
        ActorName = Cut(actorName, MaxNameLength)!;
        TargetType = targetType;
        TargetId = targetId;
        TargetName = Cut(targetName, MaxNameLength);
        Details = Cut(details, MaxDetailsLength);
        IpAddress = ipAddress;
    }

    private static string? Cut(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..(max - 1)] + "…";
}