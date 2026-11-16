namespace Litoral.Application.Pilots;

public sealed record ProfileAircraftDto(string IcaoCode, string Name, int FleetCount);

public sealed record ProfilePrivateDto(string Email, DateOnly? BirthDate, string? DiscordId);

public sealed record ActivityMonthDto(int Month, int Flights, int Minutes);

public sealed record ActivityYearDto(int Year, int Flights, int Minutes, IReadOnlyList<ActivityMonthDto> Months);

public sealed record ProfileAirportDto(string Icao, string Name, string City, string Country, double Latitude, double Longitude);

public sealed record TopRouteDto(ProfileAirportDto Departure, ProfileAirportDto Arrival, int Flights);

public sealed record ProfileDto(
    int PilotNumber,
    string DisplayId,
    string FirstName,
    string LastName,
    string DisplayName,
    string RankCode,
    string RankName,
    bool IsActive,
    string BaseIcao,
    string CurrentLocationIcao,
    string? CountryCode,
    string? State,
    string? City,
    string? VatsimId,
    string? IvaoId,
    int? Age,
    DateTime JoinedAt,
    int TotalFlightMinutes,
    int TotalFlights,
    int Points,
    string? PhotoUrl,
    string? CoverUrl,
    bool IsOwner,
    ProfilePrivateDto? Private,
    IReadOnlyList<ProfileAircraftDto> SimAircraft,
    IReadOnlyList<ActivityYearDto> Activity,
    IReadOnlyList<TopRouteDto> TopRoutes);

public sealed record FlightMapRouteDto(string DepartureIcao, string ArrivalIcao, int Flights);

public sealed record FlightMapDto(IReadOnlyList<ProfileAirportDto> Airports, IReadOnlyList<FlightMapRouteDto> Routes);

public sealed record ProfileFlightDto(
    Guid Id,
    string Callsign,
    string DepartureIcao,
    string ArrivalIcao,
    string AircraftTypeIcao,
    string Registration,
    int BlockMinutes,
    int Points,
    bool IsRejected,
    DateTime Date);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record ProfileFlightsQuery(int Page = 1, int PageSize = 10, string? Search = null, string? Sort = null);

public interface IProfileService
{
    Task<ProfileDto> GetAsync(string viewerUserId, int pilotNumber, CancellationToken ct);
    Task<FlightMapDto> FlightMapAsync(int pilotNumber, CancellationToken ct);
    Task<PagedResult<ProfileFlightDto>> FlightsAsync(int pilotNumber, ProfileFlightsQuery query, CancellationToken ct);
}