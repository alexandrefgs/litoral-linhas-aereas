using Litoral.Application.Common;
using Litoral.Application.Mel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/mel")]
[Authorize(Policy = StaffPolicies.ManageFleet)]
public class AdminMelController(IMelAdminService mel) : ControllerBase
{
    [HttpGet("catalog")]
    public async Task<ActionResult<IReadOnlyList<MelItemDto>>> Catalog(CancellationToken ct) =>
        Ok(await mel.CatalogAsync(ct));

    [HttpPost("catalog")]
    public async Task<ActionResult<MelItemDto>> Create(MelItemRequest request, CancellationToken ct) =>
        Ok(await mel.CreateAsync(request, ct));

    [HttpPut("catalog/{id:guid}")]
    public async Task<ActionResult<MelItemDto>> Update(Guid id, MelItemRequest request, CancellationToken ct) =>
        Ok(await mel.UpdateAsync(id, request, ct));

    [HttpDelete("catalog/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mel.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("open")]
    public async Task<ActionResult<MelOverviewDto>> Overview(CancellationToken ct) =>
        Ok(await mel.OverviewAsync(ct));

    [HttpPost("open")]
    public async Task<ActionResult<MelOverviewDto>> Open(OpenMelRequest request, CancellationToken ct) =>
        Ok(await mel.OpenAsync(request, ct));

    [HttpPost("open/{id:guid}/close")]
    public async Task<ActionResult<MelOverviewDto>> Close(Guid id, CancellationToken ct) =>
        Ok(await mel.CloseAsync(id, ct));
}