using Litoral.Application.Downloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/downloads")]
public class DownloadsController(IDownloadService downloads) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DownloadDto>>> List(CancellationToken ct) =>
        Ok(await downloads.ListAsync(ct));

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> File(Guid id, CancellationToken ct)
    {
        var file = await downloads.OpenAsync(id, ct);
        return PhysicalFile(file.Path, file.ContentType, file.FileName);
    }

    [HttpPost("{id:guid}/link")]
    public async Task<ActionResult<DownloadLinkResponse>> Link(Guid id, CancellationToken ct) =>
        Ok(new DownloadLinkResponse(await downloads.OpenLinkAsync(id, ct)));
}

public sealed record DownloadLinkResponse(string Url);