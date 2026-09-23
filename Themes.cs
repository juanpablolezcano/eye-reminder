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

internal static class Themes
{
    public static readonly (string Id, string UiKey)[] All =
    {
        ("Dark",    "theme.dark"),
        ("Light",   "theme.light"),
        ("Warm",    "theme.warm"),
        ("Minimal", "theme.minimal")
    };

    /// <summary>Preset accents for the settings dialog. Empty hex means "use the theme's own".</summary>
    public static readonly (string UiKey, string Hex)[] Accents =
    {
        ("accent.theme",  ""),
        ("accent.teal",   "#7FD4C4"),
        ("accent.blue",   "#7FB2F0"),
        ("accent.amber",  "#E8B473"),
        ("accent.rose",   "#E894B4"),
        ("accent.green",  "#9BD47F"),
        ("accent.violet", "#B79BE8")
    };

    public static readonly (string Id, string UiKey)[] Positions =
    {
        ("TopLeft",     "pos.topLeft"),
        ("TopCenter",   "pos.topCenter"),
        ("TopRight",    "pos.topRight"),
        ("BottomLeft",  "pos.bottomLeft"),
        ("Center",      "pos.center"),
        ("BottomRight", "pos.bottomRight")
    };

    public static Palette For(Settings settings)
    {
        var palette = settings.Theme.Trim().ToLowerInvariant() switch
        {
            "light" => new Palette(
                Background: Hex("#FAF9FAFC"),
                Border:     Hex("#1A000000"),
                Title:      Hex("#FF16181D"),
                Message:    Hex("#A616181D"),
                Accent:     Hex("#FF10998A"),
                Track:      Hex("#1A000000")),

            "warm" => new Palette(
                Background: Hex("#F21B1410"),
                Border:     Hex("#2EFFD9A0"),
                Title:      Hex("#FFFDF4E6"),
                Message:    Hex("#B0FDF4E6"),
                Accent:     Hex("#FFE0A458"),
                Track:      Hex("#26FFD9A0")),

            // Minimal keeps a dark scrim so text stays readable over any content,
            // but drops the border and softens the fill.
            "minimal" => new Palette(
                Background: Hex("#A6000000"),
                Border:     Hex("#00FFFFFF"),
                Title:      Hex("#FFFFFFFF"),
                Message:    Hex("#A0FFFFFF"),
                Accent:     Hex("#FFFFFFFF"),
                Track:      Hex("#26FFFFFF")),

            _ => new Palette(
                Background: Hex("#F0121418"),
                Border:     Hex("#2EFFFFFF"),
                Title:      Hex("#FFFFFFFF"),
                Message:    Hex("#B4FFFFFF"),
                Accent:     Hex("#FF7FD4C4"),
                Track:      Hex("#26FFFFFF"))
        };

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

    private static Color Hex(string value) => (Color)ColorConverter.ConvertFromString(value)!;

    private static bool TryHex(string value, out Color color)
    {
        try
        {
            color = Hex(value.StartsWith("#") ? value : "#" + value);
            return true;
        }
        catch
        {
            color = default;
            return false;
        }
    }
}
