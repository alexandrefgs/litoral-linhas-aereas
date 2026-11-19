using Litoral.Application.Mel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mel")]
public class MelController(IMelService mel) : ControllerBase
{
    [HttpGet("aircraft")]
    public async Task<ActionResult<IReadOnlyList<AircraftMelDto>>> ForAircraft([FromQuery] Guid[] ids, CancellationToken ct) =>
        Ok(await mel.ForAircraftAsync(ids, ct));
}