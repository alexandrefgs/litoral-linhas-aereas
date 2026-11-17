using System.Globalization;
using Litoral.Application.Common;
using Litoral.Application.Statistics;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Statistics;

public sealed class StatisticsService(LitoralDbContext db) : IStatisticsService
{
    private const int TopLimit = 10;
    private const int MonthsInChart = 12;
    private const int DailyTimelineMaxDays = 62;
    private const int MaxReportDays = 731;

    private sealed record FlightRow(
        Guid PilotId,
        DateTime SubmittedAt,
        int BlockMinutes,
        int? DistanceNm,
        string DepartureIcao,
        string ArrivalIcao,
        string AircraftTypeIcao,
        FlightNetwork Network,
        int? LandingRateFpm,
        PirepStatus Status,
        PirepSource Source,
        BookingType BookingType,
        DateTime? ReviewedAt,
        int Conformity);

    public async Task<StatisticsDto> GetAsync(string userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var pilots = await PilotsAsync(ct);
        var me = pilots.Values.FirstOrDefault(p => p.UserId == userId);
        var flights = await FlightsAsync(db.Pireps.Where(p => p.Status == PirepStatus.Approved), ct);
        var month = flights.Where(f => f.SubmittedAt >= monthStart).ToList();

        var aircraft = await db.Aircraft.CountAsync(a => a.Status != AircraftStatus.Inactive, ct);
        var airports = await db.Airports.CountAsync(ct);
        var typeNames = await db.AircraftTypes.AsNoTracking()
            .GroupBy(t => t.IcaoCode)
            .Select(g => new { g.Key, Name = g.Min(t => t.Name) })
            .ToDictionaryAsync(g => g.Key, g => g.Name!, ct);

        var active = pilots.Values.Where(p => p.IsActive).ToList();
        var byPilot = flights.GroupBy(f => f.PilotId).ToDictionary(g => g.Key, g => g.Count());
        var monthByPilot = month.GroupBy(f => f.PilotId)
            .ToDictionary(g => g.Key, g => (Minutes: g.Sum(f => f.BlockMinutes), Flights: g.Count()));

        var hoursRanking = Rank(active.Select(p => (p, p.TotalFlightMinutes, byPilot.GetValueOrDefault(p.Id))), me);
        var flightsRanking = Rank(active.Select(p => (p, byPilot.GetValueOrDefault(p.Id), p.TotalFlightMinutes)), me);
        var pointsRanking = Rank(active.Select(p => (p, p.Points, p.TotalFlightMinutes)), me);
        var monthHours = Rank(active.Select(p => (p, monthByPilot.GetValueOrDefault(p.Id).Minutes, monthByPilot.GetValueOrDefault(p.Id).Flights)), me);
        var monthFlights = Rank(active.Select(p => (p, monthByPilot.GetValueOrDefault(p.Id).Flights, monthByPilot.GetValueOrDefault(p.Id).Minutes)), me);

        var myFlights = me is null ? new List<FlightRow>() : flights.Where(f => f.PilotId == me.Id).ToList();
        var myMonth = me is null ? default : monthByPilot.GetValueOrDefault(me.Id);

        return new StatisticsDto(
            new VaTotalsDto(
                active.Count,
                flights.Count,
                flights.Sum(f => f.BlockMinutes),
                flights.Sum(f => (long)(f.DistanceNm ?? 0)),
                AverageLanding(flights),
                aircraft,
                airports),
            new MonthSummaryDto(now.Year, now.Month, month.Count, month.Sum(f => f.BlockMinutes), month.Select(f => f.PilotId).Distinct().Count()),
            Months(flights, now),
            monthHours.Take(TopLimit).ToList(),
            monthFlights.Take(TopLimit).ToList(),
            await LastAwardsAsync(pilots, ct),
            MonthlyAward.BonusByPosition,
            hoursRanking.Take(TopLimit).ToList(),
            flightsRanking.Take(TopLimit).ToList(),
            pointsRanking.Take(TopLimit).ToList(),
            TopRoutes(flights),
            TopAirports(flights),
            flights.GroupBy(f => f.AircraftTypeIcao)
                .Select(g => new CountItemDto(g.Key, typeNames.GetValueOrDefault(g.Key, g.Key), g.Count(), g.Sum(f => f.BlockMinutes)))
                .OrderByDescending(i => i.Flights)
                .ToList(),
            Networks(flights),
            new MyStatsDto(
                myFlights.Count,
                me?.TotalFlightMinutes ?? 0,
                me?.Points ?? 0,
                AverageLanding(myFlights),
                myMonth.Flights,
                myMonth.Minutes,
                PositionOf(hoursRanking),
                PositionOf(flightsRanking),
                PositionOf(pointsRanking),
                PositionOf(monthHours),
                PositionOf(monthFlights)));
    }

    public async Task<ReportDto> ReportAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        from = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        to = DateTime.SpecifyKind(to, DateTimeKind.Utc);

