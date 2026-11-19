using System.ComponentModel.DataAnnotations;

namespace Litoral.Application.Auth;

public sealed record RegisterRequest(
    [Required, MaxLength(60)] string FirstName,
    [Required, MaxLength(60)] string LastName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [StringLength(4, MinimumLength = 4)] string? HubIcao,
    bool AcceptedTerms = false);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    [MaxLength(2000)] string? TrustedDevice = null);

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
    IReadOnlyList<string> StaffPositions,
    string DisplayName,
    string? PhotoUrl,
    bool TwoFactorEnabled,
    bool TwoFactorRequired);

public sealed record AuthResponse(string Token, DateTime ExpiresAt, PilotSummary Pilot, string? TrustedDevice = null);

public sealed record LoginResponse(AuthResponse? Session, string? ChallengeToken);

public sealed record TwoFactorLoginRequest(
    [Required, MaxLength(2000)] string ChallengeToken,
    [Required, MaxLength(20)] string Code,
    bool RememberDevice);