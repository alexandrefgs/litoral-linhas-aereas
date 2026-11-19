namespace Litoral.Acars.Core.Sim;

public static class SimulatorName
{
    public static string Describe(string? connected)
    {
        var name = (connected ?? "").Trim();
        if (name.StartsWith("MSFS", StringComparison.OrdinalIgnoreCase))
            return name.Contains("2024", StringComparison.Ordinal) ? "MSFS 2024" : "MSFS 2020";
        return name.Length == 0 ? "Unknown" : name;
    }

    public static bool IsSupported(string? simulator) => simulator is "MSFS 2020" or "MSFS 2024";
}