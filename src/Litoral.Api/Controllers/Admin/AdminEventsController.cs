using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/events")]
[Authorize(Policy = StaffPolicies.ManageOperations)]
public class AdminEventsController(IEventAdminService events) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminEventDto>>> List(CancellationToken ct) =>
        Ok(await events.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<AdminEventDto>> Create(EventRequest request, CancellationToken ct) =>
        Ok(await events.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminEventDto>> Update(Guid id, EventRequest request, CancellationToken ct) =>
        Ok(await events.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await events.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<AdminEventDto>> UploadImage(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await events.UploadImageAsync(id, new PhotoUpload(stream, file.FileName, file.ContentType, file.Length, null), ct));
    }

    [HttpDelete("{id:guid}/image")]
    public async Task<ActionResult<AdminEventDto>> RemoveImage(Guid id, CancellationToken ct) =>
        Ok(await events.RemoveImageAsync(id, ct));
}