        if (to <= from)
            throw new AppException(AppErrorKind.Validation, "The end date must be after the start date.");
        if ((to - from).TotalDays > MaxReportDays)
            throw new AppException(AppErrorKind.Validation, "The period can be at most 2 years.");

        var now = DateTime.UtcNow;
        var pilots = await PilotsAsync(ct);

        var submitted = await FlightsAsync(db.Pireps.Where(p => p.SubmittedAt >= from && p.SubmittedAt < to), ct);
        var flown = submitted.Where(f => f.Status != PirepStatus.Rejected).ToList();
        var reviewed = await FlightsAsync(db.Pireps.Where(p => p.ReviewedAt != null && p.ReviewedAt >= from && p.ReviewedAt < to), ct);
        var approved = reviewed.Count(f => f.Status == PirepStatus.Approved);
        var rejected = reviewed.Count(f => f.Status == PirepStatus.Rejected);

        var pending = await db.Pireps.AsNoTracking()
            .Where(p => p.Status == PirepStatus.Pending)
            .Select(p => p.SubmittedAt)
            .ToListAsync(ct);

        var lastFlights = await db.Pireps.AsNoTracking()
            .GroupBy(p => p.PilotId)
            .Select(g => new { PilotId = g.Key, Last = g.Max(p => p.SubmittedAt) })
            .ToDictionaryAsync(g => g.PilotId, g => g.Last, ct);

        var active = pilots.Values.Where(p => p.IsActive).ToList();
        var idleSince = now.AddDays(-30);

        var minutesByPilot = flown.GroupBy(f => f.PilotId).ToDictionary(g => g.Key, g => (Minutes: g.Sum(f => f.BlockMinutes), Flights: g.Count()));

