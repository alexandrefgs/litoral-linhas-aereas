using Litoral.Application.Common;
using Litoral.Application.Scoring;
using Litoral.Domain.Entities;
using Litoral.Domain.Scoring;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Scoring;

public sealed class ScoringRuleService(LitoralDbContext db) : IScoringRuleService
{
    public async Task<IReadOnlyList<ScoringSectionDto>> TableAsync(CancellationToken ct)
    {
        var rules = await db.ScoringRules.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);

        return ScoringCatalog.Categories
            .Select(c => new ScoringSectionDto(
                c.Category.ToString(),
                c.Title,
                c.Subtitle,
                Sort(rules.Where(r => r.Category == c.Category)).Select(ToDto).ToList()))
            .Where(s => s.Rules.Count > 0)
            .ToList();
    }

    public ScoringCatalogDto Catalog() => new(
        ScoringCatalog.Categories.Select(c => new ScoringCategoryDto(c.Category.ToString(), c.Title, c.Subtitle)).ToList(),
        ScoringCatalog.Checks.Select(c => new ScoringCheckDto(
            c.Check.ToString(),
            c.Label,
            c.Source.ToString(),
            c.Modes.Select(m => m.ToString()).ToList(),
            c.MinLabel,
            c.MaxLabel,
            c.TextLabel)).ToList());

    public async Task<IReadOnlyList<ScoringRuleDto>> ListAsync(CancellationToken ct)
    {
        var rules = await db.ScoringRules.AsNoTracking().ToListAsync(ct);
        return Sort(rules).Select(ToDto).ToList();
    }

    public async Task<ScoringRuleDto> CreateAsync(ScoringRuleRequest request, CancellationToken ct)
    {
        var rule = new ScoringRule(
            Parse<ScoringCategory>(request.Category, "category"),
            Parse<ScoringCheck>(request.Check, "check"),
            request.Description,
            Parse<ScoringOutcome>(request.Outcome, "outcome"),
            request.Points,
            Parse<ScoringMode>(request.Mode, "mode"),
            request.Min,
            request.Max,
            request.Text,
            request.AircraftTypes,
            request.GraceSeconds,
            request.Order);

        if (!request.IsActive) rule.Deactivate();

        db.ScoringRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return ToDto(rule);
    }

    public async Task<ScoringRuleDto> UpdateAsync(Guid id, ScoringRuleRequest request, CancellationToken ct)
    {
        var rule = await FindAsync(id, ct);

        rule.Update(
            Parse<ScoringCategory>(request.Category, "category"),
            Parse<ScoringCheck>(request.Check, "check"),
            request.Description,
            Parse<ScoringOutcome>(request.Outcome, "outcome"),
            request.Points,
            Parse<ScoringMode>(request.Mode, "mode"),
            request.Min,
            request.Max,
            request.Text,
            request.AircraftTypes,
            request.GraceSeconds,
            request.Order);

        if (request.IsActive) rule.Activate();
        else rule.Deactivate();

        await db.SaveChangesAsync(ct);
        return ToDto(rule);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var rule = await FindAsync(id, ct);
        db.ScoringRules.Remove(rule);
        await db.SaveChangesAsync(ct);
    }

    private async Task<ScoringRule> FindAsync(Guid id, CancellationToken ct) =>
        await db.ScoringRules.FirstOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Rule not found.");

    private static IEnumerable<ScoringRule> Sort(IEnumerable<ScoringRule> rules) =>
        rules.OrderBy(r => r.Category).ThenBy(r => r.Order).ThenBy(r => r.Description);

    private static T Parse<T>(string value, string field) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var result) && Enum.IsDefined(result)
            ? result
            : throw new AppException(AppErrorKind.Validation, $"Choose a valid {field}.");

    private static ScoringRuleDto ToDto(ScoringRule r) => new(
        r.Id,
        r.Category.ToString(),
        r.Check.ToString(),
        r.Description,
        r.Outcome.ToString(),
        r.Points,
        r.Mode.ToString(),
        r.Min,
        r.Max,
        r.Text,
        r.AircraftTypes,
        r.GraceSeconds,
        r.Order,
        r.IsActive);
}