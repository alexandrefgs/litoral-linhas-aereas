namespace Litoral.Application.Mel;

public sealed record MelItemDto(
    Guid Id,
    string Code,
    string Kind,
    string Title,
    string Category,
    int? CategoryADays,
    int Days,
    string Restriction,
    string Procedure,
    string Verification,
    string AcarsCheck,
    int? CheckValue,
    string? CheckText,
    decimal? FuelPenaltyPercent,
    IReadOnlyList<string> AircraftTypes,
    bool AutoEligible,
    bool IsActive,
    int TimesOpened);

public sealed record MelItemRequest(
    string Code,
    string Kind,
    string Title,
    string Category,
    int? CategoryADays,
    string Restriction,
    string Procedure,
    string Verification,
    string? AcarsCheck,
    int? CheckValue,
    string? CheckText,
    decimal? FuelPenaltyPercent,
    IReadOnlyList<string>? AircraftTypes,
    bool AutoEligible,
    bool IsActive);

public sealed record OpenMelItemDto(
    Guid Id,
    Guid AircraftId,
    string Registration,
    string AircraftTypeIcao,
    string AirportIcao,
    string Code,
    string Kind,
    string Title,
    string Category,
    string Restriction,
    string Procedure,
    string Verification,
    decimal? FuelPenaltyPercent,
    string Source,
    string OpenedBy,
    string? Note,
    DateTime OpenedAt,
    DateTime DueAt,
    bool Expired);

public sealed record MelAircraftOptionDto(Guid Id, string Registration, string AircraftTypeIcao, string AirportIcao, string Status);

public sealed record MelOverviewDto(
    IReadOnlyList<OpenMelItemDto> Open,
    IReadOnlyList<MelAircraftOptionDto> Aircraft,
    IReadOnlyList<string> AircraftTypes);

public sealed record OpenMelRequest(Guid AircraftId, Guid MelItemId, string? Note);

public sealed record PilotMelItemDto(
    string Code,
    string Kind,
    string Title,
    string Category,
    string Restriction,
    string Procedure,
    string Verification,
    decimal? FuelPenaltyPercent,
    DateTime DueAt);

public sealed record AircraftMelDto(Guid AircraftId, IReadOnlyList<PilotMelItemDto> Items);

public sealed record FlightMelCheckDto(
    Guid Id,
    string Code,
    string Title,
    string Restriction,
    string Procedure,
    string Verification,
    string Result,
    string? Detail);

public sealed record DecideMelRequest(bool Complied);

public interface IMelAdminService
{
    Task<IReadOnlyList<MelItemDto>> CatalogAsync(CancellationToken ct);
    Task<MelItemDto> CreateAsync(MelItemRequest request, CancellationToken ct);
    Task<MelItemDto> UpdateAsync(Guid id, MelItemRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<MelOverviewDto> OverviewAsync(CancellationToken ct);
    Task<MelOverviewDto> OpenAsync(OpenMelRequest request, CancellationToken ct);
    Task<MelOverviewDto> CloseAsync(Guid id, CancellationToken ct);
}

public interface IMelService
{
    Task<IReadOnlyList<AircraftMelDto>> ForAircraftAsync(IReadOnlyCollection<Guid> aircraftIds, CancellationToken ct);
}