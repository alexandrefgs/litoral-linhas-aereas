using Litoral.Api.Extensions;
using Litoral.Application.Flights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/flights")]
public class FlightsController(IFlightDetailService flights) : ControllerBase
{
    [HttpGet("recent")]
    public async Task<ActionResult<RecentFlightsDto>> Recent([FromQuery] int take = 4, CancellationToken ct = default) =>
        Ok(await flights.RecentAsync(take, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FlightDetailDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await flights.GetAsync(User.GetUserId(), id, ct));
}