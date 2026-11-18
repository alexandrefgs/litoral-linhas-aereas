using Litoral.Domain.Enums;

namespace Litoral.Application.Admin;

public sealed record AuditEntry(
    AuditCategory Category,
    string Action,
    string? TargetType = null,
    string? TargetId = null,
    string? TargetName = null,
    string? Details = null,
    string? ActorUserId = null,
    string? ActorName = null)
{
    public const string SystemActor = "System";

    public AuditEntry AsSystem() => this with { ActorUserId = null, ActorName = SystemActor };
}

public sealed record AuditLogDto(
    Guid Id,
    DateTime At,
    string Category,
    string Severity,
    string Action,
    string ActorName,
    string? TargetType,
    string? TargetName,
    string? Details,
    string? IpAddress);

public sealed record AuditLogPageDto(IReadOnlyList<AuditLogDto> Items, int Total, int Page, int PageSize);

public interface IAuditLog
{
    Task WriteAsync(AuditEntry entry, CancellationToken ct);
}

public interface IAuditLogQuery
{
    Task<AuditLogPageDto> ListAsync(string? category, string? search, bool criticalOnly, int page, int pageSize, CancellationToken ct);
}