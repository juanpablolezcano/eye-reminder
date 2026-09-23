using System.Globalization;
using System.IO;
using System.Text.Json;

namespace EyeReminder;

/// <summary>
/// Chrome text: tray menu and settings dialog. Separate from <see cref="OverlayText"/>,
/// which holds the reminder copy the user can override.
///
/// Tables come from Languages/ui.json, keyed by the same codes as the overlay language
/// list. An entry may be a full table, a string naming another language to reuse verbatim,
/// or a table with "$extends" naming a base to inherit from and override. Anything missing
/// falls back to English, then to the key itself, so a gap shows up as a visible key rather
/// than an empty label.
/// </summary>
internal static class Ui
{
    private const string ExtendsKey = "$extends";
    private const string FallbackCode = "en";

    private static readonly Dictionary<string, Dictionary<string, string>> Packs = Load();

    private static string _code = FallbackCode;

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

        if (Packs.TryGetValue(FallbackCode, out var english) && english.TryGetValue(key, out var fallback))
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

    // ---- loading ----------------------------------------------------------

    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        var packs = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var stream = DataFiles.Open("Languages", "ui.json");
            if (stream is null) return packs;

            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("packs", out var root)) return packs;

            var raw = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in root.EnumerateObject()) raw[entry.Name] = entry.Value;

            foreach (var code in raw.Keys)
            {
                Resolve(code, raw, packs, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }
        }
        catch (JsonException error)
        {
            // Leave the chrome untranslated rather than stopping the reminder from running.
            Log.Write("ui.json could not be parsed, the interface will show raw keys", error);
        }
        catch (IOException error)
        {
            Log.Write("ui.json could not be read, the interface will show raw keys", error);
        }

        return packs;
    }

    /// <summary>
    /// Builds one table, following aliases and "$extends" chains. The visiting set makes a
    /// malformed file with a cycle in it stop rather than recurse forever.
    /// </summary>
    private static Dictionary<string, string>? Resolve(
        string code,
        Dictionary<string, JsonElement> raw,
        Dictionary<string, Dictionary<string, string>> packs,
        HashSet<string> visiting)
    {
        if (packs.TryGetValue(code, out var already)) return already;
        if (!raw.TryGetValue(code, out var element)) return null;
        if (!visiting.Add(code)) return null;

        Dictionary<string, string>? table;

        if (element.ValueKind == JsonValueKind.String)
        {
            // Alias: share the base table, since nothing mutates it after loading.
            table = Resolve(element.GetString() ?? string.Empty, raw, packs, visiting);
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (element.TryGetProperty(ExtendsKey, out var baseName) && baseName.ValueKind == JsonValueKind.String)
            {
                var inherited = Resolve(baseName.GetString() ?? string.Empty, raw, packs, visiting);
                if (inherited is not null)
                {
                    foreach (var pair in inherited) table[pair.Key] = pair.Value;
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(ExtendsKey)) continue;
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    table[property.Name] = property.Value.GetString()!;
                }
            }
        }
        else
        {
            table = null;
        }

        visiting.Remove(code);

        if (table is not null) packs[code] = table;
        return table;
    }
}
