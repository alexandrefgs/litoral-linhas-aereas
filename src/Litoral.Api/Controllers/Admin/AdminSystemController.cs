using Litoral.Application.Admin;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/system")]
[Authorize(Policy = StaffPolicies.ViewSystem)]
public class AdminSystemController(ISystemMonitorService monitor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SystemStatusDto>> Get(CancellationToken ct) =>
        Ok(await monitor.StatusAsync(ct));
}