using System.Globalization;

namespace EyeReminder;

/// <summary>
/// Chrome text: tray menu and settings dialog. Separate from <see cref="OverlayText"/>,
/// which holds the reminder copy the user can override.
///
/// Packs are keyed by the same codes as the overlay language list. Regional variants that
/// share their chrome wording (the four Spanish ones, the two Portuguese ones) reuse one
/// table; anything missing falls back to English, then to the key itself, so a gap shows up
/// as a visible key rather than an empty label.
/// </summary>
internal static partial class Ui
{
    private static readonly Dictionary<string, Dictionary<string, string>> Packs = new(StringComparer.OrdinalIgnoreCase);

    private static string _code = "en";

    static Ui()
    {
        RegisterPacks();
    }

    /// <summary>Points the chrome at a language. Call before building any menu or window.</summary>
    public static void Use(string? languageCode)
    {
        _code = OverlayText.Resolve(languageCode).Code;
    }

    public static string T(string key)
    {
        if (Packs.TryGetValue(_code, out var pack) && pack.TryGetValue(key, out var value))
        {
            return value;
        }

        if (Packs.TryGetValue("en", out var english) && english.TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    public static string T(string key, params object?[] args)
    {
        try
        {
            return string.Format(CultureInfo.CurrentCulture, T(key), args);
        }
        catch (FormatException)
        {
            return T(key);
        }
    }

    /// <summary>Copies a table, replacing the given keys. Used for regional tweaks.</summary>
    private static Dictionary<string, string> With(
        Dictionary<string, string> source, params (string Key, string Value)[] changes)
    {
        var copy = new Dictionary<string, string>(source, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in changes) copy[key] = value;
        return copy;
    }

}
