using Litoral.Api.Extensions;
using Litoral.Application.Pilots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profiles/{pilotNumber:int}")]
public class ProfilesController(IProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get(int pilotNumber, CancellationToken ct) =>
        Ok(await profiles.GetAsync(User.GetUserId(), pilotNumber, ct));

    [HttpGet("map")]
    public async Task<ActionResult<FlightMapDto>> Map(int pilotNumber, CancellationToken ct) =>
        Ok(await profiles.FlightMapAsync(pilotNumber, ct));

    [HttpGet("flights")]
    public async Task<ActionResult<PagedResult<ProfileFlightDto>>> Flights(int pilotNumber, [FromQuery] ProfileFlightsQuery query, CancellationToken ct) =>
        Ok(await profiles.FlightsAsync(pilotNumber, query, ct));
}