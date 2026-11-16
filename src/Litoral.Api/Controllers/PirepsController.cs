using Litoral.Api.Extensions;
using Litoral.Application.Flights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pireps")]
public class PirepsController(IPirepService pireps, IFlightSimulator simulator, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PirepSummaryDto>>> List(CancellationToken ct) =>
        Ok(await pireps.ListMineAsync(User.GetUserId(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PirepDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await pireps.GetMineAsync(User.GetUserId(), id, ct));

    [HttpPost("manual")]
    public async Task<ActionResult<PirepDto>> Manual(ManualPirepRequest request, CancellationToken ct) =>
        Ok(await pireps.FileManualAsync(User.GetUserId(), request, ct));

    [HttpPost("simulate")]
    public async Task<ActionResult<PirepDto>> Simulate(SimulateFlightRequest request, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return NotFound();
        return Ok(await simulator.SimulateAsync(User.GetUserId(), request, ct));
    }
}