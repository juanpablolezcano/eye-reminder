using System.IO;
using System.Text.Json;
using System.Windows.Media;

// WinForms implicit usings pull in System.Drawing, whose Color collides with the WPF one.
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace EyeReminder;

internal sealed record Palette(
    Color Background,
    Color Border,
    Color Title,
    Color Message,
    Color Accent,
    Color Track);

/// <summary>
/// One theme as it appears in the settings dialog. The caption is resolved on every read,
/// not cached, so switching the interface language relabels the shipped themes.
/// </summary>
internal sealed record ThemeOption(string Id, string? Label, string? LabelKey, Palette Palette)
{
    public string Caption => Captions.Resolve(Label, LabelKey, Id);
}

/// <summary>One preset accent. An empty hex means "leave the theme's own accent alone".</summary>
internal sealed record AccentOption(string? Label, string? LabelKey, string Hex)
{
    public string Caption => Captions.Resolve(Label, LabelKey, Hex);
}

/// <summary>A literal label wins; otherwise the key is translated.</summary>
internal static class Captions
{
    public static string Resolve(string? label, string? labelKey, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(label)) return label;
        if (!string.IsNullOrWhiteSpace(labelKey)) return Ui.T(labelKey);
        return fallback;
    }
}

/// <summary>
/// Card palettes, loaded from Themes/themes.json. Dropping that file into the settings
/// folder replaces the built-in set, which is how custom themes work.
/// </summary>
internal static class Themes
{
    public static readonly (string Id, string UiKey)[] Positions =
    {
        ("TopLeft",     "pos.topLeft"),
        ("TopCenter",   "pos.topCenter"),
        ("TopRight",    "pos.topRight"),
        ("BottomLeft",  "pos.bottomLeft"),
        ("Center",      "pos.center"),
        ("BottomRight", "pos.bottomRight")
    };

    // Everything the loader touches has to be declared above it: static initialisers run in
    // declaration order, and a field further down is still null when the loader reads it.
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly ThemeOption BuiltinTheme = new(
        "Dark",
        null,
        "theme.dark",
        new Palette(
            Background: Hex("#F0121418"),
            Border:     Hex("#2EFFFFFF"),
            Title:      Hex("#FFFFFFFF"),
            Message:    Hex("#B4FFFFFF"),
            Accent:     Hex("#FF7FD4C4"),
            Track:      Hex("#26FFFFFF")));

    private static readonly ThemeFile File = LoadFile();

    public static IReadOnlyList<ThemeOption> All { get; } = BuildThemes();

    public static IReadOnlyList<AccentOption> Accents { get; } = BuildAccents();

    public static Palette For(Settings settings)
    {
        var theme = All.FirstOrDefault(t => t.Id.Equals(settings.Theme?.Trim() ?? "", StringComparison.OrdinalIgnoreCase))
                    ?? All[0];

        var palette = theme.Palette;
        var accent = settings.AccentColor?.Trim();

        if (!string.IsNullOrEmpty(accent) && TryHex(accent, out var custom))
        {
            palette = palette with { Accent = custom };
        }

        return palette;
    }

    public static SolidColorBrush Brush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    // ---- loading ----------------------------------------------------------

    private sealed record ThemeEntry(
        string? Id, string? Label, string? LabelKey,
        string? Background, string? Border, string? Title, string? Message, string? Accent, string? Track);

    private sealed record AccentEntry(string? Label, string? LabelKey, string? Hex);

    private sealed record ThemeFile(ThemeEntry[]? Themes, AccentEntry[]? Accents);

    private static ThemeFile LoadFile()
    {
        try
        {
            using var stream = DataFiles.Open("Themes", "themes.json");
            if (stream is null) return new ThemeFile(null, null);

            return JsonSerializer.Deserialize<ThemeFile>(stream, JsonOptions) ?? new ThemeFile(null, null);
        }
        catch (JsonException error)
        {
            Log.Write("themes.json could not be parsed, falling back to the built-in theme", error);
            return new ThemeFile(null, null);
        }
        catch (IOException error)
        {
            Log.Write("themes.json could not be read, falling back to the built-in theme", error);
            return new ThemeFile(null, null);
        }
    }

    private static ThemeOption[] BuildThemes()
    {
        var themes = File.Themes?
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Id))
            .Select(entry => new ThemeOption(
                entry.Id!.Trim(),
                entry.Label,
                entry.LabelKey,
                new Palette(
                    Background: Hex(entry.Background, BuiltinTheme.Palette.Background),
                    Border:     Hex(entry.Border,     BuiltinTheme.Palette.Border),
                    Title:      Hex(entry.Title,      BuiltinTheme.Palette.Title),
                    Message:    Hex(entry.Message,    BuiltinTheme.Palette.Message),
                    Accent:     Hex(entry.Accent,     BuiltinTheme.Palette.Accent),
                    Track:      Hex(entry.Track,      BuiltinTheme.Palette.Track))))
            .ToArray();

        return themes is { Length: > 0 } ? themes : new[] { BuiltinTheme };
    }

    private static AccentOption[] BuildAccents()
    {
        var accents = File.Accents?
            .Select(entry => new AccentOption(entry.Label, entry.LabelKey, entry.Hex ?? ""))
            .ToArray();

        return accents is { Length: > 0 } ? accents : new[] { new AccentOption(null, "accent.theme", "") };
    }

    private static Color Hex(string value) => (Color)ColorConverter.ConvertFromString(value)!;

    private static Color Hex(string? value, Color fallback) =>
        !string.IsNullOrWhiteSpace(value) && TryHex(value, out var parsed) ? parsed : fallback;

    private static bool TryHex(string value, out Color color)
    {
        try
        {
            color = Hex(value.StartsWith('#') ? value : "#" + value);
            return true;
        }
        catch (FormatException)
        {
            color = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            color = default;
            return false;
        }
    }
}
