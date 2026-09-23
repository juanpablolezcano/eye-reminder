# EyeReminder

A desktop overlay for the **20/20/20 rule**: every 20 minutes, look at something
about 20 feet (6 m) away for 20 seconds.

The card floats above everything else and **never takes focus**, so it cannot
interrupt what you are typing. Clicking it dismisses it, which is one gesture away
when a reminder lands in the middle of a call.

## The card

| Where you click | What happens |
|---|---|
| Anywhere on the card | dismisses it |
| The X | dismisses it |
| The gear | opens the settings window |

## How the overlay works

The window is created with these Win32 extended styles ([Win32.cs](Win32.cs)):

| Flag | Effect |
|---|---|
| `WS_EX_NOACTIVATE` | never takes focus, not even when clicked |
| `WS_EX_TOOLWINDOW` | hidden from Alt+Tab and the taskbar |
| `WS_EX_LAYERED` | real transparency and rounded corners |

`WS_EX_NOACTIVATE` is what matters: the card can be clicked without the window
you were working in losing the foreground. Only the card's own rectangle takes
the mouse; the rest of the desktop is untouched.

It is then positioned with `SetWindowPos` + `SWP_NOACTIVATE` in physical pixels,
which keeps it correct on multi-monitor setups with mixed DPI.

## Dependencies

**None outside Microsoft.** Everything comes from .NET 10 and Windows itself:

| What | Where from |
|---|---|
| WPF (`UseWPF`) | .NET 10 Windows Desktop |
| WinForms (`UseWindowsForms`) | .NET 10 Windows Desktop; only for `NotifyIcon` and `Screen` |
| `System.Text.Json` | .NET runtime |
| `System.Media.SoundPlayer` | .NET runtime |
| `System.Drawing` | .NET runtime; draws the tray icon at runtime |
| `Microsoft.Win32.Registry` | .NET runtime; "start with Windows" |
| `user32.dll`, `dwmapi.dll` | Windows, through P/Invoke |

`dotnet list package` reports no package the project asked for; the only entry is
`Microsoft.NET.ILLink.Tasks`, which the SDK adds by itself and is a build-time
tool, not code that ends up in the executable.

## Building and running

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) on Windows.

```powershell
dotnet publish -c Release -o publish
.\publish\EyeReminder.exe
```

It opens no window at startup: it lives as a **system tray icon** next to the
clock, and shows a short "running in the background" card so you know it started.

| Action | Result |
|---|---|
| Double-click the tray icon | opens the settings window |
| Right-click | menu |
| `EyeReminder.exe --settings` | opens settings directly |

Tray menu:

- **Next break in mm:ss**: time remaining
- **Test now**: fires the card, repeatable as often as you like
- **Restart timer**: starts the interval over
- **Pause for 1 hour** / **Resume**
- **Settings...**
- **Exit**

## Settings

Everything is configured from the window (**Settings...** in the tray). Your
preferences are stored in:

```
%APPDATA%\EyeReminder\settings.json
```

**Not** next to the `.exe`, so they survive rebuilds, reinstalls, and moving the
app into a read-only folder. The [settings.json](settings.json) that ships beside
the executable holds the factory defaults and is only used to seed your profile
on first run.

| Key | Default | What it does |
|---|---|---|
| `intervalMinutes` | `20` | minutes between reminders |
| `breakSeconds` | `20` | how long the card stays up |
| `countdownSeconds` | `3` | 3, 2, 1 heads-up before the break; `0` skips it |
| `showStartupNotice` | `true` | "running in the background" card at launch |
| `pauseWhenFullscreen` | `true` | hold the reminder during full screen, presentations and Focus Assist |
| `hideFromScreenShare` | `true` | keep the card out of screen captures and shares |
| `pauseWhenIdle` | `true` | pause while there is no keyboard or mouse input |
| `idleMinutes` | `5` | minutes of inactivity before pausing |
| `language` | `auto` | language code, or `auto` to follow Windows |
| `titleOverride` | `""` | overrides the language's title |
| `countdownOverride` | `""` | overrides the countdown text |
| `messageOverride` | `""` | overrides the break message |
| `theme` | `Dark` | `Dark`, `Light`, `Warm`, `Minimal`, or one of your own |
| `accentColor` | `""` | hex override for the theme accent, e.g. `#E894B4` |
| `animation` | `Fade` | `Fade`, `Slide`, `Scale`, `None` |
| `position` | `TopCenter` | `TopCenter`, `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`, `Center` |
| `scale` | `1.0` | card size (0.7 to 2.0) |
| `allScreens` | `false` | show it on every monitor |
| `opacity` | `0.95` | card opacity |
| `sound` | `true` | play a sound when the break starts |
| `soundFile` | `notification.wav` | sound to play; see below |
| `soundVolume` | `0.45` | volume of the built-in chime (0 to 1) |
| `soundOnFinish` | `true` | replay the sound when the break ends |

