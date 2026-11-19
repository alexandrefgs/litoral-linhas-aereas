using System.Security.Claims;
using Litoral.Infrastructure.Admin;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Litoral.Infrastructure.Auth;

public sealed class StaffClaimsTransformation(
    StaffRoles staffRoles,
    LitoralDbContext db,
    IOptions<TwoFactorOptions> twoFactorOptions) : IClaimsTransformation
{
    private const string RoleClaim = "role";
    private const string RefreshedMarker = "roles_refreshed";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity
            || identity.HasClaim(c => c.Type == RefreshedMarker)
            || identity.HasClaim(c => c.Type == AcarsKeyDefaults.KeyIdClaim)
            || identity.FindFirst("sub")?.Value is not string userId)
            return principal;

        foreach (var claim in identity.FindAll(RoleClaim).ToList())
            identity.RemoveClaim(claim);

        var roles = await staffRoles.OfUserAsync(userId, CancellationToken.None);

        if (roles.Count > 0 && twoFactorOptions.Value.RequiredForStaff
            && !await db.Users.AnyAsync(u => u.Id == userId && u.TwoFactorEnabled))
            roles = [];

        foreach (var role in roles)
            identity.AddClaim(new Claim(RoleClaim, role));

        identity.AddClaim(new Claim(RefreshedMarker, "1"));
        return principal;
    }
}