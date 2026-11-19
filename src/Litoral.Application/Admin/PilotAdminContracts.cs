namespace Litoral.Application.Admin;

public sealed record AdminPilotDto(
    Guid Id,
    int PilotNumber,
    string DisplayId,
    string FullName,
    string Email,
    string RankCode,
    string? PhotoUrl,
    string BaseIcao,
    string LocationIcao,
    int FlightMinutes,
    int Points,
    int Flights,
    DateTime? LastFlightAt,
    DateTime JoinedAt,
    bool IsActive,
    IReadOnlyList<string> StaffPositions,
    bool CanDelete,
    bool TwoFactorEnabled);

public interface IPilotAdminService
{
    Task<IReadOnlyList<AdminPilotDto>> ListAsync(CancellationToken ct);
    Task<AdminPilotDto> DeactivateAsync(string actorUserId, Guid pilotId, CancellationToken ct);
    Task<AdminPilotDto> ActivateAsync(string actorUserId, Guid pilotId, CancellationToken ct);
    Task DeleteAsync(string actorUserId, Guid pilotId, CancellationToken ct);
}