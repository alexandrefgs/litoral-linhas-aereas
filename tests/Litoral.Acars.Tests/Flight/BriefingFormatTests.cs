using Litoral.Acars.Core.Api;
using Litoral.Acars.Core.Flight;

namespace Litoral.Acars.Tests.Flight;

public class BriefingFormatTests
{
    private static AirportWeather Weather(string icao = "SBGR", string? direction = "120", int? speed = 8, int? gust = null) =>
        new(icao, "SBGR 191800Z 12008KT CAVOK 25/15 Q1018", "VFR", 25, direction, speed, gust, "10 km", 1018, null);

    [Theory]
    [InlineData(39000, "FL390")]
    [InlineData(8000, "FL080")]
    [InlineData(37049, "FL370")]
    [InlineData(0, "—")]
    [InlineData(null, "—")]
    public void Cruise_is_shown_as_a_flight_level(int? feet, string expected) =>
        Assert.Equal(expected, BriefingFormat.Cruise(feet));

    [Theory]
    [InlineData(85, "1h25")]
    [InlineData(5, "0h05")]
    [InlineData(600, "10h00")]
    [InlineData(null, "—")]
    public void Duration_is_hours_and_minutes(int? minutes, string expected) =>
        Assert.Equal(expected, BriefingFormat.Duration(minutes));

    [Fact]
    public void Weight_uses_thousands_and_the_units()
    {
        Assert.Equal("6,240 kg", BriefingFormat.Weight(6240, "KGS"));
        Assert.Equal("6,240 kg", BriefingFormat.Weight(6240, null));
        Assert.Equal("13,700 lbs", BriefingFormat.Weight(13700, "LBS"));
        Assert.Equal("—", BriefingFormat.Weight(null, "kgs"));
    }

    [Fact]
    public void Wind_shows_direction_speed_and_gust()
    {
        Assert.Equal("120° 8 kt", BriefingFormat.Wind(Weather()));
        Assert.Equal("120° 15G27 kt", BriefingFormat.Wind(Weather(speed: 15, gust: 27)));
        Assert.Equal("VRB° 3 kt", BriefingFormat.Wind(Weather(direction: null, speed: 3)));
        Assert.Equal("—", BriefingFormat.Wind(Weather(speed: null)));
        Assert.Equal("—", BriefingFormat.Wind(null));
    }

    [Fact]
    public void The_weather_is_found_by_icao()
    {
        var briefing = new AcarsBriefing(
            new BriefingBooking(Guid.NewGuid(), "LTL1007", "LTL9000",
                new BriefingAirport("SBPA", "Salgado Filho", "Porto Alegre", "Brazil", -29.99, -51.17),
                new BriefingAirport("SBGR", "Guarulhos", "São Paulo", "Brazil", -23.43, -46.47),
                452, 85, new BriefingAircraft("PS-LAB", "B738", "Boeing 737-800"), DateTime.UtcNow),
            "alexandre", [Weather("SBPA"), Weather("SBGR")],
            new CrewMember("FO1000", "A. Fagundes", "FO"), new CrewMember(null, "Severino", "CMD"),
            "OPR/LITORAL PIC/FO1000 CS/LITORAL");

        Assert.Equal("SBGR", BriefingFormat.WeatherFor(briefing, "sbgr")!.Icao);
        Assert.Null(BriefingFormat.WeatherFor(briefing, "SBFL"));
        Assert.Null(BriefingFormat.WeatherFor(null, "SBGR"));
    }
}