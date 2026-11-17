using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Domain.Staff;
using Litoral.Infrastructure.Admin;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Pilots;

public sealed class PilotEraser(
    LitoralDbContext db,
    UserManager<ApplicationUser> userManager,
    StaffRoles staffRoles,
    IPhotoStorage photos,
    IAuditLog auditLog)
{
    public async Task<string?> BlockReasonAsync(Pilot pilot, bool self, CancellationToken ct)
    {
        var subject = self ? "You" : pilot.DisplayId;
        var roles = await staffRoles.OfUserAsync(pilot.UserId, ct);

        if (roles.Contains(StaffPositions.CeoFounder))
            return "The CEO & Founder account can't be deleted.";
        if (roles.Count > 0)
            return self
                ? "You are part of the staff. Ask the Chairman to remove your staff positions first."
                : $"{pilot.DisplayId} is part of the staff. Remove the staff positions first.";
        if (await db.Bookings.AnyAsync(b => b.PilotId == pilot.Id && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.InProgress), ct))
            return $"{subject} {(self ? "have" : "has")} an open flight. Cancel it before deleting the account.";
        if (await db.SpecialFlights.AnyAsync(f => f.PilotId == pilot.Id && f.Status == SpecialFlightStatus.Taken, ct))
            return $"{subject} {(self ? "have" : "has")} a special flight in progress. Finish or cancel it before deleting the account.";

        return null;
    }

    public async Task<HashSet<Guid>> BlockedIdsAsync(IReadOnlyCollection<Guid> pilotIds, CancellationToken ct)
    {
        var openFlights = await db.Bookings.AsNoTracking()
            .Where(b => pilotIds.Contains(b.PilotId) && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.InProgress))
            .Select(b => b.PilotId)
            .ToListAsync(ct);

        var specialFlights = await db.SpecialFlights.AsNoTracking()
            .Where(f => f.PilotId != null && pilotIds.Contains(f.PilotId.Value) && f.Status == SpecialFlightStatus.Taken)
            .Select(f => f.PilotId!.Value)
            .ToListAsync(ct);

        return openFlights.Concat(specialFlights).ToHashSet();
    }

    public async Task DeleteAsync(Pilot pilot, bool self, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(pilot.UserId)
            ?? throw new AppException(AppErrorKind.NotFound, "User not found.");

        var pilotId = pilot.Id;
        var photoUrl = pilot.PhotoUrl;
        var coverUrl = pilot.CoverUrl;

        var flights = await db.Pireps.CountAsync(p => p.PilotId == pilotId, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await auditLog.WriteAsync(new AuditEntry(
            self ? AuditCategory.Account : AuditCategory.Pilots,
            "Account deleted",
            "Pilot",
            pilotId.ToString(),
            AuditContext.PilotLabel(pilot),
            $"Deleted by {(self ? "the pilot" : "the staff")}\nEmail: {user.Email}\nFlights: {flights}\nHours: {pilot.TotalFlightMinutes / 60}h{pilot.TotalFlightMinutes % 60:D2}\nPoints: {pilot.Points}\nJoined: {pilot.CreatedAt:yyyy-MM-dd}"), ct);

        await db.Bookings.Where(b => b.CrewPilotId == pilotId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.CrewPilotId, (Guid?)null), ct);
        await db.SpecialFlights.Where(f => f.PilotId == pilotId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.PilotId, (Guid?)null), ct);
        await db.SpecialFlightLegs.Where(l => l.Booking != null && l.Booking.PilotId == pilotId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.BookingId, (Guid?)null), ct);
        await db.MonthlyRosters.Where(r => r.PilotId == pilotId).ExecuteDeleteAsync(ct);
        await db.Pireps.Where(p => p.PilotId == pilotId).ExecuteDeleteAsync(ct);
        await db.FlightSessions.Where(s => s.PilotId == pilotId).ExecuteDeleteAsync(ct);
        await db.Bookings.Where(b => b.PilotId == pilotId).ExecuteDeleteAsync(ct);

        db.Entry(pilot).State = EntityState.Detached;
        var deleted = await userManager.DeleteAsync(user);
        if (!deleted.Succeeded)
            throw new AppException(AppErrorKind.Conflict, string.Join(" ", deleted.Errors.Select(e => e.Description)));

        await transaction.CommitAsync(ct);

        photos.Delete(photoUrl);
        photos.Delete(coverUrl);
    }
}