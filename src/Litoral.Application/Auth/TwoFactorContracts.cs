using System.ComponentModel.DataAnnotations;

namespace Litoral.Application.Auth;

public sealed record TwoFactorStatus(bool Enabled, bool Required, int RecoveryCodesLeft);

public sealed record TwoFactorSetup(string SharedKey, string AuthenticatorUri);

public sealed record TwoFactorEnableRequest([Required, MaxLength(20)] string Code);

public sealed record TwoFactorConfirmRequest(
    [Required] string Password,
    [Required, MaxLength(20)] string Code);

public sealed record RecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);

public interface ITwoFactorService
{
    Task<TwoFactorStatus> GetStatusAsync(string userId, CancellationToken ct);
    Task<TwoFactorSetup> BeginSetupAsync(string userId, CancellationToken ct);
    Task<RecoveryCodesResponse> EnableAsync(string userId, TwoFactorEnableRequest request, CancellationToken ct);
    Task<RecoveryCodesResponse> RegenerateRecoveryCodesAsync(string userId, TwoFactorConfirmRequest request, CancellationToken ct);
    Task DisableAsync(string userId, TwoFactorConfirmRequest request, CancellationToken ct);
    Task ResetAsync(string actorUserId, Guid pilotId, CancellationToken ct);
}