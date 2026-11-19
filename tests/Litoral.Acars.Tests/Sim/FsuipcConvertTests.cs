using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Tests.Sim;

public class FsuipcConvertTests
{
    private const double TwoPow32 = 4294967296d;

    [Theory]
    [InlineData(-23.4356)]
    [InlineData(45.0)]
    [InlineData(0.0)]
    public void Latitude_comes_back_in_degrees(double degrees)
    {
        var raw = (long)(degrees * 10001750d * TwoPow32 / 90d);

        Assert.InRange(FsuipcConvert.Latitude(raw), degrees - 0.000001, degrees + 0.000001);
    }

    [Theory]
    [InlineData(-46.4731)]
    [InlineData(179.5)]
    public void Longitude_comes_back_in_degrees(double degrees)
    {
        var raw = (long)(degrees * TwoPow32 * TwoPow32 / 360d);

        Assert.InRange(FsuipcConvert.Longitude(raw), degrees - 0.000001, degrees + 0.000001);
    }

    [Fact]
    public void Altitude_in_metres_is_converted_to_feet() =>
        Assert.Equal(3281, FsuipcConvert.AltitudeFt((long)(1000 * TwoPow32)));

    [Fact]
    public void Radio_altitude_is_converted_to_feet_and_never_negative()
    {
        Assert.Equal(328, FsuipcConvert.RadioAltitudeFt(100 * 65536));
        Assert.Equal(0, FsuipcConvert.RadioAltitudeFt(-5 * 65536));
    }

    [Fact]
    public void Indicated_airspeed_is_knots_times_128() =>
        Assert.Equal(150, FsuipcConvert.AirspeedKt(150 * 128));

    [Fact]
    public void Ground_speed_in_metres_per_second_is_converted_to_knots() =>
        Assert.Equal(194, FsuipcConvert.GroundSpeedKt(100 * 65536));

    [Theory]
    [InlineData(0.25, 90)]
    [InlineData(0.75, 270)]
    [InlineData(0.0, 0)]
    [InlineData(0.99999999, 0)]
    public void Heading_is_converted_to_degrees(double fraction, int expected) =>
        Assert.Equal(expected, FsuipcConvert.HeadingDeg((uint)(fraction * TwoPow32)));

    [Fact]
    public void Vertical_speed_is_converted_to_feet_per_minute()
    {
        Assert.Equal(1000, FsuipcConvert.VerticalSpeedFpm(1300));
        Assert.Equal(-1000, FsuipcConvert.VerticalSpeedFpm(-1300));
    }

    [Fact]
    public void Negative_pitch_from_fsuipc_is_nose_up() =>
        Assert.Equal(5.0, FsuipcConvert.PitchDeg(-(int)(5 / 360d * TwoPow32)));

    [Fact]
    public void Simulation_rate_256_is_normal_speed()
    {
        Assert.Equal(1.0, FsuipcConvert.SimRate(256));
        Assert.Equal(2.0, FsuipcConvert.SimRate(512));
        Assert.Equal(0.5, FsuipcConvert.SimRate(128));
    }

    [Fact]
    public void Pounds_are_converted_to_kilograms() =>
        Assert.Equal(453.59, Math.Round(FsuipcConvert.PoundsToKg(1000), 2));

    [Theory]
    [InlineData(16383, true)]
    [InlineData(10000, true)]
    [InlineData(0, false)]
    [InlineData(5000, false)]
    public void Gear_handle_down_is_the_upper_half(int raw, bool down) =>
        Assert.Equal(down, FsuipcConvert.GearDown(raw));
}