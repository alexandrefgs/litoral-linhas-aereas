using Litoral.Application.Common;
using Litoral.Application.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/privacy-requests")]
[Authorize(Policy = StaffPolicies.ManagePilots)]
public class AdminPrivacyController(IPrivacyService privacy) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminPrivacyRequestDto>>> List([FromQuery] bool openOnly, CancellationToken ct) =>
        Ok(await privacy.ListAsync(openOnly, ct));

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<AdminPrivacyRequestDto>> Resolve(Guid id, ResolvePrivacyRequest request, CancellationToken ct) =>
        Ok(await privacy.ResolveAsync(id, request, ct));
}