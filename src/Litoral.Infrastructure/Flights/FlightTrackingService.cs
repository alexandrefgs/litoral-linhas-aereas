using Litoral.Application.Common;
using Litoral.Application.Flights;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Flights;
using Litoral.Infrastructure.Bookings;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Flights;

public sealed class FlightTrackingService(LitoralDbContext db, BookingSupport support, PirepFiling filing) : IFlightTrackingService
{
    private const int MaxPositionsPerBatch = 500;

    private static readonly TimeSpan LiveTimeout = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyList<LiveFlightDto>> LiveAsync(CancellationToken ct)
    {
        var since = DateTime.UtcNow - LiveTimeout;

        var sessions = await db.FlightSessions.AsNoTracking()
            .Include(s => s.Booking).ThenInclude(b => b.Pilot).ThenInclude(p => p.Rank)
            .Include(s => s.Booking).ThenInclude(b => b.Route).ThenInclude(r => r.DepartureAirport)
            .Include(s => s.Booking).ThenInclude(b => b.Route).ThenInclude(r => r.ArrivalAirport)
            .Include(s => s.Booking).ThenInclude(b => b.Aircraft).ThenInclude(a => a.AircraftType)
            .Where(s => s.Status == FlightSessionStatus.Active && s.Latitude != null && s.LastReportAt >= since)
            .OrderBy(s => s.Booking.Callsign)
            .ToListAsync(ct);

        return sessions.Select(s => new LiveFlightDto(
            s.Id,
            s.Booking.Callsign,
            s.Booking.Pilot.DisplayId,
            $"{s.Booking.Pilot.FirstName} {s.Booking.Pilot.LastName}",
            s.Booking.Aircraft.Registration,
            s.Booking.Aircraft.AircraftType.IcaoCode,
            s.Booking.Route.DepartureAirport.Icao,
            s.Booking.Route.ArrivalAirport.Icao,
            s.Network.ToString(),
            s.Latitude!.Value,
            s.Longitude!.Value,
            s.AltitudeFt ?? 0,
            s.GroundSpeedKt ?? 0,
            s.Heading ?? 0,
            s.Phase,
            s.LastReportAt!.Value)).ToList();
    }

    public async Task<FlightSessionDto?> GetActiveAsync(string userId, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var session = await ActiveSessionAsync(pilot.Id, ct);
        return session is null ? null : ToDto(session);
    }

    public async Task<FlightSessionDto> StartAsync(string userId, StartFlightRequest request, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var network = PirepFiling.ParseNetwork(request.Network);

        var existing = await ActiveSessionAsync(pilot.Id, ct);
        if (existing is not null) return ToDto(existing);

        var booking = await support.FindOpenBookingAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "You don't have an open flight. Book a flight first.");

        var session = new FlightSession(booking, network, request.Simulator, request.Client);
        db.FlightSessions.Add(session);
        await db.SaveChangesAsync(ct);

