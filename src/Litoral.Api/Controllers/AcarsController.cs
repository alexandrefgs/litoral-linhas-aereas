using Litoral.Api.Extensions;
using Litoral.Application.Flights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/acars")]
public class AcarsController(IFlightTrackingService tracking) : ControllerBase
{
    [HttpGet("live")]
    public async Task<ActionResult<IReadOnlyList<LiveFlightDto>>> Live(CancellationToken ct) =>
        Ok(await tracking.LiveAsync(ct));

    [HttpGet("flight")]
    public async Task<ActionResult<FlightSessionDto?>> Active(CancellationToken ct) =>
        Ok(await tracking.GetActiveAsync(User.GetUserId(), ct));

    [HttpPost("flight")]
    public async Task<ActionResult<FlightSessionDto>> Start(StartFlightRequest request, CancellationToken ct) =>
        Ok(await tracking.StartAsync(User.GetUserId(), request, ct));

    [HttpPost("flight/positions")]
    public async Task<ActionResult<int>> Positions(PositionBatchRequest request, CancellationToken ct) =>
        Ok(await tracking.ReportPositionsAsync(User.GetUserId(), request, ct));

    [HttpPost("flight/complete")]
    public async Task<ActionResult<PirepDto>> Complete(CompleteFlightRequest request, CancellationToken ct) =>
        Ok(await tracking.CompleteAsync(User.GetUserId(), request, ct));
}