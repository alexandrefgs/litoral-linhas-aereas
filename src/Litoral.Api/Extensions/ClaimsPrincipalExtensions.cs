using System.Security.Claims;
using Litoral.Application.Common;

namespace Litoral.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirst("sub")?.Value
        ?? throw new AppException(AppErrorKind.Unauthorized, "You need to sign in again.");
}