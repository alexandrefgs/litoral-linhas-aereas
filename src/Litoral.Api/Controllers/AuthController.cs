using Litoral.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct) =>
        Ok(await auth.RegisterAsync(request, ct));

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));

    [HttpPost("login/two-factor")]
    public async Task<ActionResult<AuthResponse>> LoginWithTwoFactor(TwoFactorLoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginWithTwoFactorAsync(request, ct));

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<PilotSummary>> Me(CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (userId is null)
            return Unauthorized();

        return Ok(await auth.GetCurrentAsync(userId, ct));
    }
}