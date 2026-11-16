namespace Litoral.Application.Scoring;

public sealed record ScoringRuleDto(
    Guid Id,
    string Category,
    string Check,
    string Description,
    string Outcome,
    int Points,
    string Mode,
    decimal? Min,
    decimal? Max,
    string? Text,
    string? AircraftTypes,
    int GraceSeconds,
    int Order,
    bool IsActive);

public sealed record ScoringRuleRequest(
    string Category,
    string Check,
    string Description,
    string Outcome,
    int Points,
    string Mode,
    decimal? Min,
    decimal? Max,
    string? Text,
    string? AircraftTypes,
    int GraceSeconds,
    int Order,
    bool IsActive);

public sealed record ScoringCategoryDto(string Category, string Title, string? Subtitle);

public sealed record ScoringCheckDto(
    string Check,
    string Label,
    string Source,
    IReadOnlyList<string> Modes,
    string? MinLabel,
    string? MaxLabel,
    string? TextLabel);

public sealed record ScoringCatalogDto(IReadOnlyList<ScoringCategoryDto> Categories, IReadOnlyList<ScoringCheckDto> Checks);

public sealed record ScoringSectionDto(string Category, string Title, string? Subtitle, IReadOnlyList<ScoringRuleDto> Rules);

public interface IScoringRuleService
{
    Task<IReadOnlyList<ScoringSectionDto>> TableAsync(CancellationToken ct);
    ScoringCatalogDto Catalog();
    Task<IReadOnlyList<ScoringRuleDto>> ListAsync(CancellationToken ct);
    Task<ScoringRuleDto> CreateAsync(ScoringRuleRequest request, CancellationToken ct);
    Task<ScoringRuleDto> UpdateAsync(Guid id, ScoringRuleRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}