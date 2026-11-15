using Litoral.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "online",
        airline = Airline.Name,
        icao = Airline.Icao,
        callsign = Airline.Callsign,
        timestamp = DateTime.UtcNow
    });
}