        return ToDto(session);
    }

    public async Task<int> ReportPositionsAsync(string userId, PositionBatchRequest request, CancellationToken ct)
    {
        if (request.Positions is not { Count: > 0 }) return 0;
        if (request.Positions.Count > MaxPositionsPerBatch)
            throw new AppException(AppErrorKind.Validation, $"Send up to {MaxPositionsPerBatch} positions at a time.");

        var pilot = await support.GetPilotAsync(userId, ct);
        var session = await ActiveSessionAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.Conflict, "You don't have a flight in progress.");

        var added = session.AddPositions(request.Positions.Select(ToSample));
        await db.SaveChangesAsync(ct);
        return added;
    }

    public async Task<PirepDto> CompleteAsync(string userId, CompleteFlightRequest request, CancellationToken ct)
    {
        var pilot = await support.GetPilotAsync(userId, ct);
        var session = await ActiveSessionAsync(pilot.Id, ct)
            ?? throw new AppException(AppErrorKind.Conflict, "You don't have a flight in progress.");

        var booking = await support.BookingQuery().FirstAsync(b => b.Id == session.BookingId, ct);

        var samples = await db.FlightTrackPoints.AsNoTracking()
            .Where(p => p.SessionId == session.Id)
            .OrderBy(p => p.At)
            .ToListAsync(ct);

        var inPounds = string.Equals(request.FuelUnit?.Trim(), "lbs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.FuelUnit?.Trim(), "lb", StringComparison.OrdinalIgnoreCase);
        decimal? Kg(decimal? value) => value is decimal v ? Math.Round(inPounds ? v * PirepFiling.KgPerPound : v, 1) : null;

        var events = (request.Events ?? [])
            .Select(e => new TelemetryEvent(PirepFiling.ParseEventType(e.Type), PirepFiling.Utc(e.At), e.DurationSeconds, e.Value, e.Detail))
            .ToList();

        var report = new PirepReport(
            PirepFiling.Utc(request.BlockOffAt),
            PirepFiling.Utc(request.BlockOnAt),
            PirepFiling.Utc(request.TakeoffAt),
            PirepFiling.Utc(request.LandingAt),
            request.LandingRateFpm,
            Kg(request.FuelUsed),
            Kg(request.FuelAtPushback),
            request.DistanceNm,
            request.FlightRoute,
            request.CruiseAltitudeFt,
            request.OnlinePercent,
            request.Comments);

        var data = new FlightEvaluationData
        {
            AircraftTypeIcao = booking.Aircraft.AircraftType.IcaoCode,
            PlannedArrivalIcao = booking.Route.ArrivalAirport.Icao,
            ArrivalIcao = request.ArrivalIcao,
            Network = session.Network,
            LandingRateFpm = request.LandingRateFpm,
            FuelAtPushbackKg = Kg(request.FuelAtPushback),
            FuelInPounds = request.FuelUnit is null ? null : inPounds,
            OnlinePercent = request.OnlinePercent,
            FirstEngineStarted = request.FirstEngineStarted,
            TaxiInSeconds = request.TaxiInSeconds,
            EnginesRunningAtLog = request.EnginesRunningAtLog,
            LiftoffPitch = request.LiftoffPitch,
            RotationSpeedKt = request.RotationSpeedKt,
            TakeoffFlaps = request.TakeoffFlaps,
            LandingFlaps = request.LandingFlaps,
            FlapsRetractedAtFt = request.FlapsRetractedAtFt,
            LandingConfiguredAtFt = request.LandingConfiguredAtFt,
            DiversionReported = request.DiversionReported,
            IncompleteData = request.IncompleteData,
            Samples = samples.Select(p => p.ToSample()).ToList(),
            Events = events
        };

        var pirep = await filing.FileAsync(booking, session, PirepSource.Acars, session.Network, request.ArrivalIcao, report, data, events, ct);
        return await filing.ToDtoAsync(pirep, ct);
    }

    private Task<FlightSession?> ActiveSessionAsync(Guid pilotId, CancellationToken ct) =>
        db.FlightSessions
            .Include(s => s.Booking)
            .FirstOrDefaultAsync(s => s.PilotId == pilotId && s.Status == FlightSessionStatus.Active, ct);

    private static TelemetrySample ToSample(PositionReport p) => new(
        PirepFiling.Utc(p.At),
        p.Latitude,
        p.Longitude,
        p.AltitudeFt,
        p.AglFt,
        p.IasKt,
        p.GroundSpeedKt,
        ((p.Heading % 360) + 360) % 360,
        p.VerticalSpeedFpm,
        p.Flaps,
        p.GearDown,
        p.OnGround,
        p.Phase);

    private static FlightSessionDto ToDto(FlightSession s) => new(
        s.Id,
        s.BookingId,
        s.Booking.Callsign,
        s.Network.ToString(),
        s.Status.ToString(),
        s.StartedAt,
        s.Latitude,
        s.Longitude,
        s.AltitudeFt,
        s.GroundSpeedKt,
        s.Heading,
        s.Phase,
        s.LastReportAt);
}