using Litoral.Application.Admin;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/logs")]
[Authorize(Policy = StaffPolicies.ViewReports)]
public class AdminLogsController(IAuditLogQuery logs) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AuditLogPageDto>> List(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] bool criticalOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        Ok(await logs.ListAsync(category, search, criticalOnly, page, pageSize, ct));
}