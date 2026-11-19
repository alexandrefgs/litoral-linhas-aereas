using Litoral.Application.Mel;
namespace Litoral.Application.Flights;

public sealed record AdminPirepListItemDto(
    PirepSummaryDto Summary,
    string PilotId,
    string PilotName,
    bool IsDiversion,
    DateTime? ReviewedAt);

public sealed record ReviewRuleDto(Guid Id, string Category, string Description, string Outcome, int Points, string Mode);

public sealed record RejectionPreviewDto(int RejectionNumber, int Penalty, bool DeactivatesAccount);

public sealed record AdminPirepDto(
    PirepDto Pirep,
    string PilotEmail,
    int PilotPoints,
    bool PilotIsActive,
    string? ReviewedByName,
    IReadOnlyList<ReviewRuleDto> ReviewRules,
    RejectionPreviewDto RejectionPreview,
    IReadOnlyList<FlightMelCheckDto> MelChecks);

public sealed record AddReviewItemRequest(Guid RuleId, int Quantity);

public sealed record ReviewDecisionRequest(string? Note);

public interface IPirepReviewService
{
    Task<IReadOnlyList<AdminPirepListItemDto>> ListAsync(string? status, CancellationToken ct);
    Task<AdminPirepDto> GetAsync(Guid id, CancellationToken ct);
    Task<AdminPirepDto> AddItemAsync(Guid id, AddReviewItemRequest request, CancellationToken ct);
    Task<AdminPirepDto> ToggleItemAsync(Guid id, Guid ruleId, CancellationToken ct);
    Task<AdminPirepDto> ApproveAsync(Guid id, string reviewerUserId, ReviewDecisionRequest request, CancellationToken ct);
    Task<AdminPirepDto> RejectAsync(Guid id, string reviewerUserId, ReviewDecisionRequest request, CancellationToken ct);
    Task<bool> TryAutoApproveAsync(Guid id, CancellationToken ct);
    Task<AdminPirepDto> DecideMelAsync(Guid id, Guid checkId, string reviewerUserId, DecideMelRequest request, CancellationToken ct);
}