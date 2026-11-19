using System.ComponentModel.DataAnnotations;

namespace Litoral.Application.Privacy;

public sealed record PrivacyRequestDto(
    Guid Id,
    string Type,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime DueAt,
    string? Response,
    DateTime? ResolvedAt);

public sealed record AdminPrivacyRequestDto(
    Guid Id,
    int? PilotNumber,
    string PilotLabel,
    string? Email,
    string Type,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime DueAt,
    string? Response,
    DateTime? ResolvedAt,
    string? ResolvedBy);

public sealed record PrivacyOverview(
    string CurrentTermsVersion,
    string? AcceptedTermsVersion,
    DateTime? AcceptedTermsAt,
    IReadOnlyList<PrivacyRequestDto> Requests);

public sealed record CreatePrivacyRequest(
    [Required, MaxLength(30)] string Type,
    [Required, MaxLength(2000)] string Message);

public sealed record ResolvePrivacyRequest(
    [Required, MaxLength(20)] string Status,
    [Required, MaxLength(2000)] string Response);

public sealed record DataExport(string FileName, byte[] Content);

public interface IPrivacyService
{
    Task<PrivacyOverview> GetOverviewAsync(string userId, CancellationToken ct);
    Task AcceptTermsAsync(string userId, CancellationToken ct);
    Task<PrivacyRequestDto> CreateRequestAsync(string userId, CreatePrivacyRequest request, CancellationToken ct);
    Task<DataExport> ExportAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<AdminPrivacyRequestDto>> ListAsync(bool openOnly, CancellationToken ct);
    Task<AdminPrivacyRequestDto> ResolveAsync(Guid requestId, ResolvePrivacyRequest request, CancellationToken ct);
}