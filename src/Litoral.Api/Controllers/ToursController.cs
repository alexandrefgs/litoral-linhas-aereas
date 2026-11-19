using Litoral.Api.Extensions;
using Litoral.Application.Bookings;
using Litoral.Application.Tours;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tours")]
public class ToursController(ITourService tours) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PilotTourDto>>> List(CancellationToken ct) =>
        Ok(await tours.ListAsync(User.GetUserId(), ct));

    [HttpGet("{id:guid}/aircraft")]
    public async Task<ActionResult<AircraftOptionsDto>> Aircraft(Guid id, CancellationToken ct) =>
        Ok(await tours.AircraftAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:guid}/book")]
    public async Task<ActionResult<BookingDto>> Book(Guid id, BookTourLegRequest request, CancellationToken ct) =>
        Ok(await tours.BookNextLegAsync(User.GetUserId(), id, request, ct));
}