namespace EyeReminder;

/// <summary>
/// How the card enters and leaves. Slide picks its direction from where the card sits, so
/// a card anchored at the bottom rises into view instead of dropping in from above.
/// </summary>
internal static class OverlayAnimation
{
    public const string Fade = "Fade";
    public const string Slide = "Slide";
    public const string Scale = "Scale";
    public const string None = "None";

    public static readonly (string Id, string UiKey)[] All =
    {
        (Fade,  "anim.fade"),
        (Slide, "anim.slide"),
        (Scale, "anim.scale"),
        (None,  "anim.none")
    };

    public static string Normalise(string? value)
    {
        var name = value?.Trim() ?? string.Empty;

        return All.FirstOrDefault(a => a.Id.Equals(name, StringComparison.OrdinalIgnoreCase)).Id ?? Fade;
    }

    /// <summary>Distance the card travels for <see cref="Slide"/>, in device-independent pixels.</summary>
    public const double SlideDistance = 22;

    /// <summary>How far down <see cref="Scale"/> starts before growing to full size.</summary>
    public const double ScaleFrom = 0.92;

    /// <summary>True when the card should slide up into place rather than down.</summary>
    public static bool RisesFromBelow(string position) =>
        position.Trim().StartsWith("Bottom", StringComparison.OrdinalIgnoreCase);
}
