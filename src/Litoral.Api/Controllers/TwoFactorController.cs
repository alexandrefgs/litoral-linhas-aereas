using Litoral.Api.Extensions;
using Litoral.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account/two-factor")]
public class TwoFactorController(ITwoFactorService twoFactor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TwoFactorStatus>> Status(CancellationToken ct) =>
        Ok(await twoFactor.GetStatusAsync(User.GetUserId(), ct));

    [HttpPost("setup")]
    public async Task<ActionResult<TwoFactorSetup>> Setup(CancellationToken ct) =>
        Ok(await twoFactor.BeginSetupAsync(User.GetUserId(), ct));

    [HttpPost("enable")]
    public async Task<ActionResult<RecoveryCodesResponse>> Enable(TwoFactorEnableRequest request, CancellationToken ct) =>
        Ok(await twoFactor.EnableAsync(User.GetUserId(), request, ct));

    [HttpPost("recovery-codes")]
    public async Task<ActionResult<RecoveryCodesResponse>> RecoveryCodes(TwoFactorConfirmRequest request, CancellationToken ct) =>
        Ok(await twoFactor.RegenerateRecoveryCodesAsync(User.GetUserId(), request, ct));

    [HttpPost("disable")]
    public async Task<IActionResult> Disable(TwoFactorConfirmRequest request, CancellationToken ct)
    {
        await twoFactor.DisableAsync(User.GetUserId(), request, ct);
        return NoContent();
    }
}