**Preview** shows the card with whatever is currently in the form without saving,
so you can try a style, position and size before committing.

A missing or malformed file falls back to the defaults.

## Staying out of the way

Two settings under **Privacy** keep the card from turning up at the wrong moment.

**Do not show during full screen or a presentation** asks the shell what the user
is doing, through `SHQueryUserNotificationState`. The reminder is held back while
an app is full screen, a Direct3D game is running, presentation mode is on, or
Focus Assist is silencing notifications, and it resumes the moment that ends.

**Hide from screen captures and shares** calls `SetWindowDisplayAffinity` with
`WDA_EXCLUDEFROMCAPTURE`. The compositor then refuses to hand the card's pixels
to any capture, so it is absent from a Discord or Teams share, from OBS, and from
screenshots. You still see it on your own screen.

> That needs Windows 10 2004 or newer. On anything older the call fails, a line
> goes into `log.txt`, and the card behaves normally.

## Themes

`theme` names an entry in [Themes/themes.json](Themes/themes.json). Copy that file
into `%APPDATA%\EyeReminder\` and it replaces the built-in set, which is how you
add your own:

```json
{
  "id": "Midnight",
  "label": "Midnight",
  "background": "#F00B1020", "border": "#2E6C7BFF", "title": "#FFEAF0FF",
  "message": "#B0EAF0FF", "accent": "#FF8AA0FF", "track": "#266C7BFF"
}
```

Colours are `#AARRGGBB`, so the leading pair is opacity. Use `labelKey` instead of
`label` to name a translated caption; a plain `label` shows as typed in every
language. Any field you leave out falls back to the dark theme's value.

The theme reaches the tray as well: the right-click menu is painted with the same
palette, and the tray icon is drawn in the accent colour. The icon is not an SVG,
it is drawn with GDI+ at runtime in [EyeIcon.cs](EyeIcon.cs), which is why it can
be recoloured without shipping an asset.

## Startup notice

Because the app starts with no window at all, it shows the reminder card for five
seconds at launch, saying it is running in the background and when the first break
is due. It is not a Windows notification; it is the same card,
silent, and it fades out on its own.

Turn it off with **Show a notice at startup**, or `"showStartupNotice": false`.

Launching the app while it is already running shows the same card, rather than the
second copy exiting without a word. The two instances find each other through a
named event, so the running one does the talking.

## Animations

`animation` picks how the card arrives and leaves: **Fade**, **Slide**, **Scale**
or **None**. Slide takes its direction from where the card sits, so one anchored
at the bottom rises into view instead of dropping in from above.

## Languages and text

The overlay text is **not typed in by hand**: it comes from the selected language,
and the break length is substituted into it. Set the break to 45 seconds and the
card says "45 seconds" by itself.

Bundled languages ([Languages/overlay.json](Languages/overlay.json)):

`es-AR` · `es-419` · `es-MX` · `es-ES` · `en` · `pt-BR` · `pt-PT` · `it` · `fr` ·
`de` · `nl` · `pl` · `ru` · `tr` · `ja` · `ko` · `zh-Hans` · `zh-Hant` · `hi` · `ar`

With `auto` it follows the Windows display language, with sensible fallbacks: any
`es-*` that is not AR, MX or ES lands on `es-419`; `pt-*` lands on Brazil unless it
is `pt-PT`; `zh-TW`, `zh-HK` and `zh-MO` land on Traditional. No match falls back
to English. Arabic renders with the whole card mirrored (RTL).

