using System.Text.RegularExpressions;

namespace Litoral.Domain.Flights;

public static partial class FlightPlanRemarks
{
    public const string Operator = "LITORAL";
    public const string Telephony = "LITORAL";

    private static readonly string[] Owned = ["OPR", "PIC", "CS"];
    private static readonly string[] AfterOperator = ["ORGN", "PER", "ALTN", "RALT", "TALT", "RIF", "RMK"];

    [GeneratedRegex(@"(?<=^|\s)([A-Z]{2,4})/")]
    private static partial Regex IndicatorPattern();

    [GeneratedRegex(@"^[A-Z]{2,4}/")]
    private static partial Regex StartsWithIndicator();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    public static string Required(string pilotId) =>
        $"OPR/{Operator} PIC/{pilotId.Trim().ToUpperInvariant()} CS/{Telephony}";

    public static string Apply(string? otherInformation, string pilotId)
    {
        var segments = Parse(otherInformation);
        segments.RemoveAll(s => Owned.Contains(s.Indicator));

        var index = segments.FindIndex(s => AfterOperator.Contains(s.Indicator));
        segments.InsertRange(index < 0 ? segments.Count : index, Parse(Required(pilotId)));

        return string.Join(' ', segments.Select(s => s.Value.Length == 0 ? $"{s.Indicator}/" : $"{s.Indicator}/{s.Value}"));
    }

    public static bool IsCompliant(string? otherInformation, string pilotId)
    {
        var segments = Parse(otherInformation);
        string? Value(string indicator) => segments.LastOrDefault(s => s.Indicator == indicator).Value;

        return Value("OPR") == Operator
            && Value("PIC") == pilotId.Trim().ToUpperInvariant()
            && Value("CS") == Telephony;
    }

    public static string? ApplyToPrefileLink(string? url, string pilotId)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _)) return url;

        var mark = url.IndexOf('?');
        if (mark < 0) return url;

        var fragmentAt = url.IndexOf('#', mark);
        var query = fragmentAt < 0 ? url[(mark + 1)..] : url[(mark + 1)..fragmentAt];
        var fragment = fragmentAt < 0 ? "" : url[fragmentAt..];

        var changed = false;
        var pairs = query.Split('&').Select(pair =>
        {
            var eq = pair.IndexOf('=');
            if (eq < 0) return pair;

            var value = Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));
            var updated = value.TrimStart().StartsWith("(FPL", StringComparison.OrdinalIgnoreCase)
                ? ApplyToIcaoMessage(value, pilotId)
                : StartsWithIndicator().IsMatch(value.Trim().ToUpperInvariant()) ? Apply(value, pilotId) : null;

            if (updated is null || updated == value) return pair;
            changed = true;
            return $"{pair[..eq]}={Uri.EscapeDataString(updated)}";
        }).ToList();

        return changed ? $"{url[..mark]}?{string.Join('&', pairs)}{fragment}" : url;
    }

    public static string ApplyToIcaoMessage(string message, string pilotId)
    {
        var text = message.Trim();
        var closing = text.EndsWith(')') ? ")" : "";
        var body = closing.Length > 0 ? text[..^1] : text;

        var fields = Regex.Split(body, @"(?<=\s)-|\n-");
        for (var i = fields.Length - 1; i > 0; i--)
        {
            var field = fields[i].Trim();
            if (field == "0" || StartsWithIndicator().IsMatch(field.ToUpperInvariant()))
            {
                fields[i] = Apply(field == "0" ? "" : field, pilotId) + (i < fields.Length - 1 ? " " : "");
                return string.Join("-", fields).TrimEnd() + closing;
            }
        }

        return string.Join("-", fields).TrimEnd() + " -" + Apply("", pilotId) + closing;
    }

    private static List<(string Indicator, string Value)> Parse(string? otherInformation)
    {
        var text = Spaces().Replace((otherInformation ?? "").Trim().ToUpperInvariant(), " ");
        if (text is "" or "0") return [];

        var matches = IndicatorPattern().Matches(text);
        if (matches.Count == 0) return [("RMK", text)];

        var segments = new List<(string Indicator, string Value)>();
        var leading = text[..matches[0].Index].Trim();

        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index + matches[i].Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            segments.Add((matches[i].Groups[1].Value, text[start..end].Trim()));
        }

        if (leading.Length > 0)
        {
            var rmk = segments.FindIndex(s => s.Indicator == "RMK");
            if (rmk < 0) segments.Add(("RMK", leading));
            else segments[rmk] = ("RMK", $"{leading} {segments[rmk].Value}".Trim());
        }

        return segments;
    }
}