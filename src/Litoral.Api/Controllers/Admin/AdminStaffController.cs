using Litoral.Api.Extensions;
using Litoral.Application.Admin;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/staff")]
[Authorize(Policy = StaffPolicies.ManageStaff)]
public class AdminStaffController(IStaffAdminService staff) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StaffOverviewDto>> Get(CancellationToken ct) =>
        Ok(await staff.GetAsync(User.GetUserId(), ct));

    [HttpPost("{code}")]
    public async Task<ActionResult<StaffOverviewDto>> Assign(string code, AssignStaffRequest request, CancellationToken ct) =>
        Ok(await staff.AssignAsync(User.GetUserId(), code, request.PilotId, ct));

    [HttpDelete("{code}/{pilotId:guid}")]
    public async Task<ActionResult<StaffOverviewDto>> Remove(string code, Guid pilotId, CancellationToken ct) =>
        Ok(await staff.RemoveAsync(User.GetUserId(), code, pilotId, ct));
}