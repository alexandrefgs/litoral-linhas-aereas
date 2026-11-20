using Litoral.Domain.Common;
using Litoral.Domain.Entities;
using Litoral.Domain.Enums;

namespace Litoral.Domain.Tests.Fleet;

public class AircraftSimBriefTests
{
    private static Aircraft NewAircraft() => new(
        "PS-LAA",
        new AircraftType("B738", "Boeing 737-800", false, 2900, "LA"),
        new Airport("SBFL", "Hercílio Luz", "Florianópolis", "Brazil", -27.67, -48.55, OperationType.Passenger));

    [Fact]
    public void Without_an_airframe_the_icao_type_is_used()
    {
        var aircraft = NewAircraft();

        Assert.Null(aircraft.SimBriefAirframeId);
        Assert.Equal("B738", aircraft.SimBriefType);
    }

    [Fact]
    public void A_saved_airframe_replaces_the_type()
    {
        var aircraft = NewAircraft();

        aircraft.SetSimBriefAirframe("  123456_1582090020 ");

        Assert.Equal("123456_1582090020", aircraft.SimBriefAirframeId);
        Assert.Equal("123456_1582090020", aircraft.SimBriefType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_airframe_clears_it(string? value)
    {
        var aircraft = NewAircraft();
        aircraft.SetSimBriefAirframe("123456_1582090020");

        aircraft.SetSimBriefAirframe(value);

        Assert.Null(aircraft.SimBriefAirframeId);
        Assert.Equal("B738", aircraft.SimBriefType);
    }

    [Theory]
    [InlineData("B738")]
    [InlineData("123456")]
    [InlineData("123456-1582090020")]
    [InlineData("abc_1582090020")]
    [InlineData("123456_15820")]
    public void An_invalid_airframe_is_rejected(string value) =>
        Assert.Throws<DomainException>(() => NewAircraft().SetSimBriefAirframe(value));
}