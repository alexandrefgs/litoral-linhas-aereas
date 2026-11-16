namespace Litoral.Application.Bookings;

public sealed record BookingAirportDto(string Icao, string Name, string City, string Country, double Latitude, double Longitude);

public sealed record BookingAircraftDto(
    Guid Id,
    string Registration,
    Guid TypeId,
    string TypeIcao,
    string TypeName,
    string? Variant,
    string? PhotoUrl,
    string? PhotoCredit,
    DateTime? LastFlightAt);

public sealed record CrewDto(string DisplayId, string Name, string RankCode);

public sealed record BookingDto(
    Guid Id,
    string Callsign,
    string NetworkCallsign,
    int FlightNumber,
    string Type,
    string Status,
    BookingAirportDto Departure,
    BookingAirportDto Arrival,
    int DistanceNm,
    int EstimatedMinutes,
    bool IsCargo,
    BookingAircraftDto Aircraft,
    CrewDto? Crew,
    int PointsCost,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public sealed record DestinationDto(
    Guid RouteId,
    int FlightNumber,
    string Callsign,
    BookingAirportDto Arrival,
    int DistanceNm,
    int EstimatedMinutes,
    bool IsCargo,
    IReadOnlyList<string> AircraftTypes,
    int AvailableAircraft,
    int Cost);

public sealed record BookingOverviewDto(
    string LocationIcao,
    string LocationCity,
    string BaseIcao,
    int Points,
    int CustomCost,
    BookingDto? Current);

public sealed record AircraftOptionsDto(Guid RouteId, string Callsign, int ValidityHours, IReadOnlyList<BookingAircraftDto> Aircraft);

public sealed record RandomProposalDto(DestinationDto Route, BookingAircraftDto Aircraft);

public sealed record BookFlightRequest(Guid RouteId, Guid? AircraftId);

public enum FlightDuration
{
    Any,
    Short,
    Medium,
    Long
}

public interface IBookingService
{
    Task<BookingOverviewDto> GetOverviewAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<DestinationDto>> ListDestinationsAsync(string userId, CancellationToken ct);
    Task<AircraftOptionsDto> ListAircraftAsync(string userId, Guid routeId, CancellationToken ct);
    Task<RandomProposalDto> ProposeRandomAsync(string userId, FlightDuration duration, CancellationToken ct);
    Task<BookingDto> BookRandomAsync(string userId, BookFlightRequest request, CancellationToken ct);
    Task<BookingDto> BookCustomAsync(string userId, BookFlightRequest request, CancellationToken ct);
    Task CancelCurrentAsync(string userId, CancellationToken ct);
}