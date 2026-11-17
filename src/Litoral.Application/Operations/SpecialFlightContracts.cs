using Litoral.Application.Admin;

namespace Litoral.Application.Operations;

public sealed record SpecialFlightLegDto(
    int Order,
    string Callsign,
    string DepartureIcao,
    string DepartureCity,
    string ArrivalIcao,
    string ArrivalCity,
    int DistanceNm,
    int EstimatedMinutes,
    bool IsInternational,
    string? BookingStatus,
    DateTime? CompletedAt);

public sealed record SpecialFlightDto(
    Guid Id,
    string Title,
    string? Description,
    string Type,
    int Difficulty,
    Guid AircraftId,
    string Registration,
    string AircraftTypeIcao,
    string AircraftTypeName,
    string? AircraftPhotoUrl,
    string MinimumRankCode,
    string MinimumRankName,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    string State,
    int? PilotNumber,
    string? PilotDisplayId,
    string? PilotName,
    string? PhotoUrl,
    string? PhotoCredit,
    IReadOnlyList<SpecialFlightLegDto> Legs,
    DateTime CreatedAt);

public sealed record SpecialFlightRequest(
    string Title,
    string? Description,
    string Type,
    int Difficulty,
    Guid AircraftId,
    string MinimumRank,
    DateTime StartsAt,
    DateTime EndsAt,
    IReadOnlyList<string> Stops);

public sealed record OperationsAircraftDto(
    Guid Id,
    string Registration,
    string TypeIcao,
    string TypeName,
    bool IsCargo,
    string LocationIcao,
    string Status,
    string? DeliveryAirportIcao,
    bool InSpecialFlight);

public sealed record OperationsAirportDto(string Icao, string Name, string City);

public sealed record OperationsRankDto(string Code, string Name, int Level);

public sealed record SpecialFlightOptionsDto(
    string MaintenanceHubIcao,
    IReadOnlyList<OperationsAircraftDto> Aircraft,
    IReadOnlyList<OperationsAirportDto> Airports,
    IReadOnlyList<OperationsRankDto> Ranks);

public sealed record PilotSpecialFlightDto(
    SpecialFlightDto Flight,
    bool IsMine,
    bool CanTake,
    string? BlockedReason,
    int? NextLegOrder,
    bool CanBookNextLeg);

public interface ISpecialFlightService
{
    Task<IReadOnlyList<PilotSpecialFlightDto>> ListAsync(string userId, CancellationToken ct);
    Task<PilotSpecialFlightDto> TakeAsync(string userId, Guid id, CancellationToken ct);
    Task<PilotSpecialFlightDto> BookNextLegAsync(string userId, Guid id, CancellationToken ct);
}

public interface ISpecialFlightAdminService
{
    Task<SpecialFlightOptionsDto> OptionsAsync(CancellationToken ct);
    Task<IReadOnlyList<SpecialFlightDto>> ListAsync(CancellationToken ct);
    Task<SpecialFlightDto> CreateAsync(SpecialFlightRequest request, CancellationToken ct);
    Task<SpecialFlightDto> UpdateAsync(Guid id, SpecialFlightRequest request, CancellationToken ct);
    Task<SpecialFlightDto> CancelAsync(Guid id, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<SpecialFlightDto> UploadPhotoAsync(Guid id, PhotoUpload upload, CancellationToken ct);
    Task<SpecialFlightDto> UpdatePhotoCreditAsync(Guid id, string? credit, CancellationToken ct);
    Task<SpecialFlightDto> RemovePhotoAsync(Guid id, CancellationToken ct);
}