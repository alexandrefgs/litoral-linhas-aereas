using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Tests.Sim;

public class RawSimDataTests
{
    private const double TwoPow32 = 4294967296d;

    private static RawSimData Raw() => new(
        Latitude: (long)(-23.4356 * 10001750d * TwoPow32 / 90d),
        Longitude: (long)(-46.4731 * TwoPow32 * TwoPow32 / 360d),
        Altitude: (long)(1000 * TwoPow32),
        RadioAltitude: 300 * 65536,
        Ias: 250 * 128,
        GroundSpeed: 150 * 65536,
        Heading: (uint)(0.5 * TwoPow32),
        VerticalSpeed: -1300,
        Pitch: -(int)(3 / 360d * TwoPow32),
        FlapsHandle: 16383,
        FlapDetents: 8,
        GearHandle: 16383,
        OnGround: 0,
        ParkingBrake: 0,
        EngineCombustion: [1, 0],
        FuelPounds: 10000,
        TouchdownVerticalSpeed: -195,
        Overspeed: 1,
        Stall: 0,
        Slew: 0,
        Paused: 1,
        SimRate: 512,
        Crashed: 0);

    [Fact]
    public void Every_offset_is_converted_into_the_snapshot()
    {
        var s = Raw().ToSnapshot(TestFlights.Start);

        Assert.Equal(TestFlights.Start, s.At);
        Assert.InRange(s.Latitude, -23.43561, -23.43559);
        Assert.InRange(s.Longitude, -46.47311, -46.47309);
        Assert.Equal(3281, s.AltitudeFt);
        Assert.Equal(984, s.AglFt);
        Assert.Equal(250, s.IasKt);
        Assert.Equal(292, s.GroundSpeedKt);
        Assert.Equal(180, s.HeadingTrue);
        Assert.Equal(-1000, s.VerticalSpeedFpm);
        Assert.Equal(3.0, s.PitchDeg);
        Assert.Equal(16383, s.FlapsHandle);
        Assert.Equal(8, s.FlapDetents);
        Assert.True(s.GearDown);
        Assert.False(s.OnGround);
        Assert.False(s.ParkingBrake);
        Assert.Equal(new[] { true, false }, s.Engines);
        Assert.Equal(4535.9, s.FuelKg);
        Assert.Equal(-150, s.TouchdownRateFpm);
        Assert.True(s.Overspeed);
        Assert.False(s.Stall);
        Assert.True(s.Paused);
        Assert.Equal(2.0, s.SimRate);
        Assert.False(s.Crashed);
    }

    [Fact]
    public void Any_non_zero_flag_counts_as_set()
    {
        var s = (Raw() with { OnGround = 1, ParkingBrake = 32767, Slew = 1, Crashed = 1 }).ToSnapshot(TestFlights.Start);

        Assert.True(s.OnGround);
        Assert.True(s.ParkingBrake);
        Assert.True(s.Slew);
        Assert.True(s.Crashed);
    }

    [Fact]
    public void A_zero_simulation_rate_is_read_as_normal_speed() =>
        Assert.Equal(1.0, (Raw() with { SimRate = 0 }).ToSnapshot(TestFlights.Start).SimRate);
}