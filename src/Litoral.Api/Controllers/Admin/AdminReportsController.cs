using Litoral.Application.Common;
using Litoral.Application.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Policy = StaffPolicies.ViewReports)]
public class AdminReportsController(IStatisticsService statistics) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReportDto>> Get([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct) =>
        Ok(await statistics.ReportAsync(from, to, ct));
}