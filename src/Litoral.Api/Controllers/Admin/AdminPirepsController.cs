using Litoral.Api.Extensions;
using Litoral.Application.Common;
using Litoral.Application.Flights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Litoral.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/pireps")]
[Authorize(Policy = StaffPolicies.ReviewPireps)]
public class AdminPirepsController(IPirepReviewService reviews) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminPirepListItemDto>>> List([FromQuery] string? status, CancellationToken ct) =>
        Ok(await reviews.ListAsync(status, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminPirepDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await reviews.GetAsync(id, ct));

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<AdminPirepDto>> AddItem(Guid id, AddReviewItemRequest request, CancellationToken ct) =>
        Ok(await reviews.AddItemAsync(id, request, ct));

    [HttpPost("{id:guid}/items/{ruleId:guid}/toggle")]
    public async Task<ActionResult<AdminPirepDto>> ToggleItem(Guid id, Guid ruleId, CancellationToken ct) =>
        Ok(await reviews.ToggleItemAsync(id, ruleId, ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<AdminPirepDto>> Approve(Guid id, ReviewDecisionRequest request, CancellationToken ct) =>
        Ok(await reviews.ApproveAsync(id, User.GetUserId(), request, ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<AdminPirepDto>> Reject(Guid id, ReviewDecisionRequest request, CancellationToken ct) =>
        Ok(await reviews.RejectAsync(id, User.GetUserId(), request, ct));
}