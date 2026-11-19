namespace Litoral.Application.Pilots;

public sealed record AcarsKeyDto(Guid Id, string Name, string Prefix, DateTime CreatedAt, DateTime? LastUsedAt);

public sealed record CreateAcarsKeyRequest(string Name);

public sealed record CreatedAcarsKeyDto(AcarsKeyDto Key, string Secret);

public interface IAcarsKeyService
{
    Task<IReadOnlyList<AcarsKeyDto>> ListAsync(string userId, CancellationToken ct);
    Task<CreatedAcarsKeyDto> CreateAsync(string userId, CreateAcarsKeyRequest request, CancellationToken ct);
    Task RevokeAsync(string userId, Guid id, CancellationToken ct);
}