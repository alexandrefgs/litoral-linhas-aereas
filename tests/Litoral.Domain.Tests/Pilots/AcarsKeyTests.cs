using Litoral.Domain.Common;
using Litoral.Domain.Entities;

namespace Litoral.Domain.Tests.Pilots;

public class AcarsKeyTests
{
    private static readonly DateTime Now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    private static Pilot NewPilot() => new("user-1", 1000, "Test", "Pilot", new Rank("FO", "First Officer", 1, 0, 0), "SBGR");

    [Fact]
    public void A_new_key_has_the_litoral_prefix_and_64_hex_characters()
    {
        var (_, key) = AcarsKey.Create(NewPilot(), "Home PC");

        Assert.StartsWith(AcarsKey.KeyPrefix, key);
        Assert.Equal(AcarsKey.KeyPrefix.Length + 64, key.Length);
        Assert.Matches("^ltl_[0-9a-f]{64}$", key);
    }

    [Fact]
    public void Only_the_hash_and_the_prefix_are_stored()
    {
        var (entity, key) = AcarsKey.Create(NewPilot(), "Home PC");

        Assert.Equal(AcarsKey.HashOf(key), entity.Hash);
        Assert.NotEqual(key, entity.Hash);
        Assert.Equal(key[..AcarsKey.PrefixLength], entity.Prefix);
        Assert.DoesNotContain(key[AcarsKey.PrefixLength..], entity.Hash);
    }

    [Fact]
    public void Every_key_is_different()
    {
        var pilot = NewPilot();
        var keys = Enumerable.Range(0, 50).Select(_ => AcarsKey.Create(pilot, "PC").Key).ToHashSet();

        Assert.Equal(50, keys.Count);
    }

    [Fact]
    public void The_hash_is_deterministic()
    {
        Assert.Equal(AcarsKey.HashOf("ltl_abc"), AcarsKey.HashOf("ltl_abc"));
        Assert.NotEqual(AcarsKey.HashOf("ltl_abc"), AcarsKey.HashOf("ltl_abd"));
    }

    [Fact]
    public void The_name_is_trimmed()
    {
        var (entity, _) = AcarsKey.Create(NewPilot(), "  Laptop  ");

        Assert.Equal("Laptop", entity.Name);
    }

    [Theory]
    [InlineData((string?)null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("This name is far too long to be accepted as a key name")]
    public void An_invalid_name_is_refused(string? name)
    {
        Assert.Throws<DomainException>(() => AcarsKey.Create(NewPilot(), name));
    }

    [Theory]
    [InlineData("ltl_0123456789abcdef", true)]
    [InlineData("ltl_0123", false)]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.payload.sig", false)]
    [InlineData(null, false)]
    public void LooksLikeKey_recognizes_the_format(string? value, bool expected)
    {
        Assert.Equal(expected, AcarsKey.LooksLikeKey(value));
    }

    [Fact]
    public void The_first_use_is_always_recorded()
    {
        var (entity, _) = AcarsKey.Create(NewPilot(), "PC");

        Assert.True(entity.MarkUsed(Now));
        Assert.Equal(Now, entity.LastUsedAt);
    }

    [Fact]
    public void Usage_is_refreshed_only_after_the_interval()
    {
        var (entity, _) = AcarsKey.Create(NewPilot(), "PC");
        entity.MarkUsed(Now);

        Assert.False(entity.MarkUsed(Now.AddMinutes(1)));
        Assert.Equal(Now, entity.LastUsedAt);

        Assert.True(entity.MarkUsed(Now + AcarsKey.UsageRefresh));
        Assert.Equal(Now + AcarsKey.UsageRefresh, entity.LastUsedAt);
    }

    [Fact]
    public void A_revoked_key_is_inactive_and_cant_be_revoked_again()
    {
        var (entity, _) = AcarsKey.Create(NewPilot(), "PC");

        entity.Revoke(Now);

        Assert.False(entity.IsActive);
        Assert.Equal(Now, entity.RevokedAt);
        Assert.Throws<DomainException>(() => entity.Revoke(Now));
    }
}