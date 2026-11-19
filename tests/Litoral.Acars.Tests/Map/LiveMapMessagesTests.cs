using System.Text.Json;
using Litoral.Acars.Core.Api;
using Litoral.Acars.Core.Map;

namespace Litoral.Acars.Tests.Map;

public class LiveMapMessagesTests
{
    private static LiveFlight Flight(string callsign = "LTL1007", int heading = 90, int progress = 40) => new(
        Guid.NewGuid(), callsign, "FO1000", "A. Fagundes", "PS-LAB", "B738", "Boeing 737-800", false,
        new LiveAirport("SBPA", "Porto Alegre", -29.99, -51.17),
        new LiveAirport("SBGR", "São Paulo", -23.43, -46.47),
        "Ivao", -27.1, -49.2, 37000, 450, heading, "Cruise", progress, 42);

    [Fact]
    public void Flights_are_sent_with_type_me_and_the_map_fields()
    {
        using var doc = JsonDocument.Parse(LiveMapMessages.Flights([Flight()], " ltl1007 "));
        var root = doc.RootElement;
        var flight = root.GetProperty("flights")[0];

        Assert.Equal("flights", root.GetProperty("type").GetString());
        Assert.Equal("LTL1007", root.GetProperty("me").GetString());
        Assert.Equal("LTL1007", flight.GetProperty("callsign").GetString());
        Assert.Equal("FO1000 · A. Fagundes", flight.GetProperty("pilot").GetString());
        Assert.Equal("PS-LAB · Boeing 737-800", flight.GetProperty("aircraft").GetString());
        Assert.Equal("SBPA", flight.GetProperty("dep").GetProperty("icao").GetString());
        Assert.Equal(-23.43, flight.GetProperty("arr").GetProperty("lat").GetDouble());
        Assert.Equal(37000, flight.GetProperty("alt").GetInt32());
        Assert.Equal("Cruise", flight.GetProperty("phase").GetString());
        Assert.Equal(42, flight.GetProperty("eta").GetInt32());
    }

    [Fact]
    public void Without_my_callsign_me_is_null()
    {
        using var doc = JsonDocument.Parse(LiveMapMessages.Flights([], null));

        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("me").ValueKind);
        Assert.Equal(0, doc.RootElement.GetProperty("flights").GetArrayLength());
    }

    [Theory]
    [InlineData(-90, 270)]
    [InlineData(360, 0)]
    [InlineData(725, 5)]
    public void Heading_is_kept_between_0_and_359(int heading, int expected)
    {
        using var doc = JsonDocument.Parse(LiveMapMessages.Flights([Flight(heading: heading)], null));

        Assert.Equal(expected, doc.RootElement.GetProperty("flights")[0].GetProperty("hdg").GetInt32());
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(130, 100)]
    public void Progress_is_kept_between_0_and_100(int progress, int expected)
    {
        using var doc = JsonDocument.Parse(LiveMapMessages.Flights([Flight(progress: progress)], null));

        Assert.Equal(expected, doc.RootElement.GetProperty("flights")[0].GetProperty("progress").GetInt32());
    }

    [Fact]
    public void The_theme_message_says_if_it_is_dark()
    {
        using var doc = JsonDocument.Parse(LiveMapMessages.Theme(true));

        Assert.Equal("theme", doc.RootElement.GetProperty("type").GetString());
        Assert.True(doc.RootElement.GetProperty("dark").GetBoolean());
    }
}