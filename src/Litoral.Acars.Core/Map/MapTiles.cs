namespace Litoral.Acars.Core.Map;

public static class MapTiles
{
    public const string Host = "localhost";
    public const string Attribution = "&copy; OpenStreetMap contributors &copy; CARTO";
    public const string OsmAttribution = "&copy; OpenStreetMap contributors";

    private const string OpenStreetMap = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

    public static bool HasKey(string? cartoKey) => !string.IsNullOrWhiteSpace(cartoKey);

    public static string Light(string? cartoKey) => Url("light_all", cartoKey);

    public static string Dark(string? cartoKey) => Url("dark_all", cartoKey);

    public static string AttributionFor(string? cartoKey) => HasKey(cartoKey) ? Attribution : OsmAttribution;

    private static string Url(string style, string? cartoKey) =>
        HasKey(cartoKey)
            ? $"https://basemaps.cartocdn.com/rastertiles/{style}/{{z}}/{{x}}/{{y}}.png?key={Uri.EscapeDataString(cartoKey!.Trim())}"
            : OpenStreetMap;
}