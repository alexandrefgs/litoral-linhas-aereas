namespace Litoral.Acars.Core.Api;

public static class ServerAddress
{
    public static bool TryParse(string? value, out Uri address, out string error)
    {
        address = null!;
        error = "";

        var text = (value ?? "").Trim();
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            error = "Enter the full server address, e.g. https://litoral.example.com.";
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttp && !IsLocal(uri))
        {
            error = "Use https. The ACARS key is only sent over plain http to this computer.";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            error = "The server address cannot have a query or a fragment.";
            return false;
        }

        address = new Uri(uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/");
        return true;
    }

    public static bool IsLocal(Uri uri) => uri.IsLoopback;
}