In English the distance reads **20 feet**; everywhere else, **6 metres**.

The interface follows the same setting: the tray menu and the entire settings
window are translated ([Languages/ui.json](Languages/ui.json), 72 strings per
language). Changing the language retranslates the open window in place.

> The translations were not reviewed by native speakers. If any wording reads
> wrong, the override below is the escape hatch, and a pull request is welcome.

### Where the text lives

Both files sit in `Languages/` and are embedded into the assembly at build time,
so a single-file build carries every language with nothing extra to ship.
[OverlayText.cs](OverlayText.cs) and [Ui.cs](Ui.cs) hold only the lookup logic.

A copy dropped in `%APPDATA%\EyeReminder\` wins over the embedded one, the same way
themes do, so a translation can be fixed or a language added without rebuilding.

A copy dropped in `%APPDATA%yeReminder` wins over the embedded one, the same
way themes work, so a translation can be fixed without rebuilding.

`overlay.json` is a flat list, one object per language:

```json
{ "code": "it", "label": "Italiano", "title": "Riposa gli occhi",
  "countdown": "...", "message": "... {0} secondi", "background": "... {0} min" }
```

`ui.json` maps a language code to a table of keys. Two shorthands keep regional
variants from being copied four times over:

```json
"es-MX": "es-419",                                     // reuse another table as is
"pt-PT": { "$extends": "pt-BR", "btn.save": "Guardar" } // inherit, then override
```

To add a language, add an entry to each file using the same `code`. Nothing else
needs touching: the settings window builds its language chips from the list. If
either file fails to parse, the app still runs; the card falls back to English and
the interface shows raw keys.

### Custom text

Tick **Write my own text** to override any of the three strings. The fields are
pre-filled with the language's **template**, `{0}` included, not with the already
resolved text, so custom wording still tracks the configured duration.

The override is **per field**: change only the message and the title still comes
from the language. Use `{0}` wherever the break length should appear:

```json
{ "language": "it", "messageOverride": "My own text: {0} seconds" }
```

A stray brace breaks nothing: if the format is invalid the text is shown as typed.

## Idle pause

While there is no keyboard or mouse input for `idleMinutes`, the countdown holds.
When you come back the interval **starts over** rather than firing immediately:
time away from the keyboard was already time away from the screen.

This uses `GetLastInputInfo` ([Win32.cs](Win32.cs)), which measures real system
input. The trade-off: **watching a video without touching anything counts as
idle**, so you will not be reminded then. If you watch long videos, untick it.

## Start with Windows

The **Start with Windows** checkbox writes
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, pointing at the current
executable ([StartupManager.cs](StartupManager.cs)). Unticking it removes the key.

> If you move the folder containing the `.exe`, tick the box again to re-point it.

## Sound

`soundFile` takes either a bare file name, resolved next to the executable, or a
full path. The default is `notification.wav`.

**That file is not in this repository** (licensing), so a fresh clone has no
`notification.wav`. That is fine: when the file is missing, [Chime.cs](Chime.cs)
synthesises a soft two-note bell (E5 → B5, exponential decay) in memory instead.
No Windows system sound is ever used: those share their timbre with error alerts.

To use your own, drop a `.wav` next to the `.exe` and point `soundFile` at it, or
press **Browse** in the settings window:

```json
{ "soundFile": "C:\\Windows\\Media\\Windows Notify Calendar.wav" }
```

Only uncompressed PCM `.wav` works, since `System.Media.SoundPlayer` does not decode
MP3. Convert with `ffmpeg -i in.mp3 -acodec pcm_s16le -ar 44100 out.wav`.

Free sources: [Pixabay](https://pixabay.com/sound-effects/search/notification/),
[Mixkit](https://mixkit.co/free-sound-effects/notification/),
[Freesound](https://freesound.org/search/?q=soft+chime&f=type:wav).

## Releases

Pushing a tag builds and publishes automatically
([.github/workflows/release.yml](.github/workflows/release.yml)):

```powershell
git tag v1.0.0
git push origin v1.0.0
```

That produces the self-contained single file, strips everything but the `.exe`, and
attaches it to a GitHub release. Every push to `main` also gets a plain build check.

## Licence

MIT. See [LICENSE](LICENSE).
