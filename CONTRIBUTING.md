# Contributing

Thanks for looking. Everything lands on `main` through a pull request, reviewed by
the maintainer.

## Getting a change in

1. Fork the repository and branch off `main`.
2. Make the change. `dotnet build -c Release` must pass with no warnings: the
   project runs `latest-recommended` analysers and the tree is currently clean.
3. Open a pull request. The build workflow runs on Windows and has to go green.
4. The maintainer reviews it. Direct pushes to `main` are not accepted from
   anyone, which includes the maintainer.

## What is easy to contribute

**Translations.** The twenty bundled languages were written by one person with no
native review, so corrections are welcome. Overlay copy lives in
[Languages/overlay.json](Languages/overlay.json) and interface strings in
[Languages/ui.json](Languages/ui.json). A new language needs an entry in each
file under the same `code`, and nothing else: the settings window builds its
chips from the list.

**Themes.** [Themes/themes.json](Themes/themes.json) holds the palettes. The app
can write that file itself through the theme editor, so a new theme can be built
in the interface and pasted in.

## House style

- Comments explain why, not what. If a line needs a comment to say what it does,
  the line is the problem.
- No third-party packages. The app depends on .NET and Windows, nothing else, and
  that is a deliberate constraint rather than an accident.
- No binary assets where a few lines of drawing code will do. The tray icon, the
  reminder sound and the application icon are all generated rather than shipped.
