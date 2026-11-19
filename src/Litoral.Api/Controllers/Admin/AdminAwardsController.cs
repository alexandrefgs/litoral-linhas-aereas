using Litoral.Application.Admin;
using Litoral.Application.Awards;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/awards")]
[Authorize(Policy = StaffPolicies.ManageOperations)]
public class AdminAwardsController(IAwardAdminService awards) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminAwardDto>>> List(CancellationToken ct) =>
        Ok(await awards.ListAsync(ct));

    [HttpGet("pilots")]
    public async Task<ActionResult<IReadOnlyList<AwardPilotOptionDto>>> Pilots(CancellationToken ct) =>
        Ok(await awards.PilotsAsync(ct));

    [HttpPost]
    public async Task<ActionResult<AdminAwardDto>> Create(AwardRequest request, CancellationToken ct) =>
        Ok(await awards.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminAwardDto>> Update(Guid id, AwardRequest request, CancellationToken ct) =>
        Ok(await awards.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await awards.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<AdminAwardDto>> UploadImage(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await awards.UploadImageAsync(id, new PhotoUpload(stream, file.FileName, file.ContentType, file.Length, null), ct));
    }

    [HttpDelete("{id:guid}/image")]
    public async Task<ActionResult<AdminAwardDto>> RemoveImage(Guid id, CancellationToken ct) =>
        Ok(await awards.RemoveImageAsync(id, ct));

    [HttpGet("{id:guid}/holders")]
    public async Task<ActionResult<IReadOnlyList<AwardHolderDto>>> Holders(Guid id, CancellationToken ct) =>
        Ok(await awards.HoldersAsync(id, ct));

    [HttpPost("{id:guid}/holders")]
    public async Task<ActionResult<IReadOnlyList<AwardHolderDto>>> Grant(Guid id, GrantAwardRequest request, CancellationToken ct) =>
        Ok(await awards.GrantAsync(id, request, ct));

    [HttpDelete("{id:guid}/holders/{pilotId:guid}")]
    public async Task<ActionResult<IReadOnlyList<AwardHolderDto>>> Revoke(Guid id, Guid pilotId, CancellationToken ct) =>
        Ok(await awards.RevokeAsync(id, pilotId, ct));
}