using Litoral.Application.Admin;
using Litoral.Application.Bookings;

namespace Litoral.Application.Tours;

public sealed record TourAirportDto(string Icao, string Name, string City, string Country, double Latitude, double Longitude);

public sealed record TourLegDto(int Order, string Callsign, TourAirportDto Departure, TourAirportDto Arrival, int DistanceNm, int EstimatedMinutes);

public sealed record TourAwardDto(Guid Id, string Name, string? ImageUrl);

public sealed record TourProgressDto(int CompletedLegs, DateTime StartedAt, DateTime? CompletedAt, string? CurrentLegState);

public sealed record PilotTourDto(
    Guid Id,
    string Title,
    string Description,
    string? ImageUrl,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string State,
    string MinimumRankName,
    bool IsCargo,
    IReadOnlyList<string> AircraftTypes,
    int BonusPoints,
    TourAwardDto Award,
    IReadOnlyList<TourLegDto> Legs,
    int TotalDistanceNm,
    int PilotsCompleted,
    TourProgressDto? Progress,
    int? NextLegOrder,
    bool CanBook,
    string? BlockedReason);

public sealed record AdminTourDto(
    Guid Id,
    string Title,
    string Description,
    string? ImageUrl,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string State,
    bool IsPublished,
    int MinimumRankLevel,
    string MinimumRankName,
    bool IsCargo,
    IReadOnlyList<string> AircraftTypes,
    int BonusPoints,
    TourAwardDto Award,
    IReadOnlyList<TourLegDto> Legs,
    int PilotsStarted,
    int PilotsCompleted,
    bool LegsLocked);

public sealed record TourOptionsDto(
    IReadOnlyList<TourOptionAirportDto> Airports,
    IReadOnlyList<TourOptionRankDto> Ranks,
    IReadOnlyList<TourOptionTypeDto> AircraftTypes);

public sealed record TourOptionAirportDto(string Icao, string Name, string City, int MinimumRankLevel);

public sealed record TourOptionRankDto(string Code, string Name, int Level);

public sealed record TourOptionTypeDto(string IcaoCode, string Name, int MaxRangeNm, bool IsCargo);

public sealed record TourRequest(
    string Title,
    string Description,
    DateTime? StartsAt,
    DateTime? EndsAt,
    string MinimumRank,
    bool IsCargo,
    IReadOnlyList<string> AircraftTypes,
    int BonusPoints,
    IReadOnlyList<string> Stops,
    bool IsPublished);

public sealed record BookTourLegRequest(Guid? AircraftId);

public interface ITourService
{
    Task<IReadOnlyList<PilotTourDto>> ListAsync(string userId, CancellationToken ct);
    Task<AircraftOptionsDto> AircraftAsync(string userId, Guid tourId, CancellationToken ct);
    Task<BookingDto> BookNextLegAsync(string userId, Guid tourId, BookTourLegRequest request, CancellationToken ct);
}

public interface ITourAdminService
{
    Task<TourOptionsDto> OptionsAsync(CancellationToken ct);
    Task<IReadOnlyList<AdminTourDto>> ListAsync(CancellationToken ct);
    Task<AdminTourDto> CreateAsync(TourRequest request, CancellationToken ct);
    Task<AdminTourDto> UpdateAsync(Guid id, TourRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<AdminTourDto> UploadImageAsync(Guid id, PhotoUpload upload, CancellationToken ct);
    Task<AdminTourDto> RemoveImageAsync(Guid id, CancellationToken ct);
}