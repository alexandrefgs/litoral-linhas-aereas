using Litoral.Application.Admin;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/fleet")]
[Authorize(Policy = StaffPolicies.ManageFleet)]
public class AdminFleetController(IFleetAdminService fleet) : ControllerBase
{
    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<AdminAircraftTypeDto>>> ListTypes(CancellationToken ct) =>
        Ok(await fleet.ListTypesAsync(ct));

    [HttpPost("types")]
    public async Task<ActionResult<AdminAircraftTypeDto>> CreateType(AircraftTypeRequest request, CancellationToken ct) =>
        Ok(await fleet.CreateTypeAsync(request, ct));

    [HttpPut("types/{id:guid}")]
    public async Task<ActionResult<AdminAircraftTypeDto>> UpdateType(Guid id, AircraftTypeRequest request, CancellationToken ct) =>
        Ok(await fleet.UpdateTypeAsync(id, request, ct));

    [HttpDelete("types/{id:guid}")]
    public async Task<IActionResult> DeleteType(Guid id, CancellationToken ct)
    {
        await fleet.DeleteTypeAsync(id, ct);
        return NoContent();
    }

    [HttpGet("airports")]
    public async Task<ActionResult<IReadOnlyList<FleetAirportDto>>> Airports(CancellationToken ct) =>
        Ok(await fleet.ListAirportsAsync(ct));

    [HttpGet("aircraft")]
    public async Task<ActionResult<IReadOnlyList<AdminAircraftDto>>> ListAircraft(CancellationToken ct) =>
        Ok(await fleet.ListAircraftAsync(ct));
    
    [HttpGet("aircraft/next-registration")]
    public async Task<ActionResult<RegistrationSuggestionDto>> NextRegistration([FromQuery] Guid typeId, CancellationToken ct) =>
        Ok(await fleet.SuggestRegistrationAsync(typeId, ct));

    [HttpGet("aircraft/{id:guid}")]
    public async Task<ActionResult<AdminAircraftDto>> GetAircraft(Guid id, CancellationToken ct) =>
        Ok(await fleet.GetAircraftAsync(id, ct));

    [HttpPost("aircraft")]
    public async Task<ActionResult<AdminAircraftDto>> CreateAircraft(AircraftRequest request, CancellationToken ct) =>
        Ok(await fleet.CreateAircraftAsync(request, ct));

    [HttpPut("aircraft/{id:guid}")]
    public async Task<ActionResult<AdminAircraftDto>> UpdateAircraft(Guid id, AircraftRequest request, CancellationToken ct) =>
        Ok(await fleet.UpdateAircraftAsync(id, request, ct));

    [HttpDelete("aircraft/{id:guid}")]
    public async Task<IActionResult> DeleteAircraft(Guid id, CancellationToken ct)
    {
        await fleet.DeleteAircraftAsync(id, ct);
        return NoContent();
    }

    [HttpPost("aircraft/{id:guid}/photo")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<AdminAircraftDto>> UploadPhoto(Guid id, IFormFile file, [FromForm] string? credit, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var upload = new PhotoUpload(stream, file.FileName, file.ContentType, file.Length, credit);
        return Ok(await fleet.UploadPhotoAsync(id, upload, ct));
    }

    [HttpPut("aircraft/{id:guid}/photo/credit")]
    public async Task<ActionResult<AdminAircraftDto>> UpdatePhotoCredit(Guid id, PhotoCreditRequest request, CancellationToken ct) =>
        Ok(await fleet.UpdatePhotoCreditAsync(id, request.Credit, ct));

    [HttpDelete("aircraft/{id:guid}/photo")]
    public async Task<ActionResult<AdminAircraftDto>> RemovePhoto(Guid id, CancellationToken ct) =>
        Ok(await fleet.RemovePhotoAsync(id, ct));

    [HttpPost("aircraft/{id:guid}/maintenance")]
    public async Task<ActionResult<AdminAircraftDto>> SendToMaintenance(Guid id, MaintenanceRequest request, CancellationToken ct) =>
        Ok(await fleet.SendToMaintenanceAsync(id, request, ct));

    [HttpDelete("aircraft/{id:guid}/maintenance")]
    public async Task<ActionResult<AdminAircraftDto>> ReleaseFromMaintenance(Guid id, CancellationToken ct) =>
        Ok(await fleet.ReleaseFromMaintenanceAsync(id, ct));

    [HttpPut("aircraft/{id:guid}/active")]
    public async Task<ActionResult<AdminAircraftDto>> SetActive(Guid id, ActiveRequest request, CancellationToken ct) =>
        Ok(await fleet.SetActiveAsync(id, request.Active, ct));
}

public sealed record PhotoCreditRequest(string? Credit);

public sealed record ActiveRequest(bool Active);