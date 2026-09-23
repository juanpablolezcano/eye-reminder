using System.Globalization;

namespace EyeReminder;

/// <summary>
/// Chrome text: tray menu and settings dialog. Separate from <see cref="Strings"/>, which
/// holds the overlay copy the user can override.
///
/// Packs are keyed by the same codes as <see cref="Strings.All"/>. Regional variants that
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
        RegisterEuropean();
        RegisterAsian();
    }

    /// <summary>Points the chrome at a language. Call before building any menu or window.</summary>
    public static void Use(string? languageCode)
    {
        _code = Strings.Resolve(languageCode).Code;
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

    private static void RegisterEuropean()
    {
        var es = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tray.tooltip"] = "EyeReminder — regla 20/20/20",
            ["tray.next"] = "Próximo descanso en {0}",
            ["tray.pausedUntil"] = "En pausa hasta las {0}",
            ["tray.idle"] = "En pausa por inactividad",
            ["tray.test"] = "Probar ahora",
            ["tray.reset"] = "Reiniciar contador",
            ["tray.pause"] = "Pausar 1 hora",
            ["tray.resume"] = "Reanudar",
            ["tray.settings"] = "Configuración...",
            ["tray.exit"] = "Salir",

            ["win.title"] = "EyeReminder — Configuración",
            ["sec.reminder"] = "RECORDATORIO",
            ["sec.idle"] = "INACTIVIDAD",
            ["sec.language"] = "IDIOMA",
            ["sec.style"] = "ESTILO",
            ["sec.position"] = "POSICIÓN EN PANTALLA",
            ["sec.texts"] = "TEXTOS",
            ["sec.sound"] = "SONIDO",
            ["sec.system"] = "SISTEMA",

            ["lbl.frequency"] = "Frecuencia",
            ["lbl.break"] = "Duración del descanso",
            ["lbl.countdown"] = "Cuenta regresiva",
            ["lbl.pauseAfter"] = "Pausar tras",
            ["lbl.accent"] = "Color de acento",
            ["lbl.size"] = "Tamaño",
            ["lbl.opacity"] = "Opacidad",
            ["lbl.volume"] = "Volumen",
            ["lbl.textTitle"] = "Título",
            ["lbl.textCountdown"] = "Durante la cuenta regresiva",
            ["lbl.textBreak"] = "Durante el descanso",

            ["chk.idle"] = "Pausar si no uso la PC",
            ["chk.startupNotice"] = "Avisar al arrancar que está en segundo plano",
            ["chk.allScreens"] = "Mostrar en todos los monitores",
            ["chk.soundStart"] = "Reproducir sonido al empezar",
            ["chk.soundEnd"] = "Reproducirlo también al terminar",
            ["chk.customText"] = "Escribir mis propios textos",
            ["chk.startup"] = "Arrancar con Windows",

            ["btn.browse"] = "Examinar",
            ["btn.test"] = "Probar",
            ["btn.preview"] = "Vista previa",
            ["btn.cancel"] = "Cancelar",
            ["btn.save"] = "Guardar",

            ["hint.countdown"] = "3, 2, 1 antes del descanso. 0 = sin aviso.",
            ["hint.idle"] = "Al volver, el intervalo empieza de cero.",
            ["hint.customText"] = "Sin tildar, usa los textos del idioma. {0} = cantidad de segundos.",
            ["hint.sound"] = "Vacío = sonido integrado. Acepta cualquier .wav.",
            ["hint.startup"] = "Se registrará: {0}",

            ["unit.min"] = "min",
            ["unit.sec"] = "s",
            ["opt.noCountdown"] = "sin aviso",

            ["theme.dark"] = "Oscuro",
            ["theme.light"] = "Claro",
            ["theme.warm"] = "Cálido",
            ["theme.minimal"] = "Mínimo",

            ["pos.topLeft"] = "Arriba izq.",
            ["pos.topCenter"] = "Arriba centro",
            ["pos.topRight"] = "Arriba der.",
            ["pos.bottomLeft"] = "Abajo izq.",
            ["pos.center"] = "Centro",
            ["pos.bottomRight"] = "Abajo der.",

            ["accent.theme"] = "Del tema",
            ["accent.teal"] = "Turquesa",
            ["accent.blue"] = "Azul",
            ["accent.amber"] = "Ámbar",
            ["accent.rose"] = "Rosa",
            ["accent.green"] = "Verde",
            ["accent.violet"] = "Violeta",

            ["lang.auto"] = "Automático",
            ["dlg.saveFailed"] = "No se pudo escribir {0}.\nLos cambios se aplican ahora pero se perderán al reiniciar.",
            ["dlg.registryFailed"] = "No se pudo escribir la clave de inicio. El resto se guardó igual.",
            ["file.pickSound"] = "Elegir sonido",
            ["file.wavFilter"] = "Audio WAV (*.wav)|*.wav"
        };

        Packs["es-419"] = es;
        Packs["es-MX"] = es;
        Packs["es-ES"] = es;
        Packs["es-AR"] = With(es,
            ("chk.customText", "Escribir mis propios textos"),
            ("hint.customText", "Sin tildar, usá los textos del idioma. {0} = cantidad de segundos."));

        Packs["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tray.tooltip"] = "EyeReminder — 20/20/20 rule",
            ["tray.next"] = "Next break in {0}",
            ["tray.pausedUntil"] = "Paused until {0}",
            ["tray.idle"] = "Paused while idle",
            ["tray.test"] = "Test now",
            ["tray.reset"] = "Restart timer",
            ["tray.pause"] = "Pause for 1 hour",
            ["tray.resume"] = "Resume",
            ["tray.settings"] = "Settings...",
            ["tray.exit"] = "Exit",

            ["win.title"] = "EyeReminder — Settings",
            ["sec.reminder"] = "REMINDER",
            ["sec.idle"] = "IDLE",
            ["sec.language"] = "LANGUAGE",
            ["sec.style"] = "STYLE",
            ["sec.position"] = "SCREEN POSITION",
            ["sec.texts"] = "TEXT",
            ["sec.sound"] = "SOUND",
            ["sec.system"] = "SYSTEM",

            ["lbl.frequency"] = "Frequency",
            ["lbl.break"] = "Break length",
            ["lbl.countdown"] = "Countdown",
            ["lbl.pauseAfter"] = "Pause after",
            ["lbl.accent"] = "Accent colour",
            ["lbl.size"] = "Size",
            ["lbl.opacity"] = "Opacity",
            ["lbl.volume"] = "Volume",
            ["lbl.textTitle"] = "Title",
            ["lbl.textCountdown"] = "During the countdown",
            ["lbl.textBreak"] = "During the break",

            ["chk.idle"] = "Pause when I am not using the PC",
            ["chk.startupNotice"] = "Show a notice at startup",
            ["chk.allScreens"] = "Show on every monitor",
            ["chk.soundStart"] = "Play a sound at the start",
            ["chk.soundEnd"] = "Play it again at the end",
            ["chk.customText"] = "Write my own text",
            ["chk.startup"] = "Start with Windows",

            ["btn.browse"] = "Browse",
            ["btn.test"] = "Test",
            ["btn.preview"] = "Preview",
            ["btn.cancel"] = "Cancel",
            ["btn.save"] = "Save",

            ["hint.countdown"] = "3, 2, 1 before the break. 0 = no countdown.",
            ["hint.idle"] = "When you come back, the interval starts over.",
            ["hint.customText"] = "Unticked, the language text is used. {0} = break length.",
            ["hint.sound"] = "Empty = built-in sound. Any .wav works.",
            ["hint.startup"] = "Will register: {0}",

            ["unit.min"] = "min",
            ["unit.sec"] = "s",
            ["opt.noCountdown"] = "none",

            ["theme.dark"] = "Dark",
            ["theme.light"] = "Light",
            ["theme.warm"] = "Warm",
            ["theme.minimal"] = "Minimal",

            ["pos.topLeft"] = "Top left",
            ["pos.topCenter"] = "Top centre",
            ["pos.topRight"] = "Top right",
            ["pos.bottomLeft"] = "Bottom left",
            ["pos.center"] = "Centre",
            ["pos.bottomRight"] = "Bottom right",

            ["accent.theme"] = "Theme default",
            ["accent.teal"] = "Teal",
            ["accent.blue"] = "Blue",
            ["accent.amber"] = "Amber",
            ["accent.rose"] = "Rose",
            ["accent.green"] = "Green",
            ["accent.violet"] = "Violet",

            ["lang.auto"] = "Automatic",
            ["dlg.saveFailed"] = "Could not write {0}.\nChanges apply now but will be lost on restart.",
            ["dlg.registryFailed"] = "Could not write the startup key. Everything else was saved.",
            ["file.pickSound"] = "Choose a sound",
            ["file.wavFilter"] = "WAV audio (*.wav)|*.wav"
        };

        var pt = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tray.tooltip"] = "EyeReminder — regra 20/20/20",
            ["tray.next"] = "Próxima pausa em {0}",
            ["tray.pausedUntil"] = "Pausado até {0}",
            ["tray.idle"] = "Pausado por inatividade",
            ["tray.test"] = "Testar agora",
            ["tray.reset"] = "Reiniciar contagem",
            ["tray.pause"] = "Pausar por 1 hora",
            ["tray.resume"] = "Retomar",
            ["tray.settings"] = "Configurações...",
            ["tray.exit"] = "Sair",

            ["win.title"] = "EyeReminder — Configurações",
            ["sec.reminder"] = "LEMBRETE",
            ["sec.idle"] = "INATIVIDADE",
            ["sec.language"] = "IDIOMA",
            ["sec.style"] = "ESTILO",
            ["sec.position"] = "POSIÇÃO NA TELA",
            ["sec.texts"] = "TEXTOS",
            ["sec.sound"] = "SOM",
            ["sec.system"] = "SISTEMA",

            ["lbl.frequency"] = "Frequência",
            ["lbl.break"] = "Duração da pausa",
            ["lbl.countdown"] = "Contagem regressiva",
            ["lbl.pauseAfter"] = "Pausar após",
            ["lbl.accent"] = "Cor de destaque",
            ["lbl.size"] = "Tamanho",
            ["lbl.opacity"] = "Opacidade",
            ["lbl.volume"] = "Volume",
            ["lbl.textTitle"] = "Título",
            ["lbl.textCountdown"] = "Durante a contagem",
            ["lbl.textBreak"] = "Durante a pausa",

            ["chk.idle"] = "Pausar quando eu não usar o PC",
            ["chk.startupNotice"] = "Avisar ao iniciar que está em segundo plano",
            ["chk.allScreens"] = "Mostrar em todos os monitores",
            ["chk.soundStart"] = "Tocar som no início",
            ["chk.soundEnd"] = "Tocar também no fim",
            ["chk.customText"] = "Escrever meus próprios textos",
            ["chk.startup"] = "Iniciar com o Windows",

            ["btn.browse"] = "Procurar",
            ["btn.test"] = "Testar",
            ["btn.preview"] = "Pré-visualizar",
            ["btn.cancel"] = "Cancelar",
            ["btn.save"] = "Salvar",

            ["hint.countdown"] = "3, 2, 1 antes da pausa. 0 = sem aviso.",
            ["hint.idle"] = "Ao voltar, o intervalo recomeça do zero.",
            ["hint.customText"] = "Sem marcar, usa os textos do idioma. {0} = segundos.",
            ["hint.sound"] = "Vazio = som integrado. Aceita qualquer .wav.",
            ["hint.startup"] = "Será registrado: {0}",

            ["unit.min"] = "min",
            ["unit.sec"] = "s",
            ["opt.noCountdown"] = "sem aviso",

            ["theme.dark"] = "Escuro",
            ["theme.light"] = "Claro",
            ["theme.warm"] = "Quente",
            ["theme.minimal"] = "Mínimo",

            ["pos.topLeft"] = "Sup. esq.",
            ["pos.topCenter"] = "Sup. centro",
            ["pos.topRight"] = "Sup. dir.",
            ["pos.bottomLeft"] = "Inf. esq.",
            ["pos.center"] = "Centro",
            ["pos.bottomRight"] = "Inf. dir.",

            ["accent.theme"] = "Do tema",
            ["accent.teal"] = "Turquesa",
            ["accent.blue"] = "Azul",
            ["accent.amber"] = "Âmbar",
            ["accent.rose"] = "Rosa",
            ["accent.green"] = "Verde",
            ["accent.violet"] = "Violeta",

            ["lang.auto"] = "Automático",
            ["dlg.saveFailed"] = "Não foi possível escrever {0}.\nAs mudanças valem agora mas se perdem ao reiniciar.",
            ["dlg.registryFailed"] = "Não foi possível escrever a chave de inicialização. O resto foi salvo.",
            ["file.pickSound"] = "Escolher som",
            ["file.wavFilter"] = "Áudio WAV (*.wav)|*.wav"
        };

        Packs["pt-BR"] = pt;
        Packs["pt-PT"] = With(pt, ("btn.save", "Guardar"), ("tray.settings", "Definições..."), ("win.title", "EyeReminder — Definições"));

        Packs["it"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tray.tooltip"] = "EyeReminder — regola 20/20/20",
            ["tray.next"] = "Prossima pausa tra {0}",
            ["tray.pausedUntil"] = "In pausa fino alle {0}",
            ["tray.idle"] = "In pausa per inattività",
            ["tray.test"] = "Prova ora",
            ["tray.reset"] = "Riavvia il timer",
            ["tray.pause"] = "Sospendi per 1 ora",
            ["tray.resume"] = "Riprendi",
            ["tray.settings"] = "Impostazioni...",
            ["tray.exit"] = "Esci",

            ["win.title"] = "EyeReminder — Impostazioni",
            ["sec.reminder"] = "PROMEMORIA",
            ["sec.idle"] = "INATTIVITÀ",
            ["sec.language"] = "LINGUA",
            ["sec.style"] = "STILE",
            ["sec.position"] = "POSIZIONE SULLO SCHERMO",
            ["sec.texts"] = "TESTI",
            ["sec.sound"] = "SUONO",
            ["sec.system"] = "SISTEMA",

            ["lbl.frequency"] = "Frequenza",
            ["lbl.break"] = "Durata della pausa",
            ["lbl.countdown"] = "Conto alla rovescia",
            ["lbl.pauseAfter"] = "Sospendi dopo",
            ["lbl.accent"] = "Colore accento",
            ["lbl.size"] = "Dimensione",
            ["lbl.opacity"] = "Opacità",
            ["lbl.volume"] = "Volume",
            ["lbl.textTitle"] = "Titolo",
            ["lbl.textCountdown"] = "Durante il conto alla rovescia",
            ["lbl.textBreak"] = "Durante la pausa",

            ["chk.idle"] = "Sospendi quando non uso il PC",
            ["chk.startupNotice"] = "Avvisa all'avvio che è in background",
            ["chk.allScreens"] = "Mostra su tutti gli schermi",
            ["chk.soundStart"] = "Riproduci un suono all'inizio",
            ["chk.soundEnd"] = "Riproducilo anche alla fine",
            ["chk.customText"] = "Scrivere testi personalizzati",
            ["chk.startup"] = "Avvia con Windows",

            ["btn.browse"] = "Sfoglia",
            ["btn.test"] = "Prova",
            ["btn.preview"] = "Anteprima",
            ["btn.cancel"] = "Annulla",
            ["btn.save"] = "Salva",

            ["hint.countdown"] = "3, 2, 1 prima della pausa. 0 = nessun avviso.",
            ["hint.idle"] = "Al ritorno, l'intervallo riparte da zero.",
            ["hint.customText"] = "Senza spunta usa i testi della lingua. {0} = secondi.",
            ["hint.sound"] = "Vuoto = suono integrato. Accetta qualsiasi .wav.",
            ["hint.startup"] = "Verrà registrato: {0}",

            ["unit.min"] = "min",
            ["unit.sec"] = "s",
            ["opt.noCountdown"] = "nessuno",

            ["theme.dark"] = "Scuro",
            ["theme.light"] = "Chiaro",
            ["theme.warm"] = "Caldo",
            ["theme.minimal"] = "Minimo",

            ["pos.topLeft"] = "In alto a sx",
            ["pos.topCenter"] = "In alto al centro",
            ["pos.topRight"] = "In alto a dx",
            ["pos.bottomLeft"] = "In basso a sx",
            ["pos.center"] = "Centro",
            ["pos.bottomRight"] = "In basso a dx",

            ["accent.theme"] = "Del tema",
            ["accent.teal"] = "Turchese",
            ["accent.blue"] = "Blu",
            ["accent.amber"] = "Ambra",
            ["accent.rose"] = "Rosa",
            ["accent.green"] = "Verde",
            ["accent.violet"] = "Viola",

            ["lang.auto"] = "Automatico",
            ["dlg.saveFailed"] = "Impossibile scrivere {0}.\nLe modifiche valgono ora ma andranno perse al riavvio.",
            ["dlg.registryFailed"] = "Impossibile scrivere la chiave di avvio. Il resto è stato salvato.",
            ["file.pickSound"] = "Scegli un suono",
            ["file.wavFilter"] = "Audio WAV (*.wav)|*.wav"
        };

        Packs["fr"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tray.tooltip"] = "EyeReminder — règle 20/20/20",
            ["tray.next"] = "Prochaine pause dans {0}",
            ["tray.pausedUntil"] = "En pause jusqu'à {0}",
            ["tray.idle"] = "En pause (inactivité)",
            ["tray.test"] = "Tester maintenant",
            ["tray.reset"] = "Redémarrer le minuteur",
            ["tray.pause"] = "Suspendre 1 heure",
            ["tray.resume"] = "Reprendre",
            ["tray.settings"] = "Paramètres...",
            ["tray.exit"] = "Quitter",

            ["win.title"] = "EyeReminder — Paramètres",
            ["sec.reminder"] = "RAPPEL",
            ["sec.idle"] = "INACTIVITÉ",
            ["sec.language"] = "LANGUE",
            ["sec.style"] = "STYLE",
            ["sec.position"] = "POSITION À L'ÉCRAN",
            ["sec.texts"] = "TEXTES",
            ["sec.sound"] = "SON",
            ["sec.system"] = "SYSTÈME",

            ["lbl.frequency"] = "Fréquence",
            ["lbl.break"] = "Durée de la pause",
            ["lbl.countdown"] = "Compte à rebours",
            ["lbl.pauseAfter"] = "Suspendre après",
            ["lbl.accent"] = "Couleur d'accent",
            ["lbl.size"] = "Taille",
            ["lbl.opacity"] = "Opacité",
            ["lbl.volume"] = "Volume",
            ["lbl.textTitle"] = "Titre",
            ["lbl.textCountdown"] = "Pendant le compte à rebours",
            ["lbl.textBreak"] = "Pendant la pause",

            ["chk.idle"] = "Suspendre si je n'utilise pas le PC",
            ["chk.startupNotice"] = "Prévenir au démarrage qu'il tourne en arrière-plan",
            ["chk.allScreens"] = "Afficher sur tous les écrans",
            ["chk.soundStart"] = "Jouer un son au début",
            ["chk.soundEnd"] = "Le rejouer à la fin",
            ["chk.customText"] = "Écrire mes propres textes",
            ["chk.startup"] = "Démarrer avec Windows",

            ["btn.browse"] = "Parcourir",
            ["btn.test"] = "Tester",
            ["btn.preview"] = "Aperçu",
            ["btn.cancel"] = "Annuler",
            ["btn.save"] = "Enregistrer",

            ["hint.countdown"] = "3, 2, 1 avant la pause. 0 = sans avertissement.",
            ["hint.idle"] = "À votre retour, l'intervalle repart de zéro.",
            ["hint.customText"] = "Décoché, les textes de la langue sont utilisés. {0} = secondes.",
            ["hint.sound"] = "Vide = son intégré. Accepte n'importe quel .wav.",
            ["hint.startup"] = "Sera enregistré : {0}",

            ["unit.min"] = "min",
            ["unit.sec"] = "s",
            ["opt.noCountdown"] = "aucun",

            ["theme.dark"] = "Sombre",
            ["theme.light"] = "Clair",
            ["theme.warm"] = "Chaud",
            ["theme.minimal"] = "Minimal",

            ["pos.topLeft"] = "Haut gauche",
            ["pos.topCenter"] = "Haut centre",
            ["pos.topRight"] = "Haut droite",
            ["pos.bottomLeft"] = "Bas gauche",
            ["pos.center"] = "Centre",
            ["pos.bottomRight"] = "Bas droite",

            ["accent.theme"] = "Du thème",
            ["accent.teal"] = "Turquoise",
            ["accent.blue"] = "Bleu",
            ["accent.amber"] = "Ambre",
            ["accent.rose"] = "Rose",
            ["accent.green"] = "Vert",
            ["accent.violet"] = "Violet",

            ["lang.auto"] = "Automatique",
            ["dlg.saveFailed"] = "Impossible d'écrire {0}.\nLes changements s'appliquent mais seront perdus au redémarrage.",
            ["dlg.registryFailed"] = "Impossible d'écrire la clé de démarrage. Le reste a été enregistré.",
            ["file.pickSound"] = "Choisir un son",
            ["file.wavFilter"] = "Audio WAV (*.wav)|*.wav"
        };

        Packs["de"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tray.tooltip"] = "EyeReminder — 20/20/20-Regel",
            ["tray.next"] = "Nächste Pause in {0}",
            ["tray.pausedUntil"] = "Pausiert bis {0}",
            ["tray.idle"] = "Pausiert wegen Inaktivität",
            ["tray.test"] = "Jetzt testen",
            ["tray.reset"] = "Timer neu starten",
            ["tray.pause"] = "1 Stunde pausieren",
            ["tray.resume"] = "Fortsetzen",
            ["tray.settings"] = "Einstellungen...",
            ["tray.exit"] = "Beenden",

            ["win.title"] = "EyeReminder — Einstellungen",
            ["sec.reminder"] = "ERINNERUNG",
            ["sec.idle"] = "INAKTIVITÄT",
            ["sec.language"] = "SPRACHE",
            ["sec.style"] = "STIL",
            ["sec.position"] = "POSITION AM BILDSCHIRM",
            ["sec.texts"] = "TEXTE",
            ["sec.sound"] = "TON",
            ["sec.system"] = "SYSTEM",

            ["lbl.frequency"] = "Häufigkeit",
            ["lbl.break"] = "Dauer der Pause",
            ["lbl.countdown"] = "Countdown",
            ["lbl.pauseAfter"] = "Pausieren nach",
            ["lbl.accent"] = "Akzentfarbe",
            ["lbl.size"] = "Größe",
            ["lbl.opacity"] = "Deckkraft",
            ["lbl.volume"] = "Lautstärke",
            ["lbl.textTitle"] = "Titel",
            ["lbl.textCountdown"] = "Während des Countdowns",
            ["lbl.textBreak"] = "Während der Pause",

            ["chk.idle"] = "Pausieren, wenn ich den PC nicht benutze",
            ["chk.startupNotice"] = "Beim Start melden, dass es im Hintergrund läuft",
            ["chk.allScreens"] = "Auf allen Bildschirmen anzeigen",
            ["chk.soundStart"] = "Ton am Anfang abspielen",
            ["chk.soundEnd"] = "Auch am Ende abspielen",
            ["chk.customText"] = "Eigene Texte schreiben",
            ["chk.startup"] = "Mit Windows starten",

            ["btn.browse"] = "Durchsuchen",
            ["btn.test"] = "Testen",
            ["btn.preview"] = "Vorschau",
            ["btn.cancel"] = "Abbrechen",
            ["btn.save"] = "Speichern",

            ["hint.countdown"] = "3, 2, 1 vor der Pause. 0 = ohne Vorwarnung.",
            ["hint.idle"] = "Bei der Rückkehr beginnt das Intervall von vorn.",
            ["hint.customText"] = "Ohne Haken gilt der Text der Sprache. {0} = Sekunden.",
            ["hint.sound"] = "Leer = eingebauter Ton. Jede .wav funktioniert.",
            ["hint.startup"] = "Wird eingetragen: {0}",

            ["unit.min"] = "Min.",
            ["unit.sec"] = "s",
            ["opt.noCountdown"] = "keiner",

            ["theme.dark"] = "Dunkel",
            ["theme.light"] = "Hell",
            ["theme.warm"] = "Warm",
            ["theme.minimal"] = "Minimal",

            ["pos.topLeft"] = "Oben links",
            ["pos.topCenter"] = "Oben mittig",
            ["pos.topRight"] = "Oben rechts",
            ["pos.bottomLeft"] = "Unten links",
            ["pos.center"] = "Mitte",
            ["pos.bottomRight"] = "Unten rechts",

            ["accent.theme"] = "Vom Thema",
            ["accent.teal"] = "Türkis",
            ["accent.blue"] = "Blau",
            ["accent.amber"] = "Bernstein",
            ["accent.rose"] = "Rosé",
            ["accent.green"] = "Grün",
            ["accent.violet"] = "Violett",

            ["lang.auto"] = "Automatisch",
            ["dlg.saveFailed"] = "{0} konnte nicht geschrieben werden.\nDie Änderungen gelten jetzt, gehen aber beim Neustart verloren.",
            ["dlg.registryFailed"] = "Der Autostart-Eintrag konnte nicht geschrieben werden. Alles andere wurde gespeichert.",
            ["file.pickSound"] = "Ton auswählen",
            ["file.wavFilter"] = "WAV-Audio (*.wav)|*.wav"
        };

        Packs["nl"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tray.tooltip"] = "EyeReminder — 20/20/20-regel",
            ["tray.next"] = "Volgende pauze over {0}",
            ["tray.pausedUntil"] = "Gepauzeerd tot {0}",
            ["tray.idle"] = "Gepauzeerd wegens inactiviteit",
            ["tray.test"] = "Nu testen",
            ["tray.reset"] = "Timer opnieuw starten",
            ["tray.pause"] = "1 uur pauzeren",
            ["tray.resume"] = "Hervatten",
            ["tray.settings"] = "Instellingen...",
            ["tray.exit"] = "Afsluiten",

            ["win.title"] = "EyeReminder — Instellingen",
            ["sec.reminder"] = "HERINNERING",
            ["sec.idle"] = "INACTIVITEIT",
            ["sec.language"] = "TAAL",
            ["sec.style"] = "STIJL",
            ["sec.position"] = "POSITIE OP HET SCHERM",
            ["sec.texts"] = "TEKSTEN",
            ["sec.sound"] = "GELUID",
            ["sec.system"] = "SYSTEEM",

            ["lbl.frequency"] = "Frequentie",
            ["lbl.break"] = "Duur van de pauze",
            ["lbl.countdown"] = "Aftellen",
            ["lbl.pauseAfter"] = "Pauzeren na",
            ["lbl.accent"] = "Accentkleur",
            ["lbl.size"] = "Grootte",
            ["lbl.opacity"] = "Dekking",
            ["lbl.volume"] = "Volume",
            ["lbl.textTitle"] = "Titel",
            ["lbl.textCountdown"] = "Tijdens het aftellen",
            ["lbl.textBreak"] = "Tijdens de pauze",

            ["chk.idle"] = "Pauzeren als ik de pc niet gebruik",
            ["chk.startupNotice"] = "Bij het starten melden dat het op de achtergrond draait",
            ["chk.allScreens"] = "Op alle schermen tonen",
            ["chk.soundStart"] = "Geluid afspelen bij de start",
            ["chk.soundEnd"] = "Ook aan het eind afspelen",
            ["chk.customText"] = "Eigen teksten schrijven",
            ["chk.startup"] = "Starten met Windows",

            ["btn.browse"] = "Bladeren",
            ["btn.test"] = "Testen",
            ["btn.preview"] = "Voorbeeld",
            ["btn.cancel"] = "Annuleren",
            ["btn.save"] = "Opslaan",

            ["hint.countdown"] = "3, 2, 1 voor de pauze. 0 = geen waarschuwing.",
            ["hint.idle"] = "Bij terugkomst begint het interval opnieuw.",
            ["hint.customText"] = "Uitgevinkt gebruikt de taalteksten. {0} = seconden.",
            ["hint.sound"] = "Leeg = ingebouwd geluid. Elke .wav werkt.",
            ["hint.startup"] = "Wordt geregistreerd: {0}",

            ["unit.min"] = "min",
            ["unit.sec"] = "s",
            ["opt.noCountdown"] = "geen",

            ["theme.dark"] = "Donker",
            ["theme.light"] = "Licht",
            ["theme.warm"] = "Warm",
            ["theme.minimal"] = "Minimaal",

            ["pos.topLeft"] = "Linksboven",
            ["pos.topCenter"] = "Midden boven",
            ["pos.topRight"] = "Rechtsboven",
            ["pos.bottomLeft"] = "Linksonder",
            ["pos.center"] = "Midden",
            ["pos.bottomRight"] = "Rechtsonder",

            ["accent.theme"] = "Van het thema",
            ["accent.teal"] = "Turquoise",
            ["accent.blue"] = "Blauw",
            ["accent.amber"] = "Amber",
            ["accent.rose"] = "Roze",
            ["accent.green"] = "Groen",
            ["accent.violet"] = "Violet",

            ["lang.auto"] = "Automatisch",
            ["dlg.saveFailed"] = "Kon {0} niet schrijven.\nWijzigingen gelden nu maar gaan verloren bij herstart.",
            ["dlg.registryFailed"] = "Kon de opstartsleutel niet schrijven. De rest is opgeslagen.",
            ["file.pickSound"] = "Geluid kiezen",
            ["file.wavFilter"] = "WAV-audio (*.wav)|*.wav"
        };

        Packs["pl"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tray.tooltip"] = "EyeReminder — zasada 20/20/20",
            ["tray.next"] = "Następna przerwa za {0}",
            ["tray.pausedUntil"] = "Wstrzymane do {0}",
            ["tray.idle"] = "Wstrzymane z powodu bezczynności",
            ["tray.test"] = "Przetestuj teraz",
            ["tray.reset"] = "Zrestartuj licznik",
            ["tray.pause"] = "Wstrzymaj na 1 godzinę",
            ["tray.resume"] = "Wznów",
            ["tray.settings"] = "Ustawienia...",
            ["tray.exit"] = "Zakończ",

            ["win.title"] = "EyeReminder — Ustawienia",
            ["sec.reminder"] = "PRZYPOMNIENIE",
            ["sec.idle"] = "BEZCZYNNOŚĆ",
            ["sec.language"] = "JĘZYK",
            ["sec.style"] = "STYL",
            ["sec.position"] = "POZYCJA NA EKRANIE",
            ["sec.texts"] = "TEKSTY",
            ["sec.sound"] = "DŹWIĘK",
            ["sec.system"] = "SYSTEM",

            ["lbl.frequency"] = "Częstotliwość",
            ["lbl.break"] = "Długość przerwy",
            ["lbl.countdown"] = "Odliczanie",
            ["lbl.pauseAfter"] = "Wstrzymaj po",
            ["lbl.accent"] = "Kolor akcentu",
            ["lbl.size"] = "Rozmiar",
            ["lbl.opacity"] = "Krycie",
            ["lbl.volume"] = "Głośność",
            ["lbl.textTitle"] = "Tytuł",
            ["lbl.textCountdown"] = "Podczas odliczania",
            ["lbl.textBreak"] = "Podczas przerwy",

            ["chk.idle"] = "Wstrzymaj, gdy nie używam komputera",
            ["chk.startupNotice"] = "Przy starcie informuj, że działa w tle",
            ["chk.allScreens"] = "Pokaż na wszystkich monitorach",
            ["chk.soundStart"] = "Odtwórz dźwięk na początku",
            ["chk.soundEnd"] = "Odtwórz też na końcu",
            ["chk.customText"] = "Własne teksty",
            ["chk.startup"] = "Uruchamiaj z systemem Windows",

            ["btn.browse"] = "Przeglądaj",
            ["btn.test"] = "Testuj",
            ["btn.preview"] = "Podgląd",
            ["btn.cancel"] = "Anuluj",
            ["btn.save"] = "Zapisz",

            ["hint.countdown"] = "3, 2, 1 przed przerwą. 0 = bez ostrzeżenia.",
            ["hint.idle"] = "Po powrocie odstęp liczy się od nowa.",
            ["hint.customText"] = "Bez zaznaczenia używa tekstów języka. {0} = sekundy.",
            ["hint.sound"] = "Puste = dźwięk wbudowany. Działa każdy .wav.",
            ["hint.startup"] = "Zostanie zapisane: {0}",

            ["unit.min"] = "min",
            ["unit.sec"] = "s",
            ["opt.noCountdown"] = "brak",

            ["theme.dark"] = "Ciemny",
            ["theme.light"] = "Jasny",
            ["theme.warm"] = "Ciepły",
            ["theme.minimal"] = "Minimalny",

            ["pos.topLeft"] = "Lewy górny",
            ["pos.topCenter"] = "Górny środek",
            ["pos.topRight"] = "Prawy górny",
            ["pos.bottomLeft"] = "Lewy dolny",
            ["pos.center"] = "Środek",
            ["pos.bottomRight"] = "Prawy dolny",

            ["accent.theme"] = "Z motywu",
            ["accent.teal"] = "Turkusowy",
            ["accent.blue"] = "Niebieski",
            ["accent.amber"] = "Bursztynowy",
            ["accent.rose"] = "Różowy",
            ["accent.green"] = "Zielony",
            ["accent.violet"] = "Fioletowy",

            ["lang.auto"] = "Automatycznie",
            ["dlg.saveFailed"] = "Nie udało się zapisać {0}.\nZmiany działają teraz, ale znikną po restarcie.",
            ["dlg.registryFailed"] = "Nie udało się zapisać klucza autostartu. Reszta została zapisana.",
            ["file.pickSound"] = "Wybierz dźwięk",
            ["file.wavFilter"] = "Audio WAV (*.wav)|*.wav"
        };

        Packs["ru"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tray.tooltip"] = "EyeReminder — правило 20/20/20",
            ["tray.next"] = "Следующий перерыв через {0}",
            ["tray.pausedUntil"] = "Пауза до {0}",
            ["tray.idle"] = "Пауза из-за бездействия",
            ["tray.test"] = "Проверить сейчас",
            ["tray.reset"] = "Сбросить таймер",
            ["tray.pause"] = "Пауза на 1 час",
            ["tray.resume"] = "Продолжить",
            ["tray.settings"] = "Настройки...",
            ["tray.exit"] = "Выход",

            ["win.title"] = "EyeReminder — Настройки",
            ["sec.reminder"] = "НАПОМИНАНИЕ",
            ["sec.idle"] = "БЕЗДЕЙСТВИЕ",
            ["sec.language"] = "ЯЗЫК",
            ["sec.style"] = "СТИЛЬ",
            ["sec.position"] = "ПОЛОЖЕНИЕ НА ЭКРАНЕ",
            ["sec.texts"] = "ТЕКСТЫ",
            ["sec.sound"] = "ЗВУК",
            ["sec.system"] = "СИСТЕМА",

            ["lbl.frequency"] = "Частота",
            ["lbl.break"] = "Длительность перерыва",
            ["lbl.countdown"] = "Обратный отсчёт",
            ["lbl.pauseAfter"] = "Пауза после",
            ["lbl.accent"] = "Цвет акцента",
            ["lbl.size"] = "Размер",
            ["lbl.opacity"] = "Непрозрачность",
            ["lbl.volume"] = "Громкость",
            ["lbl.textTitle"] = "Заголовок",
            ["lbl.textCountdown"] = "Во время отсчёта",
            ["lbl.textBreak"] = "Во время перерыва",

            ["chk.idle"] = "Ставить на паузу, когда я не за компьютером",
            ["chk.startupNotice"] = "Сообщать при запуске, что работает в фоне",
            ["chk.allScreens"] = "Показывать на всех мониторах",
            ["chk.soundStart"] = "Звук в начале",
            ["chk.soundEnd"] = "Звук также в конце",
            ["chk.customText"] = "Свои тексты",
            ["chk.startup"] = "Запускать вместе с Windows",

            ["btn.browse"] = "Обзор",
            ["btn.test"] = "Проверить",
            ["btn.preview"] = "Предпросмотр",
            ["btn.cancel"] = "Отмена",
            ["btn.save"] = "Сохранить",

            ["hint.countdown"] = "3, 2, 1 перед перерывом. 0 = без отсчёта.",
            ["hint.idle"] = "После возвращения интервал начинается заново.",
            ["hint.customText"] = "Без галочки берутся тексты языка. {0} = секунды.",
            ["hint.sound"] = "Пусто = встроенный звук. Подходит любой .wav.",
            ["hint.startup"] = "Будет записано: {0}",

            ["unit.min"] = "мин",
            ["unit.sec"] = "с",
            ["opt.noCountdown"] = "нет",

            ["theme.dark"] = "Тёмная",
            ["theme.light"] = "Светлая",
            ["theme.warm"] = "Тёплая",
            ["theme.minimal"] = "Минимальная",

            ["pos.topLeft"] = "Сверху слева",
            ["pos.topCenter"] = "Сверху по центру",
            ["pos.topRight"] = "Сверху справа",
            ["pos.bottomLeft"] = "Снизу слева",
            ["pos.center"] = "По центру",
            ["pos.bottomRight"] = "Снизу справа",

            ["accent.theme"] = "Из темы",
            ["accent.teal"] = "Бирюзовый",
            ["accent.blue"] = "Синий",
            ["accent.amber"] = "Янтарный",
            ["accent.rose"] = "Розовый",
            ["accent.green"] = "Зелёный",
            ["accent.violet"] = "Фиолетовый",

            ["lang.auto"] = "Автоматически",
            ["dlg.saveFailed"] = "Не удалось записать {0}.\nИзменения действуют сейчас, но пропадут после перезапуска.",
            ["dlg.registryFailed"] = "Не удалось записать ключ автозапуска. Остальное сохранено.",
            ["file.pickSound"] = "Выбрать звук",
            ["file.wavFilter"] = "Аудио WAV (*.wav)|*.wav"
        };

        Packs["tr"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tray.tooltip"] = "EyeReminder — 20/20/20 kuralı",
            ["tray.next"] = "Sonraki mola {0} sonra",
            ["tray.pausedUntil"] = "{0} saatine kadar duraklatıldı",
            ["tray.idle"] = "Hareketsizlik nedeniyle duraklatıldı",
            ["tray.test"] = "Şimdi dene",
            ["tray.reset"] = "Sayacı sıfırla",
            ["tray.pause"] = "1 saat duraklat",
            ["tray.resume"] = "Devam et",
            ["tray.settings"] = "Ayarlar...",
            ["tray.exit"] = "Çıkış",

            ["win.title"] = "EyeReminder — Ayarlar",
            ["sec.reminder"] = "HATIRLATICI",
            ["sec.idle"] = "HAREKETSİZLİK",
            ["sec.language"] = "DİL",
            ["sec.style"] = "STİL",
            ["sec.position"] = "EKRANDAKİ KONUM",
            ["sec.texts"] = "METİNLER",
            ["sec.sound"] = "SES",
            ["sec.system"] = "SİSTEM",

            ["lbl.frequency"] = "Sıklık",
            ["lbl.break"] = "Mola süresi",
            ["lbl.countdown"] = "Geri sayım",
            ["lbl.pauseAfter"] = "Şundan sonra duraklat",
            ["lbl.accent"] = "Vurgu rengi",
            ["lbl.size"] = "Boyut",
            ["lbl.opacity"] = "Saydamlık",
            ["lbl.volume"] = "Ses düzeyi",
            ["lbl.textTitle"] = "Başlık",
            ["lbl.textCountdown"] = "Geri sayım sırasında",
            ["lbl.textBreak"] = "Mola sırasında",

            ["chk.idle"] = "Bilgisayarı kullanmadığımda duraklat",
            ["chk.startupNotice"] = "Açılışta arka planda çalıştığını bildir",
            ["chk.allScreens"] = "Tüm ekranlarda göster",
            ["chk.soundStart"] = "Başlangıçta ses çal",
            ["chk.soundEnd"] = "Bitişte de çal",
            ["chk.customText"] = "Kendi metinlerimi yazayım",
            ["chk.startup"] = "Windows ile başlat",

            ["btn.browse"] = "Gözat",
            ["btn.test"] = "Dene",
            ["btn.preview"] = "Önizleme",
            ["btn.cancel"] = "İptal",
            ["btn.save"] = "Kaydet",

            ["hint.countdown"] = "Moladan önce 3, 2, 1. 0 = uyarı yok.",
            ["hint.idle"] = "Geri döndüğünüzde aralık sıfırdan başlar.",
            ["hint.customText"] = "İşaretsizken dilin metinleri kullanılır. {0} = saniye.",
            ["hint.sound"] = "Boş = yerleşik ses. Her .wav çalışır.",
            ["hint.startup"] = "Kaydedilecek: {0}",

            ["unit.min"] = "dk",
            ["unit.sec"] = "sn",
            ["opt.noCountdown"] = "yok",

            ["theme.dark"] = "Koyu",
            ["theme.light"] = "Açık",
            ["theme.warm"] = "Sıcak",
            ["theme.minimal"] = "Minimal",

            ["pos.topLeft"] = "Sol üst",
            ["pos.topCenter"] = "Üst orta",
            ["pos.topRight"] = "Sağ üst",
            ["pos.bottomLeft"] = "Sol alt",
            ["pos.center"] = "Orta",
            ["pos.bottomRight"] = "Sağ alt",

            ["accent.theme"] = "Temadan",
            ["accent.teal"] = "Turkuaz",
            ["accent.blue"] = "Mavi",
            ["accent.amber"] = "Kehribar",
            ["accent.rose"] = "Gül",
            ["accent.green"] = "Yeşil",
            ["accent.violet"] = "Mor",

            ["lang.auto"] = "Otomatik",
            ["dlg.saveFailed"] = "{0} yazılamadı.\nDeğişiklikler şimdi geçerli ama yeniden başlatınca kaybolacak.",
            ["dlg.registryFailed"] = "Başlangıç anahtarı yazılamadı. Geri kalanı kaydedildi.",
            ["file.pickSound"] = "Ses seç",
            ["file.wavFilter"] = "WAV ses (*.wav)|*.wav"
        };
    }
}
