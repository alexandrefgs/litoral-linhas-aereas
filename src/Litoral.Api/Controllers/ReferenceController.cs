using Litoral.Application.Reference;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
public class ReferenceController(IReferenceService reference) : ControllerBase
{
    [HttpGet("api/hubs")]
    public async Task<ActionResult<IReadOnlyList<HubDto>>> GetHubs(CancellationToken ct) =>
        Ok(await reference.GetHubsAsync(ct));

    [HttpGet("api/ranks")]
    public async Task<ActionResult<IReadOnlyList<RankDto>>> GetRanks(CancellationToken ct) =>
        Ok(await reference.GetRanksAsync(ct));

    [HttpGet("api/airports/{icao}")]
    public async Task<ActionResult<AirportDto>> GetAirport(string icao, CancellationToken ct)
    {
        var airport = await reference.GetAirportAsync(icao, ct);
        return airport is null ? NotFound() : Ok(airport);
    }

    [HttpGet("api/public/stats")]
    public async Task<ActionResult<PublicStatsDto>> GetPublicStats(CancellationToken ct) =>
        Ok(await reference.GetPublicStatsAsync(ct));

    [HttpGet("api/public/staff")]
    public async Task<ActionResult<IReadOnlyList<PublicStaffPositionDto>>> GetStaff(CancellationToken ct) =>
        Ok(await reference.GetStaffAsync(ct));

    [HttpGet("api/public/career")]
    public async Task<ActionResult<IReadOnlyList<CareerRankDto>>> GetCareer(CancellationToken ct) =>
        Ok(await reference.GetCareerAsync(ct));
}