namespace Litoral.Application.Statistics;

public sealed record RankedPilotDto(
    int Position,
    int PilotNumber,
    string DisplayId,
    string Name,
    string RankCode,
    string? PhotoUrl,
    int Value,
    bool IsMe);

public sealed record CountItemDto(string Key, string Label, int Flights, int Minutes);

public sealed record PeriodPointDto(string Label, DateTime Start, int Flights, int Minutes);

public sealed record AwardWinnerDto(string Category, int Position, int PilotNumber, string Name, string? PhotoUrl, int Value, int Points);

public sealed record MonthAwardsDto(int Year, int Month, IReadOnlyList<AwardWinnerDto> Winners);

public sealed record VaTotalsDto(int Pilots, int Flights, int Minutes, long DistanceNm, int? AverageLandingRate, int Aircraft, int Airports);

public sealed record MonthSummaryDto(int Year, int Month, int Flights, int Minutes, int ActivePilots);

public sealed record MyStatsDto(
    int Flights,
    int Minutes,
    int Points,
    int? AverageLandingRate,
    int MonthFlights,
    int MonthMinutes,
    int? HoursPosition,
    int? FlightsPosition,
    int? PointsPosition,
    int? MonthHoursPosition,
    int? MonthFlightsPosition);

public sealed record StatisticsDto(
    VaTotalsDto Totals,
    MonthSummaryDto Month,
    IReadOnlyList<PeriodPointDto> Monthly,
    IReadOnlyList<RankedPilotDto> MonthHours,
    IReadOnlyList<RankedPilotDto> MonthFlights,
    MonthAwardsDto? LastAwards,
    IReadOnlyList<int> Bonus,
    IReadOnlyList<RankedPilotDto> TopHours,
    IReadOnlyList<RankedPilotDto> TopFlights,
    IReadOnlyList<RankedPilotDto> TopPoints,
    IReadOnlyList<CountItemDto> TopRoutes,
    IReadOnlyList<CountItemDto> TopAirports,
    IReadOnlyList<CountItemDto> Fleet,
    IReadOnlyList<CountItemDto> Networks,
    MyStatsDto Me);

public sealed record ReportFlightsDto(int Flights, int Minutes, long DistanceNm, int? AverageLandingRate, int ActivePilots);

public sealed record ReportPirepsDto(
    int PendingNow,
    int? OldestPendingHours,
    int Approved,
    int Rejected,
    int RejectionPercent,
    int? AverageConformity,
    int? AverageReviewHours,
    int Manual);

public sealed record ReportPilotsDto(int Total, int Active, int Inactive, int NewSignups, int Idle30Days, int NeverFlown);

public sealed record ReportDto(
    DateTime From,
    DateTime To,
    ReportFlightsDto Flights,
    ReportPirepsDto Pireps,
    ReportPilotsDto Pilots,
    IReadOnlyList<PeriodPointDto> Timeline,
    IReadOnlyList<RankedPilotDto> TopPilots,
    IReadOnlyList<CountItemDto> Networks,
    IReadOnlyList<CountItemDto> BookingTypes,
    IReadOnlyList<CountItemDto> TopRoutes);

public interface IStatisticsService
{
    Task<StatisticsDto> GetAsync(string userId, CancellationToken ct);
    Task<ReportDto> ReportAsync(DateTime from, DateTime to, CancellationToken ct);
}