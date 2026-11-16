using Litoral.Application.Admin;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/routes")]
[Authorize(Policy = StaffPolicies.ManageRoutes)]
public class AdminRoutesController(IRouteAdminService routes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminRouteDto>>> List(CancellationToken ct) =>
        Ok(await routes.ListAsync(ct));

    [HttpGet("aircraft-types")]
    public async Task<ActionResult<IReadOnlyList<AircraftTypeDto>>> AircraftTypes(CancellationToken ct) =>
        Ok(await routes.ListAircraftTypesAsync(ct));

    [HttpGet("next-number")]
    public async Task<ActionResult<FlightNumberSuggestionDto>> NextNumber(
        [FromQuery] string departure, [FromQuery] string arrival, [FromQuery] bool cargo, CancellationToken ct) =>
        Ok(await routes.SuggestFlightNumberAsync(departure, arrival, cargo, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminRouteDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await routes.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<IReadOnlyList<AdminRouteDto>>> Create(CreateRouteRequest request, CancellationToken ct) =>
        Ok(await routes.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminRouteDto>> Update(Guid id, UpdateRouteRequest request, CancellationToken ct) =>
        Ok(await routes.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await routes.DeleteAsync(id, ct);
        return NoContent();
    }
}