using Litoral.Application.Admin;
using Litoral.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/airports")]
[Authorize(Policy = StaffPolicies.ManageRoutes)]
public class AdminAirportsController(IAirportAdminService airports) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminAirportDto>>> List(CancellationToken ct) =>
        Ok(await airports.ListAsync(ct));

    [HttpGet("{icao}")]
    public async Task<ActionResult<AdminAirportDto>> Get(string icao, CancellationToken ct) =>
        Ok(await airports.GetAsync(icao, ct));

    [HttpPost]
    public async Task<ActionResult<AdminAirportDto>> Create(CreateAirportRequest request, CancellationToken ct)
    {
        var airport = await airports.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { icao = airport.Icao }, airport);
    }

    [HttpPut("{icao}")]
    public async Task<ActionResult<AdminAirportDto>> Update(string icao, UpdateAirportRequest request, CancellationToken ct) =>
        Ok(await airports.UpdateAsync(icao, request, ct));

    [HttpDelete("{icao}")]
    public async Task<IActionResult> Delete(string icao, CancellationToken ct)
    {
        await airports.DeleteAsync(icao, ct);
        return NoContent();
    }
}