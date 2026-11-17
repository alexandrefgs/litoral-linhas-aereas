namespace Litoral.Application.Bookings;

public sealed record RosterLegDto(
    Guid Id,
    int Order,
    int Sequence,
    Guid RouteId,
    string Callsign,
    string DepartureIcao,
    string DepartureCity,
    string ArrivalIcao,
    string ArrivalCity,
    bool IsCargo,
    int DistanceNm,
    int EstimatedMinutes,
    string State,
    string? ArrivalPhotoUrl);

public sealed record RosterDto(
    Guid Id,
    int Year,
    int Month,
    int FlightsPerDay,
    string BaseIcao,
    int TotalFlights,
    int CompletedFlights,
    int Bonus,
    DateTime? CompletedAt,
    IReadOnlyList<RosterLegDto> Legs);

public sealed record RosterStatusDto(RosterDto? Roster, int RemainingDays, bool CanCreate, string LocationIcao, bool HasOtherOpenBooking);

public sealed record CreateRosterRequest(int FlightsPerDay);

public sealed record BookLegRequest(Guid? AircraftId);

public interface IRosterService
{
    Task<RosterStatusDto> GetAsync(string userId, CancellationToken ct);
    Task<RosterStatusDto> CreateAsync(string userId, CreateRosterRequest request, CancellationToken ct);
    Task<AircraftOptionsDto> ListLegAircraftAsync(string userId, Guid legId, CancellationToken ct);
    Task<BookingDto> BookLegAsync(string userId, Guid legId, BookLegRequest request, CancellationToken ct);
}