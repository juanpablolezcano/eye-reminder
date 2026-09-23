namespace EyeReminder;

/// <summary>
/// Every name the app writes into the filesystem, the registry or the UI, in one place.
/// Nothing else should spell these out.
/// </summary>
internal static class AppInfo
{
    public const string Name = "EyeReminder";

    /// <summary>File name of both the shipped defaults and the user's saved preferences.</summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>
    /// Default reminder sound, looked up next to the executable. When it is absent the
    /// synthesised chime in <see cref="Chime"/> takes over, so shipping it is optional.
    /// </summary>
    public const string DefaultSoundFile = "notification.wav";

    /// <summary>Name under HKCU\...\Run, and the marker looked for when reading it back.</summary>
    public const string RunKeyValueName = Name;

    public const string SingleInstanceMutex = @"Local\" + Name + ".SingleInstance";
}
