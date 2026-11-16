using Litoral.Api.Extensions;
using Litoral.Application.Airports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/airports")]
public class AirportsController(IAirportDirectoryService airports) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AirportDirectoryDto>> List(CancellationToken ct) =>
        Ok(await airports.ListAsync(User.GetUserId(), ct));

    [HttpGet("{icao}/aircraft")]
    public async Task<ActionResult<IReadOnlyList<AirportAircraftDto>>> Aircraft(string icao, CancellationToken ct) =>
        Ok(await airports.AircraftAsync(icao, ct));

    [HttpGet("{icao}/departures")]
    public async Task<ActionResult<IReadOnlyList<AirportDepartureDto>>> Departures(string icao, CancellationToken ct) =>
        Ok(await airports.DeparturesAsync(User.GetUserId(), icao, ct));

    [HttpPost("{icao}/jumpseat")]
    public async Task<ActionResult<JumpseatResultDto>> Jumpseat(string icao, CancellationToken ct) =>
        Ok(await airports.JumpseatAsync(User.GetUserId(), icao, ct));
}