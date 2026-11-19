using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Litoral.Acars.Settings;

public sealed record AcarsSettings(string ServerUrl, string? Key);

public static class SettingsStore
{
    public const string DefaultServerUrl = "https://localhost:7112";

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Litoral ACARS key");
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Litoral ACARS");
    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    private sealed record StoredSettings(string? ServerUrl, string? ProtectedKey);

    public static AcarsSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AcarsSettings(DefaultServerUrl, null);

            var stored = JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(FilePath));
            var key = stored?.ProtectedKey is { Length: > 0 } protectedKey
                ? Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protectedKey), Entropy, DataProtectionScope.CurrentUser))
                : null;
            return new AcarsSettings(stored?.ServerUrl ?? DefaultServerUrl, key);
        }
        catch (Exception ex) when (ex is IOException or JsonException or CryptographicException or FormatException)
        {
            return new AcarsSettings(DefaultServerUrl, null);
        }
    }

    public static void Save(AcarsSettings settings)
    {
        Directory.CreateDirectory(Folder);

        var protectedKey = string.IsNullOrWhiteSpace(settings.Key)
            ? null
            : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(settings.Key.Trim()), Entropy, DataProtectionScope.CurrentUser));

        File.WriteAllText(FilePath, JsonSerializer.Serialize(new StoredSettings(settings.ServerUrl, protectedKey)));
    }
}