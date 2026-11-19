using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Tests.Sim;

public class SimulatorNameTests
{
    [Theory]
    [InlineData("MSFS", "MSFS 2020")]
    [InlineData("MSFS2024", "MSFS 2024")]
    [InlineData("msfs", "MSFS 2020")]
    [InlineData("P3D64", "P3D64")]
    [InlineData("", "Unknown")]
    [InlineData(null, "Unknown")]
    public void The_connected_simulator_gets_a_readable_name(string? connected, string expected) =>
        Assert.Equal(expected, SimulatorName.Describe(connected));

    [Theory]
    [InlineData("MSFS 2020", true)]
    [InlineData("MSFS 2024", true)]
    [InlineData("P3D64", false)]
    [InlineData("FSX", false)]
    [InlineData(null, false)]
    public void Only_msfs_is_supported_for_now(string? simulator, bool supported) =>
        Assert.Equal(supported, SimulatorName.IsSupported(simulator));
}