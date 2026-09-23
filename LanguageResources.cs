using System.IO;
using System.Reflection;

namespace EyeReminder;

/// <summary>
/// Opens the JSON language files under Languages/. They are embedded in the assembly, so a
/// single-file build still carries every translation with no loose files to ship.
/// </summary>
internal static class LanguageResources
{
    public static Stream? Open(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream($"{AppInfo.Name}.Languages.{fileName}");
    }
}
