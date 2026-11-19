namespace Litoral.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResponse> LoginWithTwoFactorAsync(TwoFactorLoginRequest request, CancellationToken ct);
    Task<PilotSummary> GetCurrentAsync(string userId, CancellationToken ct);
}