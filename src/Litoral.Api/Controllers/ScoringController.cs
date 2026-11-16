using Litoral.Application.Scoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Route("api/scoring")]
[Authorize]
public class ScoringController(IScoringRuleService scoring) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScoringSectionDto>>> Table(CancellationToken ct) =>
        Ok(await scoring.TableAsync(ct));
}