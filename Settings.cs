using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace EyeReminder;

/// <summary>
/// User configuration, persisted to settings.json under %APPDATA%\EyeReminder.
/// Missing file or bad JSON falls back to defaults.
/// </summary>
public sealed class Settings
{
    public double IntervalMinutes { get; set; } = 20;
    public int BreakSeconds { get; set; } = 20;

    /// <summary>Heads-up count before the break starts. 0 skips straight to the break.</summary>
    public int CountdownSeconds { get; set; } = 3;

    /// <summary>Show a brief "running in the background" card at launch.</summary>
    public bool ShowStartupNotice { get; set; } = true;

    /// <summary>Hold the reminder while an app is full screen, a presentation is on, or
    /// notifications are silenced.</summary>
    public bool PauseWhenFullscreen { get; set; } = true;

    /// <summary>Ask the compositor to leave the card out of screen captures and shares.</summary>
    public bool HideFromScreenShare { get; set; } = true;

    /// <summary>Fade, Slide, Scale or None.</summary>
    public string Animation { get; set; } = "Fade";

    /// <summary>Pause the schedule while there is no keyboard or mouse input.</summary>
    public bool PauseWhenIdle { get; set; } = true;

    public double IdleMinutes { get; set; } = 5;

    /// <summary>Dark, Light, Warm or Minimal.</summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>Hex accent override such as "#7FD4C4". Empty uses the theme default.</summary>
    public string AccentColor { get; set; } = "";

    /// <summary>TopCenter, TopLeft, TopRight, BottomLeft, BottomRight or Center.</summary>
    public string Position { get; set; } = "TopCenter";

    public double Scale { get; set; } = 1.0;
    public bool AllScreens { get; set; }
    public double Opacity { get; set; } = 0.95;

    public bool Sound { get; set; } = true;

    /// <summary>
    /// Sound file to play. Relative names resolve next to the executable. Empty, or a
    /// path that does not exist, falls back to the synthesised chime.
    /// </summary>
    public string SoundFile { get; set; } = AppInfo.DefaultSoundFile;

    public double SoundVolume { get; set; } = 0.45;

    /// <summary>Play the same sound again when the break is over.</summary>
    public bool SoundOnFinish { get; set; } = true;

    /// <summary>Language code from <see cref="OverlayText.All"/>, or "auto" to follow Windows.</summary>
    public string Language { get; set; } = OverlayText.Auto;

    // Empty means "use the language default". {0} is replaced with BreakSeconds in both cases.
    public string TitleOverride { get; set; } = "";
    public string CountdownOverride { get; set; } = "";
    public string MessageOverride { get; set; } = "";

    /// <summary>
    /// Preferences live in the user profile, not next to the executable: that survives
    /// rebuilds, reinstalls and moving the app into a read-only folder.
    /// </summary>
    public static string FilePath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppInfo.Name,
        AppInfo.SettingsFileName);

    /// <summary>Defaults shipped alongside the .exe, used once to seed the user profile.</summary>
    private static string SeedPath => System.IO.Path.Combine(AppContext.BaseDirectory, AppInfo.SettingsFileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        // Keeps accented text readable in the file instead of \u00E1 escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static Settings Load()
    {
        // The user profile wins. On a first run it does not exist yet, so fall back to the
        // file next to the .exe and copy it across, carrying old settings over on upgrade.
        var loaded = ReadFrom(FilePath);

        if (loaded is null)
        {
            loaded = ReadFrom(SeedPath);
            loaded?.Save();
        }

        return loaded ?? new Settings();
    }

    private static Settings? ReadFrom(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;

            var loaded = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), JsonOptions);
            return loaded?.Sanitized();
        }
        catch
        {
            // A broken settings file should never stop the reminder from running.
            return null;
        }
    }

    /// <summary>Writes the file. Returns false instead of throwing when the folder is read-only.</summary>
    public bool Save()
    {
        try
        {
            Sanitized();

            var folder = System.IO.Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Working copy for the settings dialog, so Cancel leaves the live values untouched.</summary>
    public Settings Clone() => (Settings)MemberwiseClone();

    /// <summary>Overwrites every field in place, used by "restore defaults".</summary>
    public void CopyFrom(Settings other)
    {
        foreach (var property in typeof(Settings).GetProperties())
        {
            if (property.CanRead && property.CanWrite) property.SetValue(this, property.GetValue(other));
        }
    }

    public Settings Sanitized()
    {
        IntervalMinutes = Math.Clamp(IntervalMinutes, 0.25, 8 * 60);
        BreakSeconds = Math.Clamp(BreakSeconds, 3, 600);
        CountdownSeconds = Math.Clamp(CountdownSeconds, 0, 10);
        IdleMinutes = Math.Clamp(IdleMinutes, 1, 60);
        Opacity = Math.Clamp(Opacity, 0.2, 1.0);
        Scale = Math.Clamp(Scale, 0.7, 2.0);
        SoundVolume = Math.Clamp(SoundVolume, 0.0, 1.0);
        return this;
    }
}
