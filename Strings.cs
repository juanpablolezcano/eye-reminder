using System.Globalization;

namespace EyeReminder;

/// <summary>
/// One language's copy for the overlay. <see cref="Message"/> carries a {0} placeholder
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

internal static class Strings
{
    public const string Auto = "auto";

    /// <summary>
    /// Break lengths are clamped to 3 seconds and up, so every message can assume the
    /// plural form. Languages with harder plural rules (pl, ru) use an invariant
    /// abbreviation instead of trying to inflect.
    /// </summary>
    public static readonly LanguagePack[] All =
    {
        new("es-AR", "Español (AR)",
            "Descansá la vista",
            "Preparate para apartar la vista",
            "Mirá algo a 6 metros durante {0} segundos",
            "En segundo plano · primer descanso en {0} min"),

        new("es-419", "Español (LATAM)",
            "Descansa la vista",
            "Prepárate para mirar a lo lejos",
            "Mira algo a 6 metros durante {0} segundos",
            "En segundo plano · primer descanso en {0} min"),

        new("es-MX", "Español (MX)",
            "Descansa la vista",
            "Prepárate para ver a lo lejos",
            "Voltea a ver algo a 6 metros durante {0} segundos",
            "En segundo plano · primer descanso en {0} min"),

        new("es-ES", "Español (ES)",
            "Descansa la vista",
            "Prepárate para apartar la mirada",
            "Mira algo a 6 metros durante {0} segundos",
            "En segundo plano · primer descanso en {0} min"),

        new("en", "English",
            "Rest your eyes",
            "Get ready to look away",
            "Look at something 20 feet away for {0} seconds",
            "Running in the background · first break in {0} min"),

        new("pt-BR", "Português (BR)",
            "Descanse os olhos",
            "Prepare-se para desviar o olhar",
            "Olhe para algo a 6 metros por {0} segundos",
            "Em segundo plano · primeira pausa em {0} min"),

        new("pt-PT", "Português (PT)",
            "Descanse os olhos",
            "Prepare-se para desviar o olhar",
            "Olhe para algo a 6 metros durante {0} segundos",
            "Em segundo plano · primeira pausa em {0} min"),

        new("it", "Italiano",
            "Riposa gli occhi",
            "Preparati a distogliere lo sguardo",
            "Guarda qualcosa a 6 metri per {0} secondi",
            "In background · prima pausa tra {0} min"),

        new("fr", "Français",
            "Reposez vos yeux",
            "Préparez-vous à détourner le regard",
            "Regardez quelque chose à 6 mètres pendant {0} secondes",
            "En arrière-plan · première pause dans {0} min"),

        new("de", "Deutsch",
            "Augen ausruhen",
            "Gleich in die Ferne schauen",
            "Schau {0} Sekunden lang auf etwas in 6 Metern Entfernung",
            "Läuft im Hintergrund · erste Pause in {0} Min."),

        new("nl", "Nederlands",
            "Rust je ogen",
            "Maak je klaar om weg te kijken",
            "Kijk {0} seconden naar iets op 6 meter afstand",
            "Draait op de achtergrond · eerste pauze over {0} min"),

        new("pl", "Polski",
            "Odpocznij oczom",
            "Przygotuj się, by spojrzeć w dal",
            "Patrz na coś w odległości 6 metrów przez {0} sek.",
            "Działa w tle · pierwsza przerwa za {0} min"),

        new("ru", "Русский",
            "Дайте глазам отдохнуть",
            "Приготовьтесь посмотреть вдаль",
            "Смотрите вдаль, на 6 метров, {0} сек.",
            "Работает в фоне · первый перерыв через {0} мин"),

        new("tr", "Türkçe",
            "Gözlerini dinlendir",
            "Uzağa bakmaya hazırlan",
            "6 metre uzaktaki bir şeye {0} saniye bak",
            "Arka planda çalışıyor · ilk mola {0} dk sonra"),

        new("ja", "日本語",
            "目を休めましょう",
            "遠くを見る準備をしてください",
            "6メートル先を{0}秒間見てください",
            "バックグラウンドで実行中 · 最初の休憩は{0}分後"),

        new("ko", "한국어",
            "눈을 쉬게 하세요",
            "멀리 볼 준비를 하세요",
            "6미터 떨어진 곳을 {0}초 동안 보세요",
            "백그라운드에서 실행 중 · 첫 휴식까지 {0}분"),

        new("zh-Hans", "简体中文",
            "让眼睛休息一下",
            "准备望向远处",
            "看向 6 米外的物体 {0} 秒",
            "正在后台运行 · {0} 分钟后第一次休息"),

        new("zh-Hant", "繁體中文",
            "讓眼睛休息一下",
            "準備望向遠處",
            "看向 6 公尺外的物體 {0} 秒",
            "正在背景執行 · {0} 分鐘後第一次休息"),

        new("hi", "हिन्दी",
            "आंखों को आराम दें",
            "दूर देखने के लिए तैयार हो जाइए",
            "6 मीटर दूर किसी चीज़ को {0} सेकंड तक देखें",
            "पृष्ठभूमि में चल रहा है · पहला ब्रेक {0} मिनट में"),

        new("ar", "العربية",
            "أرِح عينيك",
            "استعد للنظر بعيدًا",
            "انظر إلى شيء على بعد 6 أمتار لمدة {0} ثانية",
            "يعمل في الخلفية · أول استراحة بعد {0} دقيقة",
            RightToLeft: true)
    };

    private static LanguagePack Fallback => All.First(p => p.Code == "en");

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

    private static LanguagePack Pack(string code) => All.First(p => p.Code == code);

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

        try
        {
            return string.Format(CultureInfo.CurrentCulture, Resolve(settings.Language).Background, minutes);
        }
        catch (FormatException)
        {
            return Resolve(settings.Language).Background;
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
