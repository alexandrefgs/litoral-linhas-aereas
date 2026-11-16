using System.ComponentModel.DataAnnotations;
using Litoral.Application.Auth;

namespace Litoral.Application.Pilots;

public sealed record UpdateSimBriefRequest([MaxLength(60)] string? SimBriefUsername);

public interface IPilotService
{
    Task<PilotSummary> UpdateSimBriefAsync(string userId, UpdateSimBriefRequest request, CancellationToken ct);
}