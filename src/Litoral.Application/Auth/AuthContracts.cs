using System.ComponentModel.DataAnnotations;

namespace Litoral.Application.Auth;

public sealed record RegisterRequest(
    [Required, MaxLength(60)] string FirstName,
    [Required, MaxLength(60)] string LastName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [StringLength(4, MinimumLength = 4)] string? HubIcao);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record PilotSummary(
    int PilotNumber,
    string DisplayId,
    string FirstName,
    string LastName,
    string Email,
    string RankCode,
    string RankName,
    string CurrentLocationIcao,
    int TotalFlightMinutes,
    int Points,
    string? SimBriefUsername,
    IReadOnlyList<string> StaffPositions);

public sealed record AuthResponse(string Token, DateTime ExpiresAt, PilotSummary Pilot);