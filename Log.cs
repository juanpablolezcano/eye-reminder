using System.Globalization;
using System.IO;

namespace EyeReminder;

/// <summary>
/// A single rolling text file next to the settings, written only when something was
/// swallowed. Without it a malformed themes.json or an unreadable sound just produces
/// silent fallbacks with no way to find out why.
/// </summary>
internal static class Log
{
    private const long MaxBytes = 64 * 1024;

    private static readonly Lock Gate = new();

    private static string FilePath =>
        Path.Combine(Path.GetDirectoryName(Settings.FilePath) ?? AppContext.BaseDirectory, "log.txt");

    public static void Write(string message, Exception? error = null)
    {
        var line = error is null
            ? $"{DateTime.Now.ToString("s", CultureInfo.InvariantCulture)}  {message}"
            : $"{DateTime.Now.ToString("s", CultureInfo.InvariantCulture)}  {message}: {error.GetType().Name}: {error.Message}";

        try
        {
            lock (Gate)
            {
                var path = FilePath;
                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

                // Start over rather than grow without bound; this is a breadcrumb, not an audit trail.
                if (File.Exists(path) && new FileInfo(path).Length > MaxBytes) File.Delete(path);

                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch (IOException)
        {
            // Logging must never be the reason the app misbehaves.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
