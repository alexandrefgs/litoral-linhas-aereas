using Litoral.Acars.Core.Api;

namespace Litoral.Acars.Tests.Api;

public class ServerAddressTests
{
    [Theory]
    [InlineData("https://litoral.example.com", "https://litoral.example.com/")]
    [InlineData("  https://litoral.example.com/  ", "https://litoral.example.com/")]
    [InlineData("https://example.com/litoral", "https://example.com/litoral/")]
    [InlineData("https://localhost:7112", "https://localhost:7112/")]
    [InlineData("http://localhost:5101", "http://localhost:5101/")]
    [InlineData("http://127.0.0.1:5101/", "http://127.0.0.1:5101/")]
    public void Valid_addresses_end_with_a_slash(string input, string expected)
    {
        Assert.True(ServerAddress.TryParse(input, out var address, out var error));
        Assert.Equal(expected, address.ToString());
        Assert.Equal("", error);
    }

    [Theory]
    [InlineData("http://litoral.example.com")]
    [InlineData("http://192.168.0.10:5101")]
    public void Plain_http_is_refused_outside_this_computer(string input)
    {
        Assert.False(ServerAddress.TryParse(input, out _, out var error));
        Assert.Contains("https", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("litoral.example.com")]
    [InlineData("ftp://litoral.example.com")]
    public void Anything_that_is_not_a_web_address_is_refused(string? input) =>
        Assert.False(ServerAddress.TryParse(input, out _, out _));

    [Fact]
    public void A_query_string_is_refused() =>
        Assert.False(ServerAddress.TryParse("https://litoral.example.com/?key=abc", out _, out _));

    [Fact]
    public void Loopback_addresses_are_local()
    {
        Assert.True(ServerAddress.IsLocal(new Uri("https://localhost:7112")));
        Assert.False(ServerAddress.IsLocal(new Uri("https://litoral.example.com")));
    }
}