using System.Text.Json;
using System.Text.Json.Serialization;
using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Privacy;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Legal;
using Litoral.Infrastructure.Admin;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Privacy;

public sealed class PrivacyService(
    LitoralDbContext db,
    UserManager<ApplicationUser> userManager,
    StaffRoles staffRoles,
    Notifier notifier,
    IPhotoStorage photos,
    IAuditLog auditLog,
    AuditContext auditContext) : IPrivacyService
{
    private const int MaxOpenRequests = 3;

    private static readonly JsonSerializerOptions ExportJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<PrivacyOverview> GetOverviewAsync(string userId, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);

        var requests = await db.PrivacyRequests.AsNoTracking()
            .Where(r => r.PilotId == pilot.Id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return new PrivacyOverview(LegalTerms.CurrentVersion, pilot.AcceptedTermsVersion, pilot.AcceptedTermsAt,
            requests.Select(ToDto).ToList());
    }

    public async Task AcceptTermsAsync(string userId, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);
        if (pilot.AcceptedTermsVersion == LegalTerms.CurrentVersion) return;

        pilot.AcceptTerms(LegalTerms.CurrentVersion);
        await db.SaveChangesAsync(ct);

        await WriteOwnAuditAsync(userId, pilot, "Terms accepted", $"Version {LegalTerms.CurrentVersion}", ct);
    }

    public async Task<PrivacyRequestDto> CreateRequestAsync(string userId, CreatePrivacyRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<PrivacyRequestType>(request.Type, true, out var type) || !Enum.IsDefined(type))
            throw new AppException(AppErrorKind.Validation, "Choose the type of request.");

        var pilot = await FindPilotAsync(userId, ct);

        var open = await db.PrivacyRequests.CountAsync(r => r.PilotId == pilot.Id && r.Status == PrivacyRequestStatus.Open, ct);
        if (open >= MaxOpenRequests)
            throw new AppException(AppErrorKind.Conflict, $"You already have {MaxOpenRequests} open requests. Wait for the answer before sending a new one.");

        var entity = new PrivacyRequest(pilot.Id, AuditContext.PilotLabel(pilot), type, request.Message);
        db.PrivacyRequests.Add(entity);
        await db.SaveChangesAsync(ct);

        await WriteOwnAuditAsync(userId, pilot, "Privacy request created", $"{entity.Type}\n{entity.Message}", ct);
        return ToDto(entity);
    }

    public async Task<DataExport> ExportAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new AppException(AppErrorKind.NotFound, "User not found.");
        var pilot = await FindPilotAsync(userId, ct);
        var pilotId = pilot.Id;
        var pilotTarget = pilotId.ToString();
        var roles = await staffRoles.OfUserAsync(userId, ct);

        var bookings = await db.Bookings.AsNoTracking()
            .Where(b => b.PilotId == pilotId)
            .OrderBy(b => b.CreatedAt)
            .Select(b => new
            {
                b.Callsign,
                b.Type,
                b.Status,
                Departure = b.Route.DepartureAirport.Icao,
                Arrival = b.Route.ArrivalAirport.Icao,
                Aircraft = b.Aircraft.Registration,
                b.PointsCost,
                b.CreatedAt,
                b.StartedAt,
                b.CompletedAt,
                b.ClosedAt
            })
            .ToListAsync(ct);

        var flights = await db.Pireps.AsNoTracking()
            .Where(p => p.PilotId == pilotId)
            .OrderBy(p => p.SubmittedAt)
            .Select(p => new
            {
                p.Callsign,
                p.Source,
                p.Status,
                p.Network,
                p.DepartureIcao,
                p.PlannedArrivalIcao,
                p.ArrivalIcao,
                p.Registration,
                p.AircraftTypeIcao,
                p.BlockOffAt,
                p.TakeoffAt,
                p.LandingAt,
                p.BlockOnAt,
                p.BlockMinutes,
                p.AirMinutes,
                p.DistanceNm,
                p.LandingRateFpm,
                p.FuelUsedKg,
                p.FlightRoute,
                p.CruiseAltitudeFt,
                p.OnlinePercent,
                p.Comments,
                p.ScorePoints,
                p.Conformity,
                p.SubmittedAt,
                p.ReviewedAt,
                p.ReviewNote
            })
            .ToListAsync(ct);

        var points = await db.PointTransactions.AsNoTracking()
            .Where(t => t.PilotId == pilotId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new { t.CreatedAt, t.Type, t.Amount, t.Description })
            .ToListAsync(ct);

        var awards = await db.MonthlyAwards.AsNoTracking()
            .Where(a => a.PilotId == pilotId)
            .OrderBy(a => a.Year).ThenBy(a => a.Month)
            .Select(a => new { a.Year, a.Month, a.Category, a.Position, a.Value, a.Points, a.AwardedAt })
            .ToListAsync(ct);
        
        var medals = await db.PilotAwards.AsNoTracking()
            .Where(a => a.PilotId == pilotId)
            .OrderBy(a => a.GrantedAt)
            .Select(a => new { a.Award.Name, a.Award.Kind, a.Note, a.GrantedAt })
            .ToListAsync(ct);

        var notifications = await db.Notifications.AsNoTracking()
            .Where(n => n.PilotId == pilotId)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new { n.CreatedAt, n.Type, n.Title, n.Message, n.ReadAt })
            .ToListAsync(ct);

        var requests = await db.PrivacyRequests.AsNoTracking()
            .Where(r => r.PilotId == pilotId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new { r.CreatedAt, r.Type, r.Message, r.Status, r.DueAt, r.Response, r.ResolvedAt })
            .ToListAsync(ct);

        var activity = await db.AuditLogs.AsNoTracking()
            .Where(l => l.ActorUserId == userId || (l.TargetType == "Pilot" && l.TargetId == pilotTarget))
            .OrderBy(l => l.At)
            .Select(l => new
            {
                l.At,
                l.Category,
                l.Action,
                By = l.ActorUserId == userId ? "You" : l.ActorName == AuditEntry.SystemActor || l.ActorName == "Visitor" ? l.ActorName : "Staff",
                l.TargetName,
                l.Details,
                l.IpAddress
            })
            .ToListAsync(ct);

        var export = new
        {
            ExportedAt = DateTime.UtcNow,
            Notice = "Copy of the personal data that Litoral Linhas Aéreas keeps about you (LGPD art. 18). The position track of each flight is available on the flight page of the site.",
            Account = new
            {
                user.Email,
                JoinedAt = pilot.CreatedAt,
                TwoFactorEnabled = user.TwoFactorEnabled,
                pilot.AcceptedTermsVersion,
                pilot.AcceptedTermsAt,
                StaffPositions = roles
            },
            Profile = new
            {
                pilot.PilotNumber,
                pilot.DisplayId,
                pilot.FirstName,
                pilot.LastName,
                pilot.NameFormat,
                Rank = pilot.Rank.Name,
                pilot.BaseIcao,
                pilot.CurrentLocationIcao,
                pilot.BirthDate,
                pilot.CountryCode,
                pilot.State,
                pilot.City,
                pilot.VatsimId,
                pilot.IvaoId,
                pilot.DiscordId,
                pilot.SimBriefUsername,
                SimAircraft = pilot.SimAircraftList,
                pilot.PhotoUrl,
                pilot.CoverUrl,
                pilot.TotalFlightMinutes,
                pilot.Points,
                pilot.IsActive
            },
            Bookings = bookings,
            Flights = flights,
            PointTransactions = points,
            MonthlyAwards = awards,
            Awards = medals,
            Notifications = notifications,
            PrivacyRequests = requests,
            ActivityLog = activity
        };

        await WriteOwnAuditAsync(userId, pilot, "Data exported", null, ct);

        var content = JsonSerializer.SerializeToUtf8Bytes(export, ExportJson);
        return new DataExport($"litoral-data-{pilot.DisplayId}-{DateTime.UtcNow:yyyyMMdd}.json", content);
    }

    public async Task RemoveOptionalDataAsync(string userId, CancellationToken ct)
    {
        var pilot = await FindPilotAsync(userId, ct);

        pilot.UpdatePersonal(pilot.FirstName, pilot.LastName, null, null, null, null);
        pilot.SetNetworkIds(null, null, null);
        pilot.SetSimBriefUsername(null);
        pilot.SetSimAircraft([]);
        var photo = pilot.SetPhoto(null);
        var cover = pilot.SetCover(null);

        await db.SaveChangesAsync(ct);
        photos.Delete(photo);
        photos.Delete(cover);

        await WriteOwnAuditAsync(userId, pilot, "Optional data removed",
            "Birth date, country, state, city, VATSIM, IVAO and Discord IDs, SimBrief username, simulator aircraft, photo and cover", ct);
    }

    public async Task<IReadOnlyList<AdminPrivacyRequestDto>> ListAsync(bool openOnly, CancellationToken ct)
    {
        var query = db.PrivacyRequests.AsNoTracking();
        if (openOnly)
            query = query.Where(r => r.Status == PrivacyRequestStatus.Open);

        var requests = await query
            .OrderBy(r => r.Status == PrivacyRequestStatus.Open ? 0 : 1)
            .ThenBy(r => r.Status == PrivacyRequestStatus.Open ? r.DueAt : DateTime.MaxValue)
            .ThenByDescending(r => r.ResolvedAt)
            .Take(200)
            .ToListAsync(ct);

        return await ToAdminDtosAsync(requests, ct);
    }

    public async Task<AdminPrivacyRequestDto> ResolveAsync(Guid requestId, ResolvePrivacyRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<PrivacyRequestStatus>(request.Status, true, out var status) || status == PrivacyRequestStatus.Open)
            throw new AppException(AppErrorKind.Validation, "Choose completed or rejected.");

        var entity = await db.PrivacyRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Request not found.");

        var (_, actorName) = await auditContext.ActorAsync(db, ct);
        entity.Resolve(status, request.Response, actorName);

        if (entity.PilotId is Guid pilotId)
            notifier.Send(pilotId, NotificationType.Privacy,
                status == PrivacyRequestStatus.Completed ? "Privacy request completed" : "Privacy request rejected",
                $"Your {Describe(entity.Type)} request was answered. Open Settings to read the response.",
                "/profile?tab=settings");

        await db.SaveChangesAsync(ct);

        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Privacy,
            status == PrivacyRequestStatus.Completed ? "Privacy request completed" : "Privacy request rejected",
            "Pilot", entity.PilotId?.ToString(), entity.PilotLabel, $"{entity.Type}\n{entity.Response}"), ct);

        return (await ToAdminDtosAsync([entity], ct))[0];
    }

    private async Task<List<AdminPrivacyRequestDto>> ToAdminDtosAsync(IReadOnlyList<PrivacyRequest> requests, CancellationToken ct)
    {
        var pilotIds = requests.Where(r => r.PilotId is not null).Select(r => r.PilotId!.Value).Distinct().ToList();

        var pilots = await db.Pilots.AsNoTracking()
            .Where(p => pilotIds.Contains(p.Id))
            .Select(p => new { p.Id, p.PilotNumber, p.UserId })
            .ToListAsync(ct);

        var userIds = pilots.Select(p => p.UserId).ToList();
        var emails = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(ct);

        return requests.Select(r =>
        {
            var pilot = pilots.FirstOrDefault(p => p.Id == r.PilotId);
            var email = pilot is null ? null : emails.FirstOrDefault(e => e.Id == pilot.UserId)?.Email;
            return new AdminPrivacyRequestDto(r.Id, pilot?.PilotNumber, r.PilotLabel, email, r.Type.ToString(), r.Message,
                r.Status.ToString(), r.CreatedAt, r.DueAt, r.Response, r.ResolvedAt, r.ResolvedBy);
        }).ToList();
    }

    private async Task WriteOwnAuditAsync(string userId, Pilot pilot, string action, string? details, CancellationToken ct)
    {
        var label = AuditContext.PilotLabel(pilot);
        await auditLog.WriteAsync(new AuditEntry(AuditCategory.Privacy, action, "Pilot", pilot.Id.ToString(), label, details, userId, label), ct);
    }

    private async Task<Pilot> FindPilotAsync(string userId, CancellationToken ct) =>
        await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.UserId == userId, ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");

    private static PrivacyRequestDto ToDto(PrivacyRequest r) =>
        new(r.Id, r.Type.ToString(), r.Message, r.Status.ToString(), r.CreatedAt, r.DueAt, r.Response, r.ResolvedAt);

    private static string Describe(PrivacyRequestType type) => type switch
    {
        PrivacyRequestType.Access => "data access",
        PrivacyRequestType.Correction => "data correction",
        PrivacyRequestType.Deletion => "data deletion",
        PrivacyRequestType.Portability => "data portability",
        PrivacyRequestType.ConsentWithdrawal => "consent withdrawal",
        _ => "privacy"
    };
}