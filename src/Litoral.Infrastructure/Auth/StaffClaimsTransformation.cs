using System.Security.Claims;
using Litoral.Infrastructure.Admin;
using Microsoft.AspNetCore.Authentication;

namespace Litoral.Infrastructure.Auth;

public sealed class StaffClaimsTransformation(StaffRoles staffRoles) : IClaimsTransformation
{
    private const string RoleClaim = "role";
    private const string RefreshedMarker = "roles_refreshed";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity
            || identity.HasClaim(c => c.Type == RefreshedMarker)
            || identity.FindFirst("sub")?.Value is not string userId)
            return principal;

        foreach (var claim in identity.FindAll(RoleClaim).ToList())
            identity.RemoveClaim(claim);

        foreach (var role in await staffRoles.OfUserAsync(userId, CancellationToken.None))
            identity.AddClaim(new Claim(RoleClaim, role));

        identity.AddClaim(new Claim(RefreshedMarker, "1"));
        return principal;
    }
}