using Litoral.Api.Extensions;
using Litoral.Application.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize]
public class StatisticsController(IStatisticsService statistics) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StatisticsDto>> Get(CancellationToken ct) =>
        Ok(await statistics.GetAsync(User.GetUserId(), ct));
}