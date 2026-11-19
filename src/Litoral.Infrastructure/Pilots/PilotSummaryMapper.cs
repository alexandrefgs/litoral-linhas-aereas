using Litoral.Application.Auth;
using Litoral.Domain.Entities;
using Litoral.Infrastructure.Identity;

namespace Litoral.Infrastructure.Pilots;

internal static class PilotSummaryMapper
{
    public static PilotSummary ToSummary(ApplicationUser user, Pilot pilot, IReadOnlyList<string> roles, bool requireStaffTwoFactor) => new(
        pilot.PilotNumber,
        pilot.DisplayId,
        pilot.FirstName,
        pilot.LastName,
        user.Email!,
        pilot.Rank.Code,
        pilot.Rank.Name,
        pilot.CurrentLocationIcao,
        pilot.TotalFlightMinutes,
        pilot.Points,
        pilot.SimBriefUsername,
        roles,
        pilot.PublicName,
        pilot.PhotoUrl,
        user.TwoFactorEnabled,
        requireStaffTwoFactor && roles.Count > 0);
}