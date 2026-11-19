using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Flights;
using Litoral.Application.Mel;
using Litoral.Infrastructure.Mel;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Scoring;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Events;
using Litoral.Infrastructure.Tours;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Flights;

public sealed class PirepReviewService(LitoralDbContext db, PirepFiling filing, Notifier notifier, IAuditLog auditLog, TourProgressTracker tours, EventBonus events) : IPirepReviewService
{
    public const string SystemReviewer = "SYSTEM";
    public const int AutoApprovalConformity = 98;

    private const int ListLimit = 200;
    private const int DefaultCrewDutyPoints = 10;
    private const int DefaultSpecialDomesticPoints = 300;
    private const int DefaultSpecialInternationalPoints = 500;

    public async Task<IReadOnlyList<AdminPirepListItemDto>> ListAsync(string? status, CancellationToken ct)
    {
        var query = db.Pireps.AsNoTracking().Include(p => p.Pilot).ThenInclude(p => p.Rank).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PirepStatus>(status, true, out var parsed))
                throw new AppException(AppErrorKind.Validation, "Choose a valid status.");
            query = query.Where(p => p.Status == parsed);
        }

        var pireps = await query
            .OrderBy(p => p.Status == PirepStatus.Pending ? 0 : 1)
            .ThenByDescending(p => p.SubmittedAt)
            .Take(ListLimit)
            .ToListAsync(ct);

        return pireps.Select(p => new AdminPirepListItemDto(
            PirepFiling.ToSummary(p),
            p.Pilot.DisplayId,
            $"{p.Pilot.FirstName} {p.Pilot.LastName}",
            p.IsDiversion,
            p.ReviewedAt)).ToList();
    }

    public async Task<AdminPirepDto> GetAsync(Guid id, CancellationToken ct) =>
        await ToDtoAsync(await FindAsync(id, ct), ct);

    public async Task<AdminPirepDto> AddItemAsync(Guid id, AddReviewItemRequest request, CancellationToken ct)
    {
        var pirep = await FindAsync(id, ct);
        var rule = await db.ScoringRules.FirstOrDefaultAsync(r => r.Id == request.RuleId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Rule not found.");

        pirep.AddReviewItem(rule, request.Quantity);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(pirep, ct);
    }

    public async Task<AdminPirepDto> ToggleItemAsync(Guid id, Guid ruleId, CancellationToken ct)
    {
        var pirep = await FindAsync(id, ct);
        pirep.RemoveItem(ruleId);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(pirep, ct);
    }

    public async Task<AdminPirepDto> ApproveAsync(Guid id, string reviewerUserId, ReviewDecisionRequest request, CancellationToken ct)
    {
        var pirep = await FindAsync(id, ct);
        if (await db.FlightMelChecks.AnyAsync(c => c.PirepId == pirep.Id && c.Result == MelCheckResult.Pending, ct))
            throw new AppException(AppErrorKind.Validation, "Decide the MEL items of this flight before approving it.");
        await ApplyApprovalAsync(pirep, reviewerUserId, request.Note, ct);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(pirep, ct);
    }

    public async Task<bool> TryAutoApproveAsync(Guid id, CancellationToken ct)
    {
        var pirep = await FindAsync(id, ct);

        if (!pirep.IsPending
            || pirep.Source != PirepSource.Acars
            || pirep.HasRejection
            || pirep.Conformity < AutoApprovalConformity
            || !pirep.Pilot.IsActive
            || await db.FlightMelChecks.AnyAsync(c => c.PirepId == pirep.Id && c.Result == MelCheckResult.Pending, ct))
            return false;

        await ApplyApprovalAsync(pirep, SystemReviewer, null, ct);
        await db.SaveChangesAsync(ct);

        await auditLog.WriteAsync(new AuditEntry(
            AuditCategory.Pireps,
            "PIREP auto-approved",
            "PIREP",
            pirep.Id.ToString(),
            pirep.Callsign,
            $"Pilot: {AuditContext.PilotLabel(pirep.Pilot)}\nRoute: {pirep.DepartureIcao} → {pirep.ArrivalIcao}\nConformity: {pirep.Conformity}%\nPoints: {pirep.ScorePoints}").AsSystem(), ct);

        return true;
    }

    private async Task ApplyApprovalAsync(Pirep pirep, string reviewerUserId, string? note, CancellationToken ct)
    {
        var pilot = pirep.Pilot;

        pirep.Approve(reviewerUserId, note);

        if (pirep.ScorePoints != 0)
            pilot.AddPoints(pirep.ScorePoints, PointTransactionType.FlightScore, $"Flight {pirep.Callsign} {pirep.DepartureIcao}-{pirep.ArrivalIcao} approved");

        await PayCrewAsync(pirep, ct);
        await PayRosterBonusAsync(pirep, ct);
        await PaySpecialFlightAsync(pirep, ct);
        await tours.OnApprovedAsync(pirep, ct);
        await events.OnApprovedAsync(pirep, ct);

        var auto = reviewerUserId == SystemReviewer;
        notifier.Send(pilot.Id, NotificationType.PirepApproved,
            $"{pirep.Callsign} approved{(auto ? " automatically" : "")}",
            $"{pirep.DepartureIcao} → {pirep.ArrivalIcao} · {Signed(pirep.ScorePoints)} points{(pirep.ReviewNote is { } n ? $" · \"{n}\"" : "")}",
            $"/flights/{pirep.Id}");

        await UpdateRankAsync(pilot, ct);
    }

    public async Task<AdminPirepDto> DecideMelAsync(Guid id, Guid checkId, string reviewerUserId, DecideMelRequest request, CancellationToken ct)
    {
        var pirep = await FindAsync(id, ct);
        if (!pirep.IsPending)
            throw new AppException(AppErrorKind.Conflict, "This PIREP was already reviewed.");

        var check = await db.FlightMelChecks
            .Include(c => c.AircraftMelItem).ThenInclude(a => a.MelItem)
            .FirstOrDefaultAsync(c => c.Id == checkId && c.PirepId == pirep.Id, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "MEL item not found in this PIREP.");

        var reviewer = await db.Pilots.Where(p => p.UserId == reviewerUserId).Select(p => p.FirstName + " " + p.LastName).FirstOrDefaultAsync(ct) ?? "Staff";
        check.Decide(request.Complied, reviewer);

        var rule = await db.ScoringRules.AsNoTracking().FirstOrDefaultAsync(r => r.IsActive
            && r.Check == (request.Complied ? ScoringCheck.MelCompliance : ScoringCheck.MelViolation), ct);
        if (rule is not null)
            pirep.AddMelItem(rule, $"{MelSupport.Label(check.AircraftMelItem.MelItem)}: {(request.Complied ? "procedure followed" : "procedure not followed")}");

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(pirep, ct);
    }

    public async Task<AdminPirepDto> RejectAsync(Guid id, string reviewerUserId, ReviewDecisionRequest request, CancellationToken ct)
    {
        var pirep = await FindAsync(id, ct);
        var pilot = pirep.Pilot;
        var preview = await RejectionPreviewAsync(pirep, ct);

        pirep.Reject(reviewerUserId, request.Note);

        if (preview.Penalty != 0)
            pilot.AddPoints(-Math.Abs(preview.Penalty), PointTransactionType.Penalty,
                $"Flight {pirep.Callsign} rejected ({Ordinal(preview.RejectionNumber)} rejection this month)");

        if (preview.DeactivatesAccount)
            pilot.Deactivate();

        notifier.Send(pilot.Id, NotificationType.PirepRejected,
            $"{pirep.Callsign} rejected",
            $"{Ordinal(preview.RejectionNumber)} rejection this month · {Signed(-Math.Abs(preview.Penalty))} points · \"{pirep.ReviewNote}\"",
            $"/flights/{pirep.Id}");

        if (preview.DeactivatesAccount)
            notifier.Send(pilot.Id, NotificationType.AccountDeactivated,
                "Account deactivated",
                "Your account was deactivated after too many rejected flights this month. Contact the staff to have it reactivated.");

        await UpdateRankAsync(pilot, ct);

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(pirep, ct);
    }

    private async Task<Pirep> FindAsync(Guid id, CancellationToken ct) =>
        await filing.PirepQuery()
            .Include(p => p.Booking).ThenInclude(b => b.CrewPilot)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "PIREP not found.");

    private async Task PayCrewAsync(Pirep pirep, CancellationToken ct)
    {
        var crew = pirep.Booking.CrewPilot;
        if (crew is null) return;

        var rule = await db.ScoringRules.AsNoTracking()
            .FirstOrDefaultAsync(r => r.IsActive && r.Check == ScoringCheck.CrewDuty, ct);
        if (rule is null && await db.ScoringRules.AnyAsync(r => r.Check == ScoringCheck.CrewDuty, ct)) return;

        var points = rule?.Points ?? DefaultCrewDutyPoints;
        if (points != 0)
        {
            crew.AddPoints(points, PointTransactionType.CrewDuty, $"Rostered as crew on {pirep.Callsign}");
            notifier.Send(crew.Id, NotificationType.Bonus, $"+{points} crew points", $"You were rostered as crew on {pirep.Callsign}.", "/points");
        }
    }

    private async Task PaySpecialFlightAsync(Pirep pirep, CancellationToken ct)
    {
        if (pirep.Booking.Type != BookingType.Special) return;

        var leg = await db.SpecialFlightLegs.AsNoTracking()
            .Include(l => l.Route).ThenInclude(r => r.DepartureAirport)
            .Include(l => l.Route).ThenInclude(r => r.ArrivalAirport)
            .FirstOrDefaultAsync(l => l.BookingId == pirep.BookingId, ct);
        if (leg is null) return;

        var international = leg.Route.DepartureAirport.Country != leg.Route.ArrivalAirport.Country;
        var check = international ? ScoringCheck.SpecialFlightInternational : ScoringCheck.SpecialFlightDomestic;

        var rule = await db.ScoringRules.AsNoTracking().FirstOrDefaultAsync(r => r.IsActive && r.Check == check, ct);
        if (rule is null && await db.ScoringRules.AnyAsync(r => r.Check == check, ct)) return;

        var points = rule?.Points ?? (international ? DefaultSpecialInternationalPoints : DefaultSpecialDomesticPoints);
        if (points != 0)
            pirep.Pilot.AddPoints(points, PointTransactionType.SpecialSchedule,
                $"Special flight {pirep.Callsign} ({(international ? "international" : "domestic")} leg)");
    }

    private async Task PayRosterBonusAsync(Pirep pirep, CancellationToken ct)
    {
        var roster = await db.MonthlyRosters
            .Include(r => r.Legs)
            .FirstOrDefaultAsync(r => r.Legs.Any(l => l.BookingId == pirep.BookingId), ct);

        if (roster is null || !roster.CanPayBonus) return;

        var bookingIds = roster.Legs.Where(l => l.BookingId != null).Select(l => l.BookingId!.Value).ToList();
        var statuses = await db.Pireps
            .Where(p => bookingIds.Contains(p.BookingId) && p.Id != pirep.Id)
            .Select(p => p.Status)
            .ToListAsync(ct);

        var allApproved = statuses.Count == bookingIds.Count - 1 && statuses.All(s => s == PirepStatus.Approved);
        if (!allApproved) return;

        roster.MarkBonusPaid();
        pirep.Pilot.AddPoints(roster.Bonus, PointTransactionType.MonthlyBonus, $"Monthly roster {roster.Month:D2}/{roster.Year} completed");
        notifier.Send(pirep.Pilot.Id, NotificationType.Bonus, $"+{roster.Bonus} monthly roster bonus", $"All flights of your {roster.Month:D2}/{roster.Year} roster were approved.", "/points");
    }

    private async Task UpdateRankAsync(Pilot pilot, CancellationToken ct)
    {
        var airport = await db.Airports.FirstOrDefaultAsync(a => a.Icao == pilot.CurrentLocationIcao, ct);
        if (airport is null) return;

        var ranks = await db.Ranks.ToListAsync(ct);
        var previousLevel = pilot.Rank.Level;
        if (pilot.UpdateRank(ranks, airport))
            notifier.RankChanged(pilot, previousLevel);
    }

    private async Task<RejectionPreviewDto> RejectionPreviewAsync(Pirep pirep, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var previous = await db.Pireps.CountAsync(p =>
            p.PilotId == pirep.PilotId && p.Id != pirep.Id &&
            p.Status == PirepStatus.Rejected && p.ReviewedAt >= monthStart, ct);
        var number = previous + 1;

        var rules = await db.ScoringRules.AsNoTracking()
            .Where(r => r.IsActive && (r.Check == ScoringCheck.MonthlyRejection || r.Check == ScoringCheck.RejectionDeactivation))
            .ToListAsync(ct);

        var penalty = rules
            .Where(r => r.Check == ScoringCheck.MonthlyRejection && r.Min <= number)
            .OrderByDescending(r => r.Min)
            .Select(r => r.Points)
            .FirstOrDefault();

        var deactivates = rules.Any(r => r.Check == ScoringCheck.RejectionDeactivation && r.Min <= number);

        return new RejectionPreviewDto(number, penalty, deactivates);
    }

    private async Task<AdminPirepDto> ToDtoAsync(Pirep pirep, CancellationToken ct)
    {
        var email = await db.Users.Where(u => u.Id == pirep.Pilot.UserId).Select(u => u.Email).FirstOrDefaultAsync(ct);

        string? reviewer = pirep.ReviewedBy == SystemReviewer ? "System (auto-approved)" : null;
        if (reviewer is null && pirep.ReviewedBy is { } reviewerId)
        {
            reviewer = await db.Pilots.Where(p => p.UserId == reviewerId)
                .Select(p => p.FirstName + " " + p.LastName)
                .FirstOrDefaultAsync(ct);
        }

        var reviewRules = await db.ScoringRules.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);

        return new AdminPirepDto(
            await filing.ToDtoAsync(pirep, ct),
            email ?? "",
            pirep.Pilot.Points,
            pirep.Pilot.IsActive,
            reviewer,
            reviewRules
                .Where(r => ScoringCatalog.Get(r.Check).Source == ScoringSource.Review)
                .OrderBy(r => r.Category).ThenBy(r => r.Order)
                .Select(r => new ReviewRuleDto(r.Id, ScoringCatalog.Get(r.Category).Title, r.Description, r.Outcome.ToString(), r.Points, r.Mode.ToString()))
                .ToList(),
            pirep.IsPending
                ? await RejectionPreviewAsync(pirep, ct)
                : new RejectionPreviewDto(0, 0, false),
            await MelChecksAsync(pirep.Id, ct));
    }

    private async Task<IReadOnlyList<FlightMelCheckDto>> MelChecksAsync(Guid pirepId, CancellationToken ct)
    {
        var checks = await db.FlightMelChecks.AsNoTracking()
            .Include(c => c.AircraftMelItem).ThenInclude(a => a.MelItem)
            .Where(c => c.PirepId == pirepId)
            .ToListAsync(ct);

        return checks
            .OrderBy(c => c.Result == MelCheckResult.Pending ? 0 : 1)
            .ThenBy(c => c.AircraftMelItem.MelItem.Code)
            .Select(c => new FlightMelCheckDto(
                c.Id,
                c.AircraftMelItem.MelItem.Code,
                c.AircraftMelItem.MelItem.Title,
                c.AircraftMelItem.MelItem.Restriction,
                c.AircraftMelItem.MelItem.Procedure,
                c.AircraftMelItem.MelItem.Verification.ToString(),
                c.Result.ToString(),
                c.Detail))
            .ToList();
    }

    private static string Signed(int points) => points > 0 ? $"+{points}" : points.ToString();

    private static string Ordinal(int n) => n switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{n}th"
    };
}