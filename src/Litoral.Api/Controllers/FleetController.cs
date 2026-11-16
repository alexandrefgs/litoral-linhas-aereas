using Litoral.Api.Extensions;
using Litoral.Application.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/fleet")]
public class FleetController(IPilotFleetService fleet) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FleetAircraftDto>>> List(CancellationToken ct) =>
        Ok(await fleet.ListAsync(ct));

    [HttpGet("{id:guid}/transfer")]
    public async Task<ActionResult<TransferQuoteDto>> Quote(Guid id, CancellationToken ct) =>
        Ok(await fleet.QuoteTransferAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:guid}/transfer")]
    public async Task<ActionResult<FleetAircraftDto>> Transfer(Guid id, CancellationToken ct) =>
        Ok(await fleet.TransferAsync(User.GetUserId(), id, ct));
}