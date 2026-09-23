using System.Globalization;
using System.IO;
using System.Text.Json;

namespace EyeReminder;

/// <summary>
/// One language's copy for the reminder card. <see cref="Message"/> carries a {0} placeholder
/// that is filled with the configured break length, and <see cref="Background"/> one for
/// the interval in minutes, so the text always matches the settings.
/// </summary>
internal sealed record LanguagePack(
    string Code,
    string Label,
    string Title,
    string Countdown,
    string Message,
    string Background,
    bool RightToLeft = false);

/// <summary>
/// The reminder card's copy, loaded from Languages/overlay.json.
///
/// Break lengths are clamped to 3 seconds and up, so every message can assume the plural
/// form. Languages with harder plural rules (pl, ru) use an invariant abbreviation in the
/// data instead of trying to inflect.
/// </summary>
internal static class OverlayText
{
    public const string Auto = "auto";

    /// <summary>
    /// Last resort if the embedded data cannot be read. The reminder is the point of the
    /// app, so it still has something to say rather than refusing to start.
    ///
    /// Declared before <see cref="All"/> on purpose: static initialisers run in declaration
    /// order, so the other way round the fallback would still be null when Load needs it.
    /// </summary>
    private static readonly LanguagePack[] Builtin =
    {
        new("en", "English",
            "Rest your eyes",
            "Get ready to look away",
            "Look at something 20 feet away for {0} seconds",
            "Running in the background · first break in {0} min")
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static readonly LanguagePack[] All = Load();

    private sealed record LanguageFile(LanguagePack[]? Languages);

    private static LanguagePack[] Load()
    {
        try
        {
            using var stream = LanguageResources.Open("overlay.json");
            if (stream is null) return Builtin;

            var file = JsonSerializer.Deserialize<LanguageFile>(stream, JsonOptions);

            return file?.Languages is { Length: > 0 } packs ? packs : Builtin;
        }
        catch (JsonException)
        {
            return Builtin;
        }
        catch (IOException)
        {
            return Builtin;
        }
    }

    private static LanguagePack Fallback =>
        All.FirstOrDefault(p => p.Code == "en") ?? All[0];

    /// <summary>Resolves the configured code, following the Windows display language for "auto".</summary>
    public static LanguagePack Resolve(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Equals(Auto, StringComparison.OrdinalIgnoreCase))
        {
            return FromCulture(CultureInfo.CurrentUICulture);
        }

        return All.FirstOrDefault(p => p.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? Fallback;
    }

    private static LanguagePack FromCulture(CultureInfo culture)
    {
        var name = culture.Name;                 // e.g. "es-AR", "pt-BR", "zh-Hans-CN"
        var language = culture.TwoLetterISOLanguageName;

        // Exact regional match first.
        var exact = All.FirstOrDefault(p => p.Code.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        // Then the regional groups where the wording actually differs.
        switch (language)
        {
            case "es":
                if (name.EndsWith("-ES", StringComparison.OrdinalIgnoreCase)) return Pack("es-ES");
                if (name.EndsWith("-MX", StringComparison.OrdinalIgnoreCase)) return Pack("es-MX");
                if (name.EndsWith("-AR", StringComparison.OrdinalIgnoreCase)) return Pack("es-AR");
                return Pack("es-419");

            case "pt":
                return name.EndsWith("-PT", StringComparison.OrdinalIgnoreCase) ? Pack("pt-PT") : Pack("pt-BR");

            case "zh":
                var traditional = name.Contains("Hant", StringComparison.OrdinalIgnoreCase)
                                  || name.EndsWith("-TW", StringComparison.OrdinalIgnoreCase)
                                  || name.EndsWith("-HK", StringComparison.OrdinalIgnoreCase)
                                  || name.EndsWith("-MO", StringComparison.OrdinalIgnoreCase);
                return traditional ? Pack("zh-Hant") : Pack("zh-Hans");
        }

        return All.FirstOrDefault(p => p.Code.Equals(language, StringComparison.OrdinalIgnoreCase))
               ?? Fallback;
    }

    private static LanguagePack Pack(string code) =>
        All.FirstOrDefault(p => p.Code == code) ?? Fallback;

    // ---- resolved copy ----------------------------------------------------

    // Raw templates, with {0} still in place. The settings dialog shows these so that
    // custom text starts from a placeholder and keeps following the configured length.
    public static string TitleTemplate(Settings settings) =>
        Or(settings.TitleOverride, Resolve(settings.Language).Title);

    public static string CountdownTemplate(Settings settings) =>
        Or(settings.CountdownOverride, Resolve(settings.Language).Countdown);

    public static string MessageTemplate(Settings settings) =>
        Or(settings.MessageOverride, Resolve(settings.Language).Message);

    private static string Or(string? custom, string fallback) =>
        string.IsNullOrWhiteSpace(custom) ? fallback : custom;

    public static string Title(Settings settings) =>
        Pick(settings.TitleOverride, Resolve(settings.Language).Title, settings.BreakSeconds);

    public static string Countdown(Settings settings) =>
        Pick(settings.CountdownOverride, Resolve(settings.Language).Countdown, settings.BreakSeconds);

    public static string Message(Settings settings) =>
        Pick(settings.MessageOverride, Resolve(settings.Language).Message, settings.BreakSeconds);

    /// <summary>"Running in the background, first break in N minutes."</summary>
    public static string Background(Settings settings)
    {
        var minutes = settings.IntervalMinutes.ToString("0.#", CultureInfo.CurrentCulture);
        var template = Resolve(settings.Language).Background;

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, minutes);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    /// <summary>
    /// Uses the override when the user set one, otherwise the language default. Either way
    /// {0} becomes the configured break length, so custom text keeps the live number too.
    /// </summary>
    private static string Pick(string? custom, string fallback, int seconds)
    {
        var template = string.IsNullOrWhiteSpace(custom) ? fallback : custom;

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, seconds);
        }
        catch (FormatException)
        {
            // A stray brace in custom text must not blank the overlay.
            return template;
        }
    }
}
