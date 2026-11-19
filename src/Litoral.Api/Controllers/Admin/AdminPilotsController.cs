using Litoral.Api.Extensions;
using Litoral.Application.Admin;
using Litoral.Application.Auth;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/pilots")]
[Authorize(Policy = StaffPolicies.ManagePilots)]
public class AdminPilotsController(IPilotAdminService pilots, ITwoFactorService twoFactor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminPilotDto>>> List(CancellationToken ct) =>
        Ok(await pilots.ListAsync(ct));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<AdminPilotDto>> Deactivate(Guid id, CancellationToken ct) =>
        Ok(await pilots.DeactivateAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<AdminPilotDto>> Activate(Guid id, CancellationToken ct) =>
        Ok(await pilots.ActivateAsync(User.GetUserId(), id, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await pilots.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/two-factor/reset")]
    public async Task<IActionResult> ResetTwoFactor(Guid id, CancellationToken ct)
    {
        await twoFactor.ResetAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}