using System.ComponentModel.DataAnnotations;
using Litoral.Application.Admin;

namespace Litoral.Application.Pilots;

public sealed record NameFormatOptionDto(string Value, string Example);

public sealed record SimAircraftOptionDto(string IcaoCode, string Name);

public sealed record PilotSettingsDto(
    string FirstName,
    string LastName,
    string NameFormat,
    IReadOnlyList<NameFormatOptionDto> NameFormats,
    string Email,
    DateOnly? BirthDate,
    string? CountryCode,
    string? State,
    string? City,
    string? VatsimId,
    string? IvaoId,
    string? DiscordId,
    string? SimBriefUsername,
    IReadOnlyList<string> SimAircraft,
    IReadOnlyList<SimAircraftOptionDto> AircraftTypes,
    string? PhotoUrl,
    string? CoverUrl);

public sealed record UpdatePersonalRequest(
    [Required, MaxLength(60)] string FirstName,
    [Required, MaxLength(60)] string LastName,
    [Required] string NameFormat,
    DateOnly? BirthDate,
    [MaxLength(2)] string? CountryCode,
    [MaxLength(60)] string? State,
    [MaxLength(80)] string? City,
    [MaxLength(10)] string? VatsimId,
    [MaxLength(10)] string? IvaoId,
    [MaxLength(20)] string? DiscordId);

public sealed record UpdateConfigRequest(
    [MaxLength(60)] string? SimBriefUsername,
    IReadOnlyList<string>? SimAircraft);

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword);

public interface IPilotSettingsService
{
    Task<PilotSettingsDto> GetAsync(string userId, CancellationToken ct);
    Task<PilotSettingsDto> UpdatePersonalAsync(string userId, UpdatePersonalRequest request, CancellationToken ct);
    Task<PilotSettingsDto> UpdateConfigAsync(string userId, UpdateConfigRequest request, CancellationToken ct);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct);
    Task<PilotSettingsDto> UploadPhotoAsync(string userId, PhotoUpload upload, CancellationToken ct);
    Task<PilotSettingsDto> RemovePhotoAsync(string userId, CancellationToken ct);
    Task<PilotSettingsDto> UploadCoverAsync(string userId, PhotoUpload upload, CancellationToken ct);
    Task<PilotSettingsDto> RemoveCoverAsync(string userId, CancellationToken ct);
}