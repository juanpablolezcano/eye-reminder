using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using TextBlock = System.Windows.Controls.TextBlock;
using Style = System.Windows.Style;
using FontFamily = System.Windows.Media.FontFamily;
using Brushes = System.Windows.Media.Brushes;

namespace EyeReminder;

/// <summary>
/// Builds a theme by hand. Every field is a plain #AARRGGBB box with a swatch, and the
/// preview above is the reminder card's own shapes, so the result is visible while editing.
/// </summary>
public partial class ThemeEditorWindow : Window
{
    /// <summary>Id of the theme that was saved, so the caller can select it.</summary>
    public string? SavedId { get; private set; }

    private readonly string _originalId;
    private readonly Palette _seedPalette;
    private readonly bool _isCopy;
    private readonly List<(string Key, TextBox Input, Border Swatch)> _fields = new();

    public ThemeEditorWindow(ThemeOption seed, bool canDelete)
    {
        InitializeComponent();

        _originalId = seed.Id;
        _seedPalette = seed.Palette;

        // The four shipped themes are read only: editing one starts a copy instead.
        _isCopy = Themes.IsShipped(seed.Id);

        var accent = Themes.For(new Settings { Theme = seed.Id }).Accent;
        Icon = EyeIcon.ForWindow(System.Drawing.Color.FromArgb(accent.A, accent.R, accent.G, accent.B));

        Title = Ui.T("win.themeTitle");
        LblName.Text = Ui.T("lbl.themeName");
        HintEditor.Text = Ui.T("hint.themeEditor");
        DeleteButton.Content = Ui.T("btn.delete");
        CancelButton.Content = Ui.T("btn.cancel");
        SaveButton.Content = Ui.T("btn.save");

        NameInput.Text = _isCopy ? Themes.UniqueName(seed.Caption) : seed.Caption;
        DeleteButton.IsEnabled = canDelete && !_isCopy;
        ResetButton.Content = Ui.T("btn.resetColours");

        BuildColourFields(seed.Palette);

        NameInput.TextChanged += (_, _) => UpdatePreview();
        CancelButton.Click += (_, _) => Close();
        ResetButton.Click += (_, _) => ResetColours();
        DeleteButton.Click += (_, _) => Delete();
        SaveButton.Click += (_, _) => Save();

        UpdatePreview();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Win32.UseDarkTitleBar(new WindowInteropHelper(this).Handle);
    }

