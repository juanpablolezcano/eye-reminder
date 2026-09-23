using Microsoft.Win32;

namespace EyeReminder;

/// <summary>
/// "Start with Windows" through the per-user Run key. Preferred over a Startup-folder
/// shortcut because it needs no COM interop and reads back cleanly for the checkbox.
/// </summary>
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = AppInfo.RunKeyValueName;

    private static string? ExePath => Environment.ProcessPath;

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string value
                   && value.Contains(AppInfo.Name, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Returns false when the registry write was refused.</summary>
    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (key is null) return false;

            if (enabled)
            {
                if (string.IsNullOrEmpty(ExePath)) return false;
                key.SetValue(ValueName, "\"" + ExePath + "\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
