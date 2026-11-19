using System.ComponentModel.DataAnnotations;

namespace Litoral.Application.Applications;

public sealed record RegistrationStatusDto(bool Open, IReadOnlyList<string> Simulators, IReadOnlyList<string> AddOns);

public sealed record AdminApplicationDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string HubIcao,
    string? VatsimId,
    string? IvaoId,
    string Simulator,
    IReadOnlyList<string> AddOns,
    string Motivation,
    DateTime CreatedAt,
    bool VatsimInUse,
    bool IvaoInUse);

public sealed record AdminApplicationsDto(bool RegistrationOpen, IReadOnlyList<AdminApplicationDto> Applications);

public sealed record RejectApplicationRequest([Required, MaxLength(500)] string Reason);

public sealed record SetRegistrationRequest(bool Open);

public interface IApplicationService
{
    Task<RegistrationStatusDto> GetRegistrationStatusAsync(CancellationToken ct);
    Task<AdminApplicationsDto> ListAsync(CancellationToken ct);
    Task ApproveAsync(Guid applicationId, CancellationToken ct);
    Task RejectAsync(Guid applicationId, RejectApplicationRequest request, CancellationToken ct);
    Task SetRegistrationOpenAsync(bool open, CancellationToken ct);
}