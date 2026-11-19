using System.Security.Cryptography;
using System.Text;
using Litoral.Domain.Common;

namespace Litoral.Domain.Entities;

public sealed class AcarsKey : Entity
{
    public const string KeyPrefix = "ltl_";
    public const int MaxPerPilot = 3;
    public const int MaxNameLength = 40;
    public const int PrefixLength = 12;
    public static readonly TimeSpan UsageRefresh = TimeSpan.FromMinutes(5);

    private const int SecretBytes = 32;

    public Guid PilotId { get; private set; }
    public Pilot Pilot { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Prefix { get; private set; } = null!;
    public string Hash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    private AcarsKey() { }

    private AcarsKey(Pilot pilot, string name, string key)
    {
        Pilot = pilot;
        PilotId = pilot.Id;
        Name = name;
        Prefix = key[..PrefixLength];
        Hash = HashOf(key);
    }

    public static (AcarsKey Entity, string Key) Create(Pilot pilot, string? name)
    {
        var clean = (name ?? "").Trim();
        if (clean.Length == 0 || clean.Length > MaxNameLength)
            throw new DomainException($"Give the key a name with up to {MaxNameLength} characters, e.g. Home PC.");

        var key = KeyPrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(SecretBytes)).ToLowerInvariant();
        return (new AcarsKey(pilot, clean, key), key);
    }

    public static bool LooksLikeKey(string? value) =>
        value is { Length: > PrefixLength } && value.StartsWith(KeyPrefix, StringComparison.Ordinal);

    public static string HashOf(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();

    public bool MarkUsed(DateTime now)
    {
        if (LastUsedAt is DateTime last && now - last < UsageRefresh) return false;
        LastUsedAt = now;
        return true;
    }

    public void Revoke(DateTime now)
    {
        if (!IsActive) throw new DomainException("This key was already revoked.");
        RevokedAt = now;
    }
}