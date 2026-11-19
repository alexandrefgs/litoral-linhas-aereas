using Litoral.Application.Common;
using Litoral.Application.Downloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/downloads")]
[Authorize(Policy = StaffPolicies.ManageDownloads)]
public class AdminDownloadsController(IDownloadAdminService downloads) : ControllerBase
{
    private const long MaxRequestBytes = 21 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminDownloadDto>>> List(CancellationToken ct) =>
        Ok(await downloads.ListAsync(ct));

    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<ActionResult<AdminDownloadDto>> Create([FromForm] DownloadForm form, CancellationToken ct)
    {
        await using var stream = form.File?.OpenReadStream();
        return Ok(await downloads.CreateAsync(form.ToInput(), Upload(form.File, stream), ct));
    }

    [HttpPut("{id:guid}")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<ActionResult<AdminDownloadDto>> Update(Guid id, [FromForm] DownloadForm form, CancellationToken ct)
    {
        await using var stream = form.File?.OpenReadStream();
        return Ok(await downloads.UpdateAsync(id, form.ToInput(), Upload(form.File, stream), ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await downloads.DeleteAsync(id, ct);
        return NoContent();
    }

    private static FileUpload? Upload(IFormFile? file, Stream? stream) =>
        file is null || stream is null ? null : new FileUpload(stream, file.FileName, file.ContentType, file.Length);
}

public sealed class DownloadForm
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Category { get; set; } = "";
    public string? AircraftType { get; set; }
    public string? Version { get; set; }
    public string? ExternalUrl { get; set; }
    public IFormFile? File { get; set; }

    public DownloadInput ToInput() => new(Title, Description, Category, AircraftType, Version, ExternalUrl);
}