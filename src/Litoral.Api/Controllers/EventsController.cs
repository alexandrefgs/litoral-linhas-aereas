using Litoral.Api.Extensions;
using Litoral.Application.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/events")]
public class EventsController(IEventService events) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PilotEventDto>>> List(CancellationToken ct) =>
        Ok(await events.ListAsync(User.GetUserId(), ct));
}