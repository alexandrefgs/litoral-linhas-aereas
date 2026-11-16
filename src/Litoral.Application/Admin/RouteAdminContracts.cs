namespace Litoral.Application.Admin;

public sealed record AdminRouteDto(
    Guid Id,
    int FlightNumber,
    string Callsign,
    string Block,
    string DepartureIcao,
    string DepartureCity,
    string ArrivalIcao,
    string ArrivalCity,
    bool IsCargo,
    IReadOnlyList<Guid> AircraftTypeIds,
    IReadOnlyList<string> AircraftTypes,
    int DistanceNm,
    int EstimatedMinutes,
    bool IsActive);

public sealed record CreateRouteRequest(
    int FlightNumber,
    string DepartureIcao,
    string ArrivalIcao,
    IReadOnlyList<Guid> AircraftTypeIds,
    int? EstimatedMinutes,
    bool CreateReturn,
    int? ReturnFlightNumber);

public sealed record UpdateRouteRequest(
    int FlightNumber,
    string DepartureIcao,
    string ArrivalIcao,
    IReadOnlyList<Guid> AircraftTypeIds,
    int? EstimatedMinutes,
    bool IsActive);

public sealed record FlightNumberSuggestionDto(string Block, int FlightNumber, int? ReturnFlightNumber);

public sealed record AircraftTypeDto(Guid Id, string IcaoCode, string Name, bool IsCargo, int MaxRangeNm);

public interface IRouteAdminService
{
    Task<IReadOnlyList<AdminRouteDto>> ListAsync(CancellationToken ct);
    Task<AdminRouteDto> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<AdminRouteDto>> CreateAsync(CreateRouteRequest request, CancellationToken ct);
    Task<AdminRouteDto> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<FlightNumberSuggestionDto> SuggestFlightNumberAsync(string departureIcao, string arrivalIcao, bool cargo, CancellationToken ct);
    Task<IReadOnlyList<AircraftTypeDto>> ListAircraftTypesAsync(CancellationToken ct);
}