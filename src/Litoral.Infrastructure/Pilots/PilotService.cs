using Litoral.Application.Auth;
using Litoral.Application.Common;
using Litoral.Application.Pilots;
using Litoral.Infrastructure.Identity;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Pilots;

public sealed class PilotService(LitoralDbContext db, UserManager<ApplicationUser> userManager) : IPilotService
{
    public async Task<PilotSummary> UpdateSimBriefAsync(string userId, UpdateSimBriefRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new AppException(AppErrorKind.NotFound, "Usuário não encontrado.");

        var pilot = await db.Pilots.Include(p => p.Rank).FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new AppException(AppErrorKind.NotFound, "Piloto não encontrado.");

        pilot.SetSimBriefUsername(request.SimBriefUsername);
        await db.SaveChangesAsync(ct);

        var roles = await userManager.GetRolesAsync(user);
        return PilotSummaryMapper.ToSummary(user.Email!, pilot, roles.ToList());
    }
}