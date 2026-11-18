using Litoral.Application.Common;
using Litoral.Application.Notifications;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Notifications;

public sealed class NotificationService(LitoralDbContext db) : INotificationService
{
    private const int ListLimit = 30;

    public async Task<NotificationListDto> ListAsync(string userId, CancellationToken ct)
    {
        var pilotId = await PilotIdAsync(userId, ct);
        var mine = db.Notifications.AsNoTracking().Where(n => n.PilotId == pilotId);

        var unread = await mine.CountAsync(n => n.ReadAt == null, ct);
        var items = await mine
            .OrderByDescending(n => n.CreatedAt)
            .Take(ListLimit)
            .Select(n => new NotificationDto(n.Id, n.Type.ToString(), n.Title, n.Message, n.Link, n.CreatedAt, n.ReadAt != null))
            .ToListAsync(ct);

        return new NotificationListDto(unread, items);
    }

    public async Task<int> UnreadCountAsync(string userId, CancellationToken ct)
    {
        var pilotId = await PilotIdAsync(userId, ct);
        return await db.Notifications.CountAsync(n => n.PilotId == pilotId && n.ReadAt == null, ct);
    }

    public async Task MarkReadAsync(string userId, Guid id, CancellationToken ct)
    {
        var pilotId = await PilotIdAsync(userId, ct);
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.PilotId == pilotId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Notification not found.");

        notification.MarkRead();
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken ct)
    {
        var pilotId = await PilotIdAsync(userId, ct);
        var now = DateTime.UtcNow;
        await db.Notifications
            .Where(n => n.PilotId == pilotId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now), ct);
    }

    private async Task<Guid> PilotIdAsync(string userId, CancellationToken ct) =>
        await db.Pilots.Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct)
        ?? throw new AppException(AppErrorKind.NotFound, "Pilot not found.");
}