    private void BuildColourFields(Palette palette)
    {
        (string Key, Color Value)[] slots =
        {
            ("lbl.colourBackground", palette.Background),
            ("lbl.colourTitle",      palette.Title),
            ("lbl.colourAccent",     palette.Accent),
            ("lbl.colourBorder",     palette.Border),
            ("lbl.colourMessage",    palette.Message),
            ("lbl.colourTrack",      palette.Track)
        };

        for (var i = 0; i < slots.Length; i++)
        {
            var (key, value) = slots[i];

            var label = new TextBlock
            {
                Text = Ui.T(key),
                FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromArgb(0xC8, 0xFF, 0xFF, 0xFF)),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var swatch = new Border
            {
                Width = 26,
                CornerRadius = new CornerRadius(7),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(8, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = Ui.T("win.colourTitle")
            };

            var slot = i;
            swatch.MouseLeftButtonDown += (_, _) => PickColour(slot);

            var input = new TextBox
            {
                Text = ToHex(value),
                Style = (Style)FindResource("Box"),
                FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace")
            };

            input.TextChanged += (_, _) => UpdatePreview();

            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
            DockPanel.SetDock(swatch, Dock.Right);
            row.Children.Add(swatch);
            row.Children.Add(input);

            var cell = new StackPanel();
            cell.Children.Add(label);
            cell.Children.Add(row);

            // Two columns, three rows: background/title/accent on the left, the rest right.
            Grid.SetColumn(cell, i < 3 ? 0 : 2);
            Grid.SetRow(cell, i % 3);
            ColourRows.Children.Add(cell);

            _fields.Add((key, input, swatch));
        }
    }

    /// <summary>Opens the picker on one slot, seeded with whatever that field holds now.</summary>
    private void PickColour(int slot)
    {
        var (key, input, _) = _fields[slot];
        var current = TryParse(input.Text, out var parsed) ? parsed : Colors.Black;

        var chosen = ColourPickerWindow.Pick(this, current, Ui.T(key));
        if (chosen is null) return;

        input.Text = ToHex(chosen.Value);
    }

    /// <summary>Puts the six fields back to the theme this editor was opened from.</summary>
    private void ResetColours()
    {
        Color[] seed =
        {
            _seedPalette.Background, _seedPalette.Title, _seedPalette.Accent,
            _seedPalette.Border, _seedPalette.Message, _seedPalette.Track
        };

        for (var i = 0; i < _fields.Count && i < seed.Length; i++)
        {
            _fields[i].Input.Text = ToHex(seed[i]);
        }
    }

    private Palette ReadPalette()
    {
        Color At(int index, Color fallback) =>
            TryParse(_fields[index].Input.Text, out var parsed) ? parsed : fallback;

        var builtin = Themes.All[0].Palette;

        return new Palette(
            Background: At(0, builtin.Background),
            Title:      At(1, builtin.Title),
            Accent:     At(2, builtin.Accent),
            Border:     At(3, builtin.Border),
            Message:    At(4, builtin.Message),
            Track:      At(5, builtin.Track));
    }

    private void UpdatePreview()
    {
        var palette = ReadPalette();

        foreach (var (_, input, swatch) in _fields)
        {
            var valid = TryParse(input.Text, out var colour);

            swatch.Background = valid ? new SolidColorBrush(colour) : Brushes.Transparent;
            input.Foreground = valid
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(Color.FromRgb(0xE8, 0x94, 0xB4));
        }

        Preview.Background = new SolidColorBrush(palette.Background);
        Preview.BorderBrush = new SolidColorBrush(palette.Border);

        PreviewEye.Stroke = new SolidColorBrush(palette.Accent);
        PreviewPupil.Fill = new SolidColorBrush(palette.Accent);

        PreviewTitle.Foreground = new SolidColorBrush(palette.Title);
        PreviewCount.Foreground = new SolidColorBrush(palette.Title);
        PreviewMessage.Foreground = new SolidColorBrush(palette.Message);

        PreviewTrack.Background = new SolidColorBrush(palette.Track);
        PreviewFill.Background = new SolidColorBrush(palette.Accent);

        var sample = new Settings { BreakSeconds = 20, Language = OverlayText.Auto };
        PreviewTitle.Text = string.IsNullOrWhiteSpace(NameInput.Text)
            ? OverlayText.Title(sample)
            : NameInput.Text;
        PreviewMessage.Text = OverlayText.Message(sample);
    }

    private void Save()
    {
        var name = NameInput.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            NameInput.Focus();
            return;
        }

        // A shipped theme is never written over: it always saves under a fresh id. One of the
        // user's own keeps its id so settings.json does not have to be rewritten.
        var id = _isCopy ? UniqueId(Slug(name)) : _originalId;

        if (!Themes.Save(id, name, ReadPalette()))
        {
            MessageBox.Show(this, Ui.T("dlg.saveFailed", DataFiles.OverrideFolder),
                AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SavedId = id;
        DialogResult = true;
        Close();
    }

    private void Delete()
    {
        if (!Themes.Remove(_originalId))
        {
            MessageBox.Show(this, Ui.T("dlg.saveFailed", DataFiles.OverrideFolder),
                AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SavedId = Themes.All[0].Id;
        DialogResult = true;
        Close();
    }

    /// <summary>Keeps a fresh id from colliding with a shipped one.</summary>
    private static string UniqueId(string preferred)
    {
        if (!Themes.All.Any(t => t.Id.Equals(preferred, StringComparison.OrdinalIgnoreCase))) return preferred;

        for (var n = 2; n < 100; n++)
        {
            var candidate = preferred + n.ToString(CultureInfo.InvariantCulture);
            if (!Themes.All.Any(t => t.Id.Equals(candidate, StringComparison.OrdinalIgnoreCase))) return candidate;
        }

        return preferred;
    }

    /// <summary>Turns a display name into an id that is safe to keep in settings.json.</summary>
    private static string Slug(string name)
    {
        var cleaned = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
        return string.IsNullOrEmpty(cleaned) ? "Custom" : cleaned;
    }

    private static string ToHex(Color colour) =>
        $"#{colour.A:X2}{colour.R:X2}{colour.G:X2}{colour.B:X2}";

    private static bool TryParse(string? text, out Color colour)
    {
        colour = default;
        var value = text?.Trim();

        if (string.IsNullOrEmpty(value)) return false;
        if (!value.StartsWith('#')) value = "#" + value;

        try
        {
            colour = (Color)ColorConverter.ConvertFromString(value)!;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
