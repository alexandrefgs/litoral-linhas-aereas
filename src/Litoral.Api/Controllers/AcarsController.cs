using Litoral.Api.Extensions;
using Litoral.Application.Bookings;
using Litoral.Application.Flights;
using Litoral.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AcarsKeyDefaults.AcarsSchemes)]
[Route("api/acars")]
public class AcarsController(IFlightTrackingService tracking, IBriefingService briefing) : ControllerBase
{
    [HttpGet("live")]
    public async Task<ActionResult<IReadOnlyList<LiveFlightDto>>> Live(CancellationToken ct) =>
        Ok(await tracking.LiveAsync(ct));
    
    [HttpGet("live/{sessionId:guid}/track")]
    public async Task<ActionResult<IReadOnlyList<TrackPointDto>>> LiveTrack(Guid sessionId, CancellationToken ct) =>
        Ok(await tracking.LiveTrackAsync(sessionId, ct));

    [HttpGet("booking")]
    public async Task<ActionResult<AcarsBookingDto?>> Booking(CancellationToken ct) =>
        Ok(await tracking.GetBookingAsync(User.GetUserId(), ct));

    [HttpGet("briefing")]
    public async Task<ActionResult<BriefingDto>> Briefing(CancellationToken ct) =>
        Ok(await briefing.GetAsync(User.GetUserId(), ct));

    [HttpGet("briefing/ofp")]
    public async Task<ActionResult<SimBriefOfpDto>> Ofp(CancellationToken ct) =>
        Ok(await briefing.GetSimBriefAsync(User.GetUserId(), ct));

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