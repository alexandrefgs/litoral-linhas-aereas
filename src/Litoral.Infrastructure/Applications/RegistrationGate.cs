using System.Security.Cryptography;
using System.Text;
using Litoral.Domain.Entities;
using Litoral.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Litoral.Infrastructure.Applications;

public sealed class RegistrationGate(LitoralDbContext db)
{
    public static string HashEmail(string email) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToUpperInvariant())));

    public async Task<bool> IsOpenAsync(CancellationToken ct)
    {
        var value = await db.SystemSettings.AsNoTracking()
            .Where(s => s.Key == SystemSetting.RegistrationOpen)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        return value is null || value == "true";
    }

    public async Task SetOpenAsync(bool open, CancellationToken ct)
    {
        var value = open ? "true" : "false";
        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == SystemSetting.RegistrationOpen, ct);

        if (setting is null)
            db.SystemSettings.Add(new SystemSetting(SystemSetting.RegistrationOpen, value));
        else
            setting.Set(value);

        await db.SaveChangesAsync(ct);
    }

    public async Task<DateTime?> BlockedUntilAsync(string email, CancellationToken ct)
    {
        var hash = HashEmail(email);
        var now = DateTime.UtcNow;

        return await db.RegistrationBlocks.AsNoTracking()
            .Where(b => b.EmailHash == hash && b.BlockedUntil > now)
            .OrderByDescending(b => b.BlockedUntil)
            .Select(b => (DateTime?)b.BlockedUntil)
            .FirstOrDefaultAsync(ct);
    }

    public void Block(string email) => db.RegistrationBlocks.Add(new RegistrationBlock(HashEmail(email)));
}