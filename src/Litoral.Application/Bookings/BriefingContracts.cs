using Litoral.Application.Flights;

namespace Litoral.Application.Bookings;

public sealed record MetarDto(
    string Icao,
    string? Raw,
    DateTime? ObservedAt,
    string? Category,
    int? TemperatureC,
    int? DewpointC,
    string? WindDirection,
    int? WindSpeedKt,
    int? WindGustKt,
    string? Visibility,
    int? QnhHpa,
    string? Taf);

public sealed record BriefingDto(
    BookingDto Booking,
    string? SimBriefUsername,
    string SimBriefDispatchUrl,
    IReadOnlyList<MetarDto> Metars,
    FlightCrewMemberDto Captain,
    FlightCrewMemberDto FirstOfficer,
    IReadOnlyList<string> BaseIcaos,
    bool SimBriefApiEnabled,
    string FlightPlanRemarks);

public sealed record SimBriefLoadsheetDto(
    int? Passengers,
    int? Cargo,
    int? Payload,
    int? ZeroFuelWeight,
    int? MaxZeroFuelWeight,
    int? TakeoffWeight,
    int? MaxTakeoffWeight,
    int? LandingWeight,
    int? MaxLandingWeight,
    int? TaxiFuel,
    int? TripFuel,
    int? ContingencyFuel,
    int? AlternateFuel,
    int? ReserveFuel,
    int? ExtraFuel,
    int? TakeoffFuel,
    int? BlockFuel);

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
    DateTime? GeneratedAt,
    string? DepartureRunway,
    string? ArrivalRunway,
    int? CostIndex,
    string? Airac,
    SimBriefLoadsheetDto Loadsheet,
    string? PlanHtml);

public sealed record SimBriefRequest(
    string StaticId,
    string Origin,
    string Destination,
    string AircraftType,
    string Registration,
    string FlightNumber,
    string Callsign,
    string Captain,
    string FirstOfficer,
    string Remarks);

public sealed record SimBriefGenerateDto(string Url);

public interface IBriefingService
{
    Task<BriefingDto> GetAsync(string userId, CancellationToken ct);
    Task<SimBriefOfpDto> GetSimBriefAsync(string userId, CancellationToken ct);
    Task<byte[]> GetSimBriefPdfAsync(string userId, CancellationToken ct);
    Task<SimBriefGenerateDto> GenerateSimBriefAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<MetarDto>> GetWeatherAsync(string userId, CancellationToken ct);
}

public interface IMetarProvider
{
    Task<IReadOnlyList<MetarDto>> GetAsync(IReadOnlyList<string> icaos, CancellationToken ct);
}

public interface ISimBriefProvider
{
    bool CanGenerate { get; }
    string BuildGenerateUrl(SimBriefRequest request);
    Task<SimBriefOfpDto?> GetLatestAsync(string username, string? staticId, string expectedOrigin, string expectedDestination, CancellationToken ct);
    Task<byte[]> DownloadPdfAsync(string url, CancellationToken ct);
}