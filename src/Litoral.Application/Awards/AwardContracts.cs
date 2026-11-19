using Litoral.Application.Admin;

namespace Litoral.Application.Awards;

public sealed record PilotAwardDto(
    Guid AwardId,
    string Name,
    string Description,
    string Kind,
    string? ImageUrl,
    string? Note,
    DateTime GrantedAt);

public sealed record AdminAwardDto(
    Guid Id,
    string Name,
    string Description,
    string Kind,
    string? ImageUrl,
    bool IsActive,
    int Holders,
    DateTime CreatedAt);

public sealed record AwardHolderDto(
    Guid PilotId,
    int PilotNumber,
    string DisplayId,
    string Name,
    string? PhotoUrl,
    string? Note,
    string GrantedBy,
    DateTime GrantedAt);

public sealed record AwardPilotOptionDto(Guid PilotId, int PilotNumber, string DisplayId, string Name, string? PhotoUrl);

public sealed record AwardRequest(string Name, string Description, bool IsActive);

public sealed record GrantAwardRequest(Guid PilotId, string? Note);

public interface IAwardService
{
    Task<IReadOnlyList<PilotAwardDto>> ForPilotAsync(int pilotNumber, CancellationToken ct);
}

public interface IAwardAdminService
{
    Task<IReadOnlyList<AdminAwardDto>> ListAsync(CancellationToken ct);
    Task<AdminAwardDto> CreateAsync(AwardRequest request, CancellationToken ct);
    Task<AdminAwardDto> UpdateAsync(Guid id, AwardRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<AdminAwardDto> UploadImageAsync(Guid id, PhotoUpload upload, CancellationToken ct);
    Task<AdminAwardDto> RemoveImageAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<AwardHolderDto>> HoldersAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<AwardPilotOptionDto>> PilotsAsync(CancellationToken ct);
    Task<IReadOnlyList<AwardHolderDto>> GrantAsync(Guid id, GrantAwardRequest request, CancellationToken ct);
    Task<IReadOnlyList<AwardHolderDto>> RevokeAsync(Guid id, Guid pilotId, CancellationToken ct);
}