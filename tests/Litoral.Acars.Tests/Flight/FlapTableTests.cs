using Litoral.Acars.Core.Flight;

namespace Litoral.Acars.Tests.Flight;

public class FlapTableTests
{
    [Theory]
    [InlineData("B738", 0, 0)]
    [InlineData("B738", 2048, 1)]
    [InlineData("B738", 6144, 5)]
    [InlineData("B738", 12288, 25)]
    [InlineData("B738", 16383, 40)]
    [InlineData("B38M", 14336, 30)]
    [InlineData("B77W", 0, 0)]
    [InlineData("B77W", 2731, 1)]
    [InlineData("B77W", 8192, 15)]
    [InlineData("B77F", 13653, 25)]
    [InlineData("B77F", 16383, 30)]
    public void Handle_position_becomes_flap_degrees(string type, int handle, int degrees) =>
        Assert.Equal(degrees, FlapTable.ToDegrees(type, handle));

    [Fact]
    public void A_handle_between_detents_goes_to_the_closest_one()
    {
        Assert.Equal(1, FlapTable.ToDegrees("B738", 2500));
        Assert.Equal(2, FlapTable.ToDegrees("B738", 3500));
    }

    [Fact]
    public void Out_of_range_values_are_clamped()
    {
        Assert.Equal(0, FlapTable.ToDegrees("B738", -50));
        Assert.Equal(40, FlapTable.ToDegrees("B738", 20000));
    }

    [Fact]
    public void An_unknown_type_has_no_flap_reading()
    {
        Assert.Null(FlapTable.ToDegrees("A320", 8192));
        Assert.Null(FlapTable.ToDegrees(null, 8192));
    }

    [Fact]
    public void A_detent_count_that_does_not_match_the_type_is_not_trusted()
    {
        Assert.Null(FlapTable.ToDegrees("B738", 8192, simDetents: 6));
        Assert.Equal(10, FlapTable.ToDegrees("B738", 8192, simDetents: 8));
        Assert.Equal(15, FlapTable.ToDegrees("B77W", 8192, simDetents: 6));
    }

    [Fact]
    public void Type_codes_ignore_case_and_spaces() =>
        Assert.Equal(40, FlapTable.ToDegrees(" b738 ", 16383));

    [Theory]
    [InlineData("B738", 0)]
    [InlineData("B738", 1)]
    [InlineData("B738", 15)]
    [InlineData("B738", 40)]
    [InlineData("B77W", 5)]
    [InlineData("B77W", 30)]
    public void Handle_and_degrees_round_trip(string type, int degrees) =>
        Assert.Equal(degrees, FlapTable.ToDegrees(type, FlapTable.ToHandle(type, degrees)));

    [Fact]
    public void Asking_for_a_flap_setting_the_type_does_not_have_throws() =>
        Assert.Throws<ArgumentException>(() => FlapTable.ToHandle("B77W", 40));

    [Fact]
    public void Detents_do_not_count_flaps_up()
    {
        Assert.Equal(8, FlapTable.Detents("B738"));
        Assert.Equal(6, FlapTable.Detents("B77W"));
        Assert.Equal(0, FlapTable.Detents("A320"));
    }
}