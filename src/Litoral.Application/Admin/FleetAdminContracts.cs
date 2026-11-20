namespace Litoral.Application.Admin;

public sealed record AdminAircraftTypeDto(
    Guid Id,
    string IcaoCode,
    string Name,
    bool IsCargo,
    int MaxRangeNm,
    int MaintenanceIntervalHours,
    int CCheckIntervalHours,
    int DCheckIntervalHours,
    string? RegistrationPrefix,
    string? DeliveryAirportIcao,
    int AircraftCount,
    int RouteCount);

public sealed record AircraftTypeRequest(
    string IcaoCode,
    string Name,
    bool IsCargo,
    int MaxRangeNm,
    int MaintenanceIntervalHours,
    int CCheckIntervalHours,
    int DCheckIntervalHours,
    string RegistrationPrefix,
    string? DeliveryAirportIcao);

public sealed record AdminAircraftDto(
    Guid Id,
    string Registration,
    Guid AircraftTypeId,
    string AircraftTypeIcao,
    string AircraftTypeName,
    bool IsCargo,
    string? Variant,
    string? SimBriefAirframeId,
    string? PhotoUrl,
    string? PhotoCredit,
    string CurrentAirportIcao,
    string CurrentAirportCity,
    string Status,
    int TotalMinutes,
    int Cycles,
    string NextCheck,
    int NextCheckUsedMinutes,
    int NextCheckIntervalHours,
    bool MaintenanceDue,
    string? CurrentCheck,
    string? MaintenanceReason,
    DateTime? MaintenanceStartedAt,
    DateTime? MaintenanceUntil,
    DateTime? LastMaintenanceAt,
    DateTime? LastFlightAt);

public sealed record AircraftRequest(
    string Registration,
    Guid AircraftTypeId,
    string? Variant,
    string CurrentAirportIcao,
    string? SimBriefAirframeId = null);

public sealed record RegistrationSuggestionDto(string Block, string? Registration, int Free);

public sealed record MaintenanceRequest(string Reason, DateTime? Until);

public sealed record FleetAirportDto(string Icao, string Name, string City, bool Passenger, bool Cargo);

public sealed record PhotoUpload(Stream Content, string FileName, string ContentType, long Length, string? Credit);

public interface IFleetAdminService
{
    Task<IReadOnlyList<AdminAircraftTypeDto>> ListTypesAsync(CancellationToken ct);
    Task<AdminAircraftTypeDto> CreateTypeAsync(AircraftTypeRequest request, CancellationToken ct);
    Task<AdminAircraftTypeDto> UpdateTypeAsync(Guid id, AircraftTypeRequest request, CancellationToken ct);
    Task DeleteTypeAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<AdminAircraftDto>> ListAircraftAsync(CancellationToken ct);
    Task<RegistrationSuggestionDto> SuggestRegistrationAsync(Guid aircraftTypeId, CancellationToken ct);
    Task<AdminAircraftDto> GetAircraftAsync(Guid id, CancellationToken ct);
    Task<AdminAircraftDto> CreateAircraftAsync(AircraftRequest request, CancellationToken ct);
    Task<AdminAircraftDto> UpdateAircraftAsync(Guid id, AircraftRequest request, CancellationToken ct);
    Task DeleteAircraftAsync(Guid id, CancellationToken ct);

    Task<AdminAircraftDto> UploadPhotoAsync(Guid id, PhotoUpload upload, CancellationToken ct);
    Task<AdminAircraftDto> UpdatePhotoCreditAsync(Guid id, string? credit, CancellationToken ct);
    Task<AdminAircraftDto> RemovePhotoAsync(Guid id, CancellationToken ct);

    Task<AdminAircraftDto> SendToMaintenanceAsync(Guid id, MaintenanceRequest request, CancellationToken ct);
    Task<AdminAircraftDto> ReleaseFromMaintenanceAsync(Guid id, CancellationToken ct);
    Task<AdminAircraftDto> SetActiveAsync(Guid id, bool active, CancellationToken ct);

    Task<IReadOnlyList<FleetAirportDto>> ListAirportsAsync(CancellationToken ct);
}

public interface IPhotoStorage
{
    Task<string> SaveAsync(string folder, PhotoUpload upload, CancellationToken ct);
    void Delete(string? url);
}