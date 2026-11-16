namespace Litoral.Application.Bookings;

public sealed record FleetAircraftDto(
    Guid Id,
    string Registration,
    string TypeIcao,
    string TypeName,
    bool IsCargo,
    string? Variant,
    string? PhotoUrl,
    string? PhotoCredit,
    string LocationIcao,
    string LocationCity,
    string Status,
    int TotalMinutes,
    DateTime? LastFlightAt);

public sealed record TransferQuoteDto(
    Guid AircraftId,
    string Registration,
    string FromIcao,
    string ToIcao,
    int DistanceNm,
    int Cost,
    bool Free,
    int Points,
    string? BlockedReason);

public interface IPilotFleetService
{
    Task<IReadOnlyList<FleetAircraftDto>> ListAsync(CancellationToken ct);
    Task<TransferQuoteDto> QuoteTransferAsync(string userId, Guid aircraftId, CancellationToken ct);
    Task<FleetAircraftDto> TransferAsync(string userId, Guid aircraftId, CancellationToken ct);
}