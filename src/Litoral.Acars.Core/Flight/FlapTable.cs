using Litoral.Acars.Core.Sim;

namespace Litoral.Acars.Core.Flight;

public static class FlapTable
{
    private static readonly int[] Boeing737 = [0, 1, 2, 5, 10, 15, 25, 30, 40];
    private static readonly int[] Boeing777 = [0, 1, 5, 15, 20, 25, 30];

    private static readonly Dictionary<string, int[]> ByType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["B736"] = Boeing737,
        ["B737"] = Boeing737,
        ["B738"] = Boeing737,
        ["B739"] = Boeing737,
        ["B38M"] = Boeing737,
        ["B77W"] = Boeing777,
        ["B77F"] = Boeing777
    };

    public static IReadOnlyList<int>? For(string? typeIcao) =>
        typeIcao is not null && ByType.TryGetValue(typeIcao.Trim(), out var table) ? table : null;

    public static int? ToDegrees(string? typeIcao, int handle, int simDetents = 0)
    {
        if (For(typeIcao) is not { } table) return null;
        if (simDetents > 0 && simDetents != table.Count - 1) return null;

        var clamped = Math.Clamp(handle, 0, FsuipcConvert.FullControl);
        var index = (int)Math.Round(clamped * (table.Count - 1) / (double)FsuipcConvert.FullControl);
        return table[index];
    }

    public static int ToHandle(string? typeIcao, int degrees)
    {
        if (For(typeIcao) is not { } table) throw new ArgumentException($"No flap table for {typeIcao}.", nameof(typeIcao));

        var index = table.ToList().IndexOf(degrees);
        if (index < 0) throw new ArgumentException($"{typeIcao} has no flaps {degrees}.", nameof(degrees));
        return (int)Math.Round(index * FsuipcConvert.FullControl / (double)(table.Count - 1));
    }

    public static int Detents(string? typeIcao) => For(typeIcao) is { } table ? table.Count - 1 : 0;
}