using Litoral.Application.Admin;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/airports")]
[Authorize(Policy = StaffPolicies.ManageRoutes)]
public class AdminAirportsController(IAirportAdminService airports, IAirportLookupService lookup) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminAirportDto>>> List(CancellationToken ct) =>
        Ok(await airports.ListAsync(ct));

    [HttpGet("lookup/{icao}")]
    public async Task<ActionResult<AirportLookupDto>> Lookup(string icao, CancellationToken ct) =>
        Ok(await lookup.FindAsync(icao, ct));

    [HttpGet("{icao}")]
    public async Task<ActionResult<AdminAirportDto>> Get(string icao, CancellationToken ct) =>
        Ok(await airports.GetAsync(icao, ct));

    [HttpPost]
    public async Task<ActionResult<AdminAirportDto>> Create(CreateAirportRequest request, CancellationToken ct)
    {
        var airport = await airports.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { icao = airport.Icao }, airport);
    }

    [HttpPut("{icao}")]
    public async Task<ActionResult<AdminAirportDto>> Update(string icao, UpdateAirportRequest request, CancellationToken ct) =>
        Ok(await airports.UpdateAsync(icao, request, ct));

    [HttpDelete("{icao}")]
    public async Task<IActionResult> Delete(string icao, CancellationToken ct)
    {
        await airports.DeleteAsync(icao, ct);
        return NoContent();
    }
        [HttpPost("{icao}/photo")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<AdminAirportDto>> UploadPhoto(string icao, IFormFile file, [FromForm] string? credit, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await airports.UploadPhotoAsync(icao, new PhotoUpload(stream, file.FileName, file.ContentType, file.Length, credit), ct));
    }

    [HttpPut("{icao}/photo/credit")]
    public async Task<ActionResult<AdminAirportDto>> UpdatePhotoCredit(string icao, AirportPhotoCreditRequest request, CancellationToken ct) =>
        Ok(await airports.UpdatePhotoCreditAsync(icao, request.Credit, ct));

    [HttpDelete("{icao}/photo")]
    public async Task<ActionResult<AdminAirportDto>> RemovePhoto(string icao, CancellationToken ct) =>
        Ok(await airports.RemovePhotoAsync(icao, ct));
}