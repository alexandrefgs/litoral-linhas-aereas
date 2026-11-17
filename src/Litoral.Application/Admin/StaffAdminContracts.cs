namespace Litoral.Application.Admin;

public sealed record StaffMemberDto(Guid PilotId, int PilotNumber, string DisplayId, string Name, string? PhotoUrl);

public sealed record StaffPositionDto(
    string Code,
    string Name,
    string Team,
    bool SingleHolder,
    bool Editable,
    IReadOnlyList<StaffMemberDto> Holders);

public sealed record StaffOverviewDto(IReadOnlyList<StaffPositionDto> Positions, IReadOnlyList<StaffMemberDto> Candidates);

public sealed record AssignStaffRequest(Guid PilotId);

public interface IStaffAdminService
{
    Task<StaffOverviewDto> GetAsync(string actorUserId, CancellationToken ct);
    Task<StaffOverviewDto> AssignAsync(string actorUserId, string positionCode, Guid pilotId, CancellationToken ct);
    Task<StaffOverviewDto> RemoveAsync(string actorUserId, string positionCode, Guid pilotId, CancellationToken ct);
}