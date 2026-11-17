using Litoral.Api.Extensions;
using Litoral.Application.Admin;
using Litoral.Application.Pilots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pilots/me")]
public class PilotsController(IPilotSettingsService settings, IPointsStatementService points) : ControllerBase
{
    private const int MaxUploadBytes = 6 * 1024 * 1024;

    [HttpGet("settings")]
    public async Task<ActionResult<PilotSettingsDto>> Get(CancellationToken ct) =>
        Ok(await settings.GetAsync(User.GetUserId(), ct));
    
    [HttpGet("points")]
    public async Task<ActionResult<PointsStatementDto>> Points(
        [FromQuery] string? group,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await points.GetAsync(User.GetUserId(), group, page, pageSize, ct));

    [HttpPut("settings/personal")]
    public async Task<ActionResult<PilotSettingsDto>> UpdatePersonal(UpdatePersonalRequest request, CancellationToken ct) =>
        Ok(await settings.UpdatePersonalAsync(User.GetUserId(), request, ct));

    [HttpPut("settings/config")]
    public async Task<ActionResult<PilotSettingsDto>> UpdateConfig(UpdateConfigRequest request, CancellationToken ct) =>
        Ok(await settings.UpdateConfigAsync(User.GetUserId(), request, ct));

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await settings.ChangePasswordAsync(User.GetUserId(), request, ct);
        return NoContent();
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request, CancellationToken ct)
    {
        await settings.DeleteAccountAsync(User.GetUserId(), request, ct);
        return NoContent();
    }

    [HttpPost("photo")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<PilotSettingsDto>> UploadPhoto(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await settings.UploadPhotoAsync(User.GetUserId(), new PhotoUpload(stream, file.FileName, file.ContentType, file.Length, null), ct));
    }

    [HttpDelete("photo")]
    public async Task<ActionResult<PilotSettingsDto>> RemovePhoto(CancellationToken ct) =>
        Ok(await settings.RemovePhotoAsync(User.GetUserId(), ct));

    [HttpPost("cover")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<PilotSettingsDto>> UploadCover(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await settings.UploadCoverAsync(User.GetUserId(), new PhotoUpload(stream, file.FileName, file.ContentType, file.Length, null), ct));
    }

    [HttpDelete("cover")]
    public async Task<ActionResult<PilotSettingsDto>> RemoveCover(CancellationToken ct) =>
        Ok(await settings.RemoveCoverAsync(User.GetUserId(), ct));
}