        return new ReportDto(
            from,
            to,
            new ReportFlightsDto(
                flown.Count,
                flown.Sum(f => f.BlockMinutes),
                flown.Sum(f => (long)(f.DistanceNm ?? 0)),
                AverageLanding(flown),
                flown.Select(f => f.PilotId).Distinct().Count()),
            new ReportPirepsDto(
                pending.Count,
                pending.Count == 0 ? null : (int)(now - pending.Min()).TotalHours,
                approved,
                rejected,
                approved + rejected == 0 ? 0 : (int)Math.Round(rejected * 100.0 / (approved + rejected)),
                reviewed.Count == 0 ? null : (int)Math.Round(reviewed.Average(f => f.Conformity)),
                reviewed.Count == 0 ? null : (int)Math.Round(reviewed.Average(f => (f.ReviewedAt!.Value - f.SubmittedAt).TotalHours)),
                submitted.Count(f => f.Source == PirepSource.Manual)),
            new ReportPilotsDto(
                pilots.Count,
                active.Count,
                pilots.Count - active.Count,
                pilots.Values.Count(p => p.CreatedAt >= from && p.CreatedAt < to),
                active.Count(p => lastFlights.TryGetValue(p.Id, out var last) && last < idleSince),
                active.Count(p => !lastFlights.ContainsKey(p.Id))),
            Timeline(flown, from, to),
            Rank(pilots.Values.Select(p => (p, minutesByPilot.GetValueOrDefault(p.Id).Minutes, minutesByPilot.GetValueOrDefault(p.Id).Flights)), null)
                .Take(TopLimit)
                .ToList(),
            Networks(flown),
            flown.GroupBy(f => f.BookingType)
                .Select(g => new CountItemDto(g.Key.ToString(), g.Key.ToString(), g.Count(), g.Sum(f => f.BlockMinutes)))
                .OrderByDescending(i => i.Flights)
                .ToList(),
            TopRoutes(flown));
    }

    private async Task<Dictionary<Guid, Pilot>> PilotsAsync(CancellationToken ct) =>
        await db.Pilots.AsNoTracking().Include(p => p.Rank).ToDictionaryAsync(p => p.Id, ct);

    private static async Task<List<FlightRow>> FlightsAsync(IQueryable<Pirep> query, CancellationToken ct) =>
        await query.AsNoTracking()
            .Select(p => new FlightRow(
                p.PilotId,
                p.SubmittedAt,
                p.BlockMinutes,
                p.DistanceNm,
                p.DepartureIcao,
                p.ArrivalIcao,
                p.AircraftTypeIcao,
                p.Network,
                p.LandingRateFpm,
                p.Status,
                p.Source,
                p.Booking.Type,
                p.ReviewedAt,
                p.Conformity))
            .ToListAsync(ct);

    private async Task<MonthAwardsDto?> LastAwardsAsync(Dictionary<Guid, Pilot> pilots, CancellationToken ct)
    {
        var last = await db.MonthlyAwards.AsNoTracking()
            .OrderByDescending(a => a.Year).ThenByDescending(a => a.Month)
            .Select(a => new { a.Year, a.Month })
            .FirstOrDefaultAsync(ct);

        if (last is null) return null;

        var awards = await db.MonthlyAwards.AsNoTracking()
            .Where(a => a.Year == last.Year && a.Month == last.Month)
            .OrderBy(a => a.Category).ThenBy(a => a.Position)
            .ToListAsync(ct);

        return new MonthAwardsDto(last.Year, last.Month, awards
            .Select(a => new AwardWinnerDto(
                a.Category.ToString(),
                a.Position,
                a.PilotNumber,
                a.PilotId is Guid id && pilots.TryGetValue(id, out var pilot) ? pilot.PublicName : a.PilotName,
                a.PilotId is Guid photoId && pilots.TryGetValue(photoId, out var withPhoto) ? withPhoto.PhotoUrl : null,
                a.Value,
                a.Points))
            .ToList());
    }

    private static List<RankedPilotDto> Rank(IEnumerable<(Pilot Pilot, int Value, int Tie)> rows, Pilot? me) =>
        rows.Where(r => r.Value > 0)
            .OrderByDescending(r => r.Value)
            .ThenByDescending(r => r.Tie)
            .ThenBy(r => r.Pilot.PilotNumber)
            .Select((r, i) => new RankedPilotDto(
                i + 1,
                r.Pilot.PilotNumber,
                r.Pilot.DisplayId,
                r.Pilot.PublicName,
                r.Pilot.Rank.Code,
                r.Pilot.PhotoUrl,
                r.Value,
                me is not null && r.Pilot.Id == me.Id))
            .ToList();

    private static int? PositionOf(List<RankedPilotDto> ranking) =>
        ranking.FirstOrDefault(r => r.IsMe)?.Position;

    private static int? AverageLanding(IReadOnlyCollection<FlightRow> flights)
    {
        var rates = flights.Where(f => f.LandingRateFpm is not null).Select(f => Math.Abs(f.LandingRateFpm!.Value)).ToList();
        return rates.Count == 0 ? null : (int)Math.Round(rates.Average());
    }

    private static List<PeriodPointDto> Months(IReadOnlyCollection<FlightRow> flights, DateTime now)
    {
        var first = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(MonthsInChart - 1));
        return Enumerable.Range(0, MonthsInChart)
            .Select(i => first.AddMonths(i))
            .Select(start =>
            {
                var end = start.AddMonths(1);
                var inMonth = flights.Where(f => f.SubmittedAt >= start && f.SubmittedAt < end).ToList();
                return new PeriodPointDto(start.ToString("MMM yy", CultureInfo.InvariantCulture), start, inMonth.Count, inMonth.Sum(f => f.BlockMinutes));
            })
            .ToList();
    }

    private static List<PeriodPointDto> Timeline(IReadOnlyCollection<FlightRow> flights, DateTime from, DateTime to)
    {
        var daily = (to - from).TotalDays <= DailyTimelineMaxDays;
        var points = new List<PeriodPointDto>();
        var start = daily ? from.Date : new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (start < to)
        {
            var end = daily ? start.AddDays(1) : start.AddMonths(1);
            var inPeriod = flights.Where(f => f.SubmittedAt >= start && f.SubmittedAt < end).ToList();
            points.Add(new PeriodPointDto(
                start.ToString(daily ? "dd MMM" : "MMM yy", CultureInfo.InvariantCulture),
                DateTime.SpecifyKind(start, DateTimeKind.Utc),
                inPeriod.Count,
                inPeriod.Sum(f => f.BlockMinutes)));
            start = end;
        }

        return points;
    }

    private static List<CountItemDto> TopRoutes(IReadOnlyCollection<FlightRow> flights) =>
        flights.GroupBy(f => (f.DepartureIcao, f.ArrivalIcao))
            .Select(g => new CountItemDto($"{g.Key.DepartureIcao}-{g.Key.ArrivalIcao}", $"{g.Key.DepartureIcao} → {g.Key.ArrivalIcao}", g.Count(), g.Sum(f => f.BlockMinutes)))
            .OrderByDescending(i => i.Flights)
            .ThenBy(i => i.Key)
            .Take(TopLimit)
            .ToList();

    private static List<CountItemDto> TopAirports(IReadOnlyCollection<FlightRow> flights) =>
        flights.SelectMany(f => new[] { (Icao: f.DepartureIcao, f.BlockMinutes), (Icao: f.ArrivalIcao, f.BlockMinutes) })
            .GroupBy(x => x.Icao)
            .Select(g => new CountItemDto(g.Key, g.Key, g.Count(), g.Sum(x => x.BlockMinutes)))
            .OrderByDescending(i => i.Flights)
            .ThenBy(i => i.Key)
            .Take(TopLimit)
            .ToList();

    private static List<CountItemDto> Networks(IReadOnlyCollection<FlightRow> flights) =>
        flights.GroupBy(f => f.Network)
            .Select(g => new CountItemDto(g.Key.ToString(), g.Key switch
            {
                FlightNetwork.Vatsim => "VATSIM",
                FlightNetwork.Ivao => "IVAO",
                _ => "Offline"
            }, g.Count(), g.Sum(f => f.BlockMinutes)))
            .OrderByDescending(i => i.Flights)
            .ToList();
}