using Litoral.Application.Auth;
using Litoral.Application.Pilots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PilotsController(IPilotService pilots) : ControllerBase
{
    [HttpPut("me/simbrief")]
    public async Task<ActionResult<PilotSummary>> UpdateSimBrief(UpdateSimBriefRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (userId is null)
            return Unauthorized();

        return Ok(await pilots.UpdateSimBriefAsync(userId, request, ct));
    }
}