using System.IO;
using System.Reflection;

namespace EyeReminder;

/// <summary>
/// Opens the JSON data files (languages, themes).
///
/// A file dropped next to settings.json in the user profile wins over the copy embedded in
/// the assembly, so a translation can be fixed or a theme added without rebuilding. The
/// embedded copy is what a single-file build ships with, so nothing has to travel loose.
/// </summary>
internal static class DataFiles
{
    /// <summary>Folder the user can drop overrides into, alongside their settings.</summary>
    public static string OverrideFolder =>
        Path.GetDirectoryName(Settings.FilePath) ?? AppContext.BaseDirectory;

    /// <summary>The copy compiled into the assembly, ignoring any user override.</summary>
    public static Stream? OpenShipped(string folder, string fileName) =>
        Assembly.GetExecutingAssembly().GetManifestResourceStream($"{AppInfo.Name}.{folder}.{fileName}");

    public static Stream? Open(string folder, string fileName)
    {
        var overridePath = Path.Combine(OverrideFolder, fileName);

        try
        {
            if (File.Exists(overridePath))
            {
                return File.OpenRead(overridePath);
            }
        }
        catch (IOException error)
        {
            Log.Write($"Could not read the override {overridePath}", error);
        }
        catch (UnauthorizedAccessException error)
        {
            Log.Write($"Could not read the override {overridePath}", error);
        }

        return OpenShipped(folder, fileName);
    }
}
