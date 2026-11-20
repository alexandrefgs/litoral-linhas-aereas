using System.IO;
using Litoral.Acars.Core.Flight;

namespace Litoral.Acars.Ui;

public sealed class FlightLogFile(string callsign, DateTime startedUtc)
{
    public static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Litoral ACARS", "Logs");

    public string FilePath { get; } = Path.Combine(Folder, $"{Safe(callsign)}_{startedUtc:yyyyMMdd-HHmm}Z.log");

    public void Append(FlightLogEntry entry)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.AppendAllText(FilePath, entry + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string Safe(string name) =>
        string.Concat(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
}