using Litoral.Api.Extensions;
using Litoral.Application.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings")]
public class BookingsController(IBookingService bookings, IBriefingService briefing) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BookingOverviewDto>> Overview(CancellationToken ct) =>
        Ok(await bookings.GetOverviewAsync(User.GetUserId(), ct));

    [HttpGet("destinations")]
    public async Task<ActionResult<IReadOnlyList<DestinationDto>>> Destinations(CancellationToken ct) =>
        Ok(await bookings.ListDestinationsAsync(User.GetUserId(), ct));

    [HttpGet("routes/{routeId:guid}/aircraft")]
    public async Task<ActionResult<AircraftOptionsDto>> Aircraft(Guid routeId, CancellationToken ct) =>
        Ok(await bookings.ListAircraftAsync(User.GetUserId(), routeId, ct));

    [HttpGet("random")]
    public async Task<ActionResult<RandomProposalDto>> ProposeRandom([FromQuery] FlightDuration duration, CancellationToken ct) =>
        Ok(await bookings.ProposeRandomAsync(User.GetUserId(), duration, ct));

    [HttpPost("random")]
    public async Task<ActionResult<BookingDto>> BookRandom(BookFlightRequest request, CancellationToken ct) =>
        Ok(await bookings.BookRandomAsync(User.GetUserId(), request, ct));

    [HttpPost("custom")]
    public async Task<ActionResult<BookingDto>> BookCustom(BookFlightRequest request, CancellationToken ct) =>
        Ok(await bookings.BookCustomAsync(User.GetUserId(), request, ct));

    [HttpDelete("current")]
    public async Task<IActionResult> Cancel(CancellationToken ct)
    {
        await bookings.CancelCurrentAsync(User.GetUserId(), ct);
        return NoContent();
    }

    [HttpGet("current/briefing")]
    public async Task<ActionResult<BriefingDto>> Briefing(CancellationToken ct) =>
        Ok(await briefing.GetAsync(User.GetUserId(), ct));
    
    [HttpGet("current/weather")]
    public async Task<ActionResult<IReadOnlyList<MetarDto>>> Weather(CancellationToken ct) =>
        Ok(await briefing.GetWeatherAsync(User.GetUserId(), ct));
    
    [HttpPost("current/simbrief/generate")]
    public async Task<ActionResult<SimBriefGenerateDto>> GenerateSimBrief(CancellationToken ct) =>
        Ok(await briefing.GenerateSimBriefAsync(User.GetUserId(), ct));
    
    [HttpGet("current/simbrief/pdf")]
    public async Task<IActionResult> SimBriefPdf(CancellationToken ct) =>
        File(await briefing.GetSimBriefPdfAsync(User.GetUserId(), ct), "application/pdf");

    [HttpGet("current/simbrief")]
    public async Task<ActionResult<SimBriefOfpDto>> SimBrief(CancellationToken ct) =>
        Ok(await briefing.GetSimBriefAsync(User.GetUserId(), ct));
}