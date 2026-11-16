using Litoral.Application.Auth;
using Litoral.Domain.Entities;

namespace Litoral.Infrastructure.Pilots;

internal static class PilotSummaryMapper
{
    public static PilotSummary ToSummary(string email, Pilot pilot, IReadOnlyList<string> roles) => new(
        pilot.PilotNumber,
        pilot.DisplayId,
        pilot.FirstName,
        pilot.LastName,
        email,
        pilot.Rank.Code,
        pilot.Rank.Name,
        pilot.CurrentLocationIcao,
        pilot.TotalFlightMinutes,
        pilot.Points,
        pilot.SimBriefUsername,
        roles);
}