using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Admin;

public sealed class StaffRoles(LitoralDbContext db)
{
    public async Task<Dictionary<string, List<string>>> ByUserAsync(CancellationToken ct)
    {
        var pairs = await (from userRole in db.UserRoles
                           join role in db.Roles on userRole.RoleId equals role.Id
                           select new { userRole.UserId, role.Name })
            .ToListAsync(ct);

        return pairs
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Name!).ToList());
    }

    public async Task<List<string>> OfUserAsync(string userId, CancellationToken ct) =>
        await (from userRole in db.UserRoles
               join role in db.Roles on userRole.RoleId equals role.Id
               where userRole.UserId == userId
               select role.Name!)
            .ToListAsync(ct);
}