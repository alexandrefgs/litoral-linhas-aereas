using Litoral.Api.Extensions;
using Litoral.Application.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account/privacy")]
public class PrivacyController(IPrivacyService privacy) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PrivacyOverview>> Overview(CancellationToken ct) =>
        Ok(await privacy.GetOverviewAsync(User.GetUserId(), ct));

    [HttpPost("terms/accept")]
    public async Task<IActionResult> AcceptTerms(CancellationToken ct)
    {
        await privacy.AcceptTermsAsync(User.GetUserId(), ct);
        return NoContent();
    }

    [HttpPost("requests")]
    public async Task<ActionResult<PrivacyRequestDto>> CreateRequest(CreatePrivacyRequest request, CancellationToken ct) =>
        Ok(await privacy.CreateRequestAsync(User.GetUserId(), request, ct));

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var export = await privacy.ExportAsync(User.GetUserId(), ct);
        return File(export.Content, "application/json", export.FileName);
    }
}