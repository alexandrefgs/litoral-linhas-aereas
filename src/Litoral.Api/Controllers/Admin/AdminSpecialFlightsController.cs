using Litoral.Application.Admin;
using Litoral.Application.Common;
using Litoral.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/special-flights")]
[Authorize(Policy = StaffPolicies.ManageOperations)]
public class AdminSpecialFlightsController(ISpecialFlightAdminService flights) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ActionResult<SpecialFlightOptionsDto>> Options(CancellationToken ct) =>
        Ok(await flights.OptionsAsync(ct));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SpecialFlightDto>>> List(CancellationToken ct) =>
        Ok(await flights.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<SpecialFlightDto>> Create(SpecialFlightRequest request, CancellationToken ct) =>
        Ok(await flights.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SpecialFlightDto>> Update(Guid id, SpecialFlightRequest request, CancellationToken ct) =>
        Ok(await flights.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<SpecialFlightDto>> Cancel(Guid id, CancellationToken ct) =>
        Ok(await flights.CancelAsync(id, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await flights.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/photo")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<SpecialFlightDto>> UploadPhoto(Guid id, IFormFile file, [FromForm] string? credit, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await flights.UploadPhotoAsync(id, new PhotoUpload(stream, file.FileName, file.ContentType, file.Length, credit), ct));
    }

    [HttpPut("{id:guid}/photo/credit")]
    public async Task<ActionResult<SpecialFlightDto>> UpdatePhotoCredit(Guid id, AirportPhotoCreditRequest request, CancellationToken ct) =>
        Ok(await flights.UpdatePhotoCreditAsync(id, request.Credit, ct));

    [HttpDelete("{id:guid}/photo")]
    public async Task<ActionResult<SpecialFlightDto>> RemovePhoto(Guid id, CancellationToken ct) =>
        Ok(await flights.RemovePhotoAsync(id, ct));
}