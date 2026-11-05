using System.Globalization;
using System.Security;

namespace SensorWidget.Core;

/// <summary>Appends failures to a text file. A failure to write is ignored so logging can never close the widget.</summary>
public sealed class ErrorLog
{
    private readonly string _path;

    public ErrorLog(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    /// <summary>%LocalAppData%\SensorWidget\errors.log</summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SensorWidget",
        "errors.log");

    public void Write(Exception failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        var entry = string.Create(CultureInfo.InvariantCulture, $"{DateTimeOffset.Now:O} {failure}{Environment.NewLine}");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.AppendAllText(_path, entry);
        }
        catch (Exception writeFailure) when (IsLogWriteFailure(writeFailure))
        {
            // Deliberately ignored: the widget has nowhere else to report this and must stay open (FR5.2).
        }
    }

    private static bool IsLogWriteFailure(Exception failure) =>
        failure is IOException or UnauthorizedAccessException or ArgumentException
            or NotSupportedException or SecurityException;
}
