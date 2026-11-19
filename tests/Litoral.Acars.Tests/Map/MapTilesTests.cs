using Litoral.Acars.Core.Map;

namespace Litoral.Acars.Tests.Map;

public class MapTilesTests
{
    [Fact]
    public void With_a_key_the_carto_tiles_are_used()
    {
        Assert.Equal("https://basemaps.cartocdn.com/rastertiles/light_all/{z}/{x}/{y}.png?key=abc123", MapTiles.Light(" abc123 "));
        Assert.Equal("https://basemaps.cartocdn.com/rastertiles/dark_all/{z}/{x}/{y}.png?key=abc123", MapTiles.Dark("abc123"));
        Assert.Equal(MapTiles.Attribution, MapTiles.AttributionFor("abc123"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Without_a_key_the_map_falls_back_to_openstreetmap(string? key)
    {
        Assert.False(MapTiles.HasKey(key));
        Assert.Equal("https://tile.openstreetmap.org/{z}/{x}/{y}.png", MapTiles.Light(key));
        Assert.Equal("https://tile.openstreetmap.org/{z}/{x}/{y}.png", MapTiles.Dark(key));
        Assert.Equal(MapTiles.OsmAttribution, MapTiles.AttributionFor(key));
    }

    [Fact]
    public void The_key_is_escaped_in_the_url() =>
        Assert.EndsWith("?key=a%26b", MapTiles.Light("a&b"));
}