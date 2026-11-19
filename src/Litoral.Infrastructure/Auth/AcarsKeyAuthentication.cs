using System.Security.Claims;
using System.Text.Encodings.Web;
using Litoral.Application.Admin;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;
using Litoral.Infrastructure.Audit;
using Litoral.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Litoral.Infrastructure.Auth;

public static class AcarsKeyDefaults
{
    public const string Scheme = "AcarsKey";
    public const string Header = "X-Acars-Key";
    public const string KeyIdClaim = "acars_key";
    public const string Bearer = "Bearer";
    public const string AcarsSchemes = Bearer + "," + Scheme;
}

public sealed class AcarsKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    LitoralDbContext db,
    IAuditLog auditLog) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AcarsKeyDefaults.Header, out var values))
            return AuthenticateResult.NoResult();

        var key = values.ToString().Trim();
        if (!AcarsKey.LooksLikeKey(key))
            return AuthenticateResult.Fail("Invalid ACARS key.");

        var hash = AcarsKey.HashOf(key);
        var entry = await db.AcarsKeys
            .Include(k => k.Pilot)
            .FirstOrDefaultAsync(k => k.Hash == hash && k.RevokedAt == null, Context.RequestAborted);

        if (entry is null || !entry.Pilot.IsActive)
            return AuthenticateResult.Fail("This ACARS key is not valid anymore.");

        var firstUse = entry.LastUsedAt is null;
        if (entry.MarkUsed(DateTime.UtcNow))
            await db.SaveChangesAsync(Context.RequestAborted);

        if (firstUse)
            await auditLog.WriteAsync(new AuditEntry(AuditCategory.Account, "ACARS key first used", "Pilot", entry.PilotId.ToString(),
                AuditContext.PilotLabel(entry.Pilot), $"Key: {entry.Name} ({entry.Prefix}…)",
                entry.Pilot.UserId, AuditContext.PilotLabel(entry.Pilot)), Context.RequestAborted);

        var identity = new ClaimsIdentity(
            [new Claim("sub", entry.Pilot.UserId), new Claim(AcarsKeyDefaults.KeyIdClaim, entry.Id.ToString())],
            Scheme.Name, "sub", "role");

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}