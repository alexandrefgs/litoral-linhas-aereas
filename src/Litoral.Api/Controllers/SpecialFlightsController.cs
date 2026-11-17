using Litoral.Api.Extensions;
using Litoral.Application.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/special-flights")]
public class SpecialFlightsController(ISpecialFlightService flights) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PilotSpecialFlightDto>>> List(CancellationToken ct) =>
        Ok(await flights.ListAsync(User.GetUserId(), ct));

    [HttpPost("{id:guid}/take")]
    public async Task<ActionResult<PilotSpecialFlightDto>> Take(Guid id, CancellationToken ct) =>
        Ok(await flights.TakeAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:guid}/next-leg")]
    public async Task<ActionResult<PilotSpecialFlightDto>> BookNextLeg(Guid id, CancellationToken ct) =>
        Ok(await flights.BookNextLegAsync(User.GetUserId(), id, ct));
}