namespace Litoral.Application.Bookings;

public sealed record MetarDto(string Icao, string? Raw, DateTime? ObservedAt);

public sealed record BriefingDto(BookingDto Booking, string? SimBriefUsername, string SimBriefDispatchUrl, IReadOnlyList<MetarDto> Metars);

public sealed record SimBriefOfpDto(
    string Origin,
    string Destination,
    bool MatchesBooking,
    string? Route,
    int? CruiseAltitude,
    string? Alternate,
    int? BlockFuel,
    string? FuelUnits,
    int? EnrouteMinutes,
    string? PdfUrl,
    string? VatsimPrefileUrl,
    string? IvaoPrefileUrl,
    DateTime? GeneratedAt);

public interface IBriefingService
{
    Task<BriefingDto> GetAsync(string userId, CancellationToken ct);
    Task<SimBriefOfpDto> GetSimBriefAsync(string userId, CancellationToken ct);
}

public interface IMetarProvider
{
    Task<IReadOnlyList<MetarDto>> GetAsync(IReadOnlyList<string> icaos, CancellationToken ct);
}

public interface ISimBriefProvider
{
    Task<SimBriefOfpDto?> GetLatestAsync(string username, string expectedOrigin, string expectedDestination, CancellationToken ct);
}