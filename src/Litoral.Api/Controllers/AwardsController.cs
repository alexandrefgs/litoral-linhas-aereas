using Litoral.Application.Awards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profiles/{pilotNumber:int}/awards")]
public class AwardsController(IAwardService awards) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PilotAwardDto>>> Get(int pilotNumber, CancellationToken ct) =>
        Ok(await awards.ForPilotAsync(pilotNumber, ct));
}