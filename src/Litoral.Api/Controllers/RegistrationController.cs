using Litoral.Application.Applications;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers;

[ApiController]
[Route("api/registration")]
public class RegistrationController(IApplicationService applications) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RegistrationStatusDto>> Status(CancellationToken ct) =>
        Ok(await applications.GetRegistrationStatusAsync(ct));
}