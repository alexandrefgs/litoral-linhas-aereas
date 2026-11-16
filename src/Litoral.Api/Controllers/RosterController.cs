using Litoral.Api.Extensions;
using Litoral.Application.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/roster")]
public class RosterController(IRosterService roster) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RosterStatusDto>> Get(CancellationToken ct) =>
        Ok(await roster.GetAsync(User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<RosterStatusDto>> Create(CreateRosterRequest request, CancellationToken ct) =>
        Ok(await roster.CreateAsync(User.GetUserId(), request, ct));

    [HttpGet("legs/{legId:guid}/aircraft")]
    public async Task<ActionResult<AircraftOptionsDto>> Aircraft(Guid legId, CancellationToken ct) =>
        Ok(await roster.ListLegAircraftAsync(User.GetUserId(), legId, ct));

    [HttpPost("legs/{legId:guid}/book")]
    public async Task<ActionResult<BookingDto>> Book(Guid legId, BookLegRequest request, CancellationToken ct) =>
        Ok(await roster.BookLegAsync(User.GetUserId(), legId, request, ct));
}