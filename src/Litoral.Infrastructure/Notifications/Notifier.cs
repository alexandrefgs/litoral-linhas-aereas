using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Persistence;

namespace Litoral.Infrastructure.Notifications;

public sealed class Notifier(LitoralDbContext db)
{
    public void Send(Guid pilotId, NotificationType type, string title, string message, string? link = null) =>
        db.Notifications.Add(new Notification(pilotId, type, title, message, link));

    public void SendMany(IEnumerable<Guid> pilotIds, NotificationType type, string title, string message, string? link = null) =>
        db.Notifications.AddRange(pilotIds.Distinct().Select(id => new Notification(id, type, title, message, link)));

    public void RankChanged(Pilot pilot, int previousLevel)
    {
        var promoted = pilot.Rank.Level > previousLevel;
        Send(pilot.Id,
            promoted ? NotificationType.Promotion : NotificationType.Demotion,
            promoted ? $"Promoted to {pilot.Rank.Name}" : $"Rank changed to {pilot.Rank.Name}",
            promoted
                ? $"Congratulations! You are now {pilot.DisplayId}."
                : $"Your points dropped below the minimum for your previous rank. You are now {pilot.DisplayId}.",
            "/dashboard");
    }
}