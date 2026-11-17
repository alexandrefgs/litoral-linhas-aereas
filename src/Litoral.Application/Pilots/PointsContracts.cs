namespace Litoral.Application.Pilots;

public sealed record PointTransactionDto(
    Guid Id,
    DateTime At,
    string Type,
    string Group,
    string Description,
    int Amount,
    int? BalanceAfter);

public sealed record PointsSummaryDto(
    int Balance,
    int MonthEarned,
    int MonthSpent,
    int TotalEarned,
    int TotalSpent);

public sealed record PointsStatementDto(
    PointsSummaryDto Summary,
    IReadOnlyList<PointTransactionDto> Items,
    int Total,
    int Page,
    int PageSize);

public interface IPointsStatementService
{
    Task<PointsStatementDto> GetAsync(string userId, string? group, int page, int pageSize, CancellationToken ct);
}