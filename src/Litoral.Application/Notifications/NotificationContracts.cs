namespace Litoral.Application.Notifications;

public sealed record NotificationDto(Guid Id, string Type, string Title, string Message, string? Link, DateTime CreatedAt, bool IsRead);

public sealed record NotificationListDto(int Unread, IReadOnlyList<NotificationDto> Items);

public interface INotificationService
{
    Task<NotificationListDto> ListAsync(string userId, CancellationToken ct);
    Task<int> UnreadCountAsync(string userId, CancellationToken ct);
    Task MarkReadAsync(string userId, Guid id, CancellationToken ct);
    Task MarkAllReadAsync(string userId, CancellationToken ct);
}