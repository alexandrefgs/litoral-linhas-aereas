using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Tours;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/tours")]
[Authorize(Policy = StaffPolicies.ManageOperations)]
public class AdminToursController(ITourAdminService tours) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ActionResult<TourOptionsDto>> Options(CancellationToken ct) =>
        Ok(await tours.OptionsAsync(ct));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminTourDto>>> List(CancellationToken ct) =>
        Ok(await tours.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<AdminTourDto>> Create(TourRequest request, CancellationToken ct) =>
        Ok(await tours.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminTourDto>> Update(Guid id, TourRequest request, CancellationToken ct) =>
        Ok(await tours.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await tours.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<AdminTourDto>> UploadImage(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await tours.UploadImageAsync(id, new PhotoUpload(stream, file.FileName, file.ContentType, file.Length, null), ct));
    }

    [HttpDelete("{id:guid}/image")]
    public async Task<ActionResult<AdminTourDto>> RemoveImage(Guid id, CancellationToken ct) =>
        Ok(await tours.RemoveImageAsync(id, ct));
}