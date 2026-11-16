using Litoral.Application.Common;
using Litoral.Application.Scoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/scoring")]
[Authorize(Policy = StaffPolicies.ReviewPireps)]
public class AdminScoringController(IScoringRuleService scoring) : ControllerBase
{
    [HttpGet("catalog")]
    public ActionResult<ScoringCatalogDto> Catalog() => Ok(scoring.Catalog());

    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<ScoringRuleDto>>> List(CancellationToken ct) =>
        Ok(await scoring.ListAsync(ct));

    [HttpPost("rules")]
    public async Task<ActionResult<ScoringRuleDto>> Create(ScoringRuleRequest request, CancellationToken ct) =>
        Ok(await scoring.CreateAsync(request, ct));

    [HttpPut("rules/{id:guid}")]
    public async Task<ActionResult<ScoringRuleDto>> Update(Guid id, ScoringRuleRequest request, CancellationToken ct) =>
        Ok(await scoring.UpdateAsync(id, request, ct));

    [HttpDelete("rules/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await scoring.DeleteAsync(id, ct);
        return NoContent();
    }
}