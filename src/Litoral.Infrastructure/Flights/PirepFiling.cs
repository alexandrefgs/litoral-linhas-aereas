using Litoral.Application.Common;
using Litoral.Application.Flights;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Flights;
using Litoral.Domain.Scoring;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Flights;

public sealed class PirepFiling(LitoralDbContext db)
{
    public const decimal KgPerPound = 0.45359237m;
    private const int MaxTrackPoints = 1500;
    private static readonly TimeSpan ScheduledMaintenanceDuration = TimeSpan.FromDays(2);

    public async Task<Pirep> FileAsync(
        Booking booking,
        FlightSession? session,
        PirepSource source,
        FlightNetwork network,
        string arrivalIcao,
        PirepReport report,
        FlightEvaluationData data,
        IReadOnlyList<TelemetryEvent> events,
        CancellationToken ct)
    {
        if (await db.Pireps.AnyAsync(p => p.BookingId == booking.Id, ct))
            throw new AppException(AppErrorKind.Conflict, "A PIREP was already filed for this flight.");

        arrivalIcao = (arrivalIcao ?? "").Trim().ToUpperInvariant();
        if (arrivalIcao.Length == 0)
            throw new AppException(AppErrorKind.Validation, "Enter the arrival airport.");

        var arrival = await db.Airports.FirstOrDefaultAsync(a => a.Icao == arrivalIcao, ct);
        var landedAt = arrival ?? booking.Route.ArrivalAirport;

        if (booking.Status == BookingStatus.Booked) booking.Start();

        var pirep = new Pirep(booking, session, source, network, arrivalIcao, report);
        pirep.RecordEvents(events);

        var rules = await db.ScoringRules.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);
        var evaluation = PirepEvaluator.Evaluate(
            data with
            {
                BlockMinutes = pirep.BlockMinutes,
                ArrivalIcao = pirep.ArrivalIcao,
                PlannedArrivalIcao = pirep.PlannedArrivalIcao,
                AircraftTypeIcao = pirep.AircraftTypeIcao,
                Network = network,
                IncompleteData = data.IncompleteData || arrival is null
            },
            rules);

        pirep.ApplyEvaluation(evaluation);

        booking.Complete(pirep.BlockMinutes, landedAt);
        if (booking.Aircraft.IsMaintenanceDue)
            booking.Aircraft.SendToMaintenance(
                $"Scheduled maintenance ({booking.Aircraft.AircraftType.MaintenanceIntervalHours}h interval)",
                DateTime.UtcNow.Add(ScheduledMaintenanceDuration));
        booking.Pilot.AddFlightTime(pirep.BlockMinutes);
        booking.Pilot.MoveTo(landedAt.Icao);

        var roster = await db.MonthlyRosters
            .Include(r => r.Legs)
            .FirstOrDefaultAsync(r => r.Legs.Any(l => l.BookingId == booking.Id), ct);
        roster?.CompleteLeg(booking.Id);

        session?.Complete();

        db.Pireps.Add(pirep);
        await db.SaveChangesAsync(ct);
        return pirep;
    }

    public IQueryable<Pirep> PirepQuery() =>
        db.Pireps.Include(p => p.Pilot).ThenInclude(p => p.Rank);

    public async Task<PirepDto> ToDtoAsync(Pirep pirep, CancellationToken ct)
    {
        var track = new List<TrackPointDto>();

        if (pirep.FlightSessionId is Guid sessionId)
        {
            var points = await db.FlightTrackPoints.AsNoTracking()
                .Where(p => p.SessionId == sessionId)
                .OrderBy(p => p.At)
                .Select(p => new TrackPointDto(p.At, p.Latitude, p.Longitude, p.AltitudeFt, p.GroundSpeedKt))
                .ToListAsync(ct);

            var step = Math.Max(1, (int)Math.Ceiling(points.Count / (double)MaxTrackPoints));
            track = points.Where((_, i) => i % step == 0 || i == points.Count - 1).ToList();
        }

        return new PirepDto(
            ToSummary(pirep),
            pirep.Pilot.DisplayId,
            $"{pirep.Pilot.FirstName} {pirep.Pilot.LastName}",
            pirep.BlockOffAt,
            pirep.TakeoffAt,
            pirep.LandingAt,
            pirep.BlockOnAt,
            pirep.AirMinutes,
            pirep.FuelUsedKg,
            pirep.FuelAtPushbackKg,
            pirep.DistanceNm,
            pirep.FlightRoute,
            pirep.CruiseAltitudeFt,
            pirep.OnlinePercent,
            pirep.Comments,
            pirep.ReviewedAt,
            pirep.ReviewNote,
            pirep.Items
                .OrderBy(i => i.Outcome == ScoringOutcome.Reject ? 0 : 1)
                .ThenBy(i => i.Category)
                .Select(i => new PirepScoreItemDto(i.RuleId, i.Category.ToString(), i.Check.ToString(), i.Description, i.Outcome.ToString(), i.Points, i.Quantity, i.IsManual, i.IsVoided))                .ToList(),
            pirep.Events
                .OrderBy(e => e.At)
                .Select(e => new PirepEventDto(e.Type.ToString(), e.At, e.DurationSeconds, e.Value, e.Detail))
                .ToList(),
            track);
    }

    public static PirepSummaryDto ToSummary(Pirep p) => new(
        p.Id,
        p.Callsign,
        p.DepartureIcao,
        p.ArrivalIcao,
        p.PlannedArrivalIcao,
        p.Registration,
        p.AircraftTypeIcao,
        p.Source.ToString(),
        p.Status.ToString(),
        p.Network.ToString(),
        p.BlockMinutes,
        p.LandingRateFpm,
        p.ScorePoints,
        p.HasRejection,
        p.Conformity,
        p.SubmittedAt);

    public static FlightNetwork ParseNetwork(string? value) =>
        Enum.TryParse<FlightNetwork>(value, true, out var network) && Enum.IsDefined(network)
            ? network
            : throw new AppException(AppErrorKind.Validation, "Choose a valid network: Offline, VATSIM or IVAO.");

    public static FlightEventType ParseEventType(string? value) =>
        Enum.TryParse<FlightEventType>(value, true, out var type) && Enum.IsDefined(type) ? type : FlightEventType.Other;

    public static DateTime Utc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime? Utc(DateTime? value) => value is DateTime v ? Utc(v) : null;
}