using Litoral.Application.Applications;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/applications")]
[Authorize(Policy = StaffPolicies.ManagePilots)]
public class AdminApplicationsController(IApplicationService applications) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminApplicationsDto>> List(CancellationToken ct) =>
        Ok(await applications.ListAsync(ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await applications.ApproveAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectApplicationRequest request, CancellationToken ct)
    {
        await applications.RejectAsync(id, request, ct);
        return NoContent();
    }

    [HttpPut("registration")]
    public async Task<IActionResult> SetRegistration(SetRegistrationRequest request, CancellationToken ct)
    {
        await applications.SetRegistrationOpenAsync(request.Open, ct);
        return NoContent();
    }
}