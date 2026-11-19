using Litoral.Application.Admin;

namespace Litoral.Application.Events;

public sealed record PilotEventDto(
    Guid Id,
    string Title,
    string Description,
    string? ImageUrl,
    DateTime StartsAt,
    DateTime EndsAt,
    string State,
    IReadOnlyList<string> Airports,
    int BonusPoints,
    int Flights,
    int Pilots,
    int MyFlights);

public sealed record AdminEventDto(
    Guid Id,
    string Title,
    string Description,
    string? ImageUrl,
    DateTime StartsAt,
    DateTime EndsAt,
    string State,
    bool IsPublished,
    IReadOnlyList<string> Airports,
    int BonusPoints,
    int Flights,
    int Pilots);

public sealed record EventRequest(
    string Title,
    string Description,
    DateTime StartsAt,
    DateTime EndsAt,
    IReadOnlyList<string> Airports,
    int BonusPoints,
    bool IsPublished);

public interface IEventService
{
    Task<IReadOnlyList<PilotEventDto>> ListAsync(string userId, CancellationToken ct);
}

public interface IEventAdminService
{
    Task<IReadOnlyList<AdminEventDto>> ListAsync(CancellationToken ct);
    Task<AdminEventDto> CreateAsync(EventRequest request, CancellationToken ct);
    Task<AdminEventDto> UpdateAsync(Guid id, EventRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<AdminEventDto> UploadImageAsync(Guid id, PhotoUpload upload, CancellationToken ct);
    Task<AdminEventDto> RemoveImageAsync(Guid id, CancellationToken ct);
}