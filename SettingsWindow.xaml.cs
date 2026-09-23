using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

// Disambiguate against the System.Drawing / System.Windows.Forms types that the
// WinForms implicit usings bring into scope.
using Panel = System.Windows.Controls.Panel;
using Orientation = System.Windows.Controls.Orientation;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;
using RadioButton = System.Windows.Controls.RadioButton;

namespace EyeReminder;

public partial class SettingsWindow : Window
{
    /// <summary>Raised when the user asks for a preview, carrying the not-yet-saved values.</summary>
    public event Action<Settings>? PreviewRequested;

    /// <summary>Raised after settings.json was written, so the app can apply them live.</summary>
    public event Action<Settings>? Saved;

    private readonly Settings _working;
    private RadioButton? _autoLanguageChip;
    private bool _loaded;

    public SettingsWindow(Settings current)
    {
        InitializeComponent();

        // Work on a copy so Cancel leaves the running configuration untouched.
        _working = current.Clone();

        // Same mark as the tray, in the same accent, so the window is recognisable at a glance.
        var accent = Themes.For(_working).Accent;
        Icon = EyeIcon.ForWindow(System.Drawing.Color.FromArgb(accent.A, accent.R, accent.G, accent.B));

        Ui.Use(_working.Language);

        BuildLanguageChips();
        BuildOptionChips();
        ApplyUiText();
        LoadValues();
        WireEvents();

        _loaded = true;
    }

    /// <summary>
    /// Pushes the current chrome language into every static caption. Called at construction
    /// and again whenever the language chip changes, so the dialog retranslates in place.
    /// </summary>
    private void ApplyUiText()
    {
        Title = Ui.T("win.title");

        SecReminder.Text = Ui.T("sec.reminder");
        SecIdle.Text = Ui.T("sec.idle");
        SecLanguage.Text = Ui.T("sec.language");
        SecStyle.Text = Ui.T("sec.style");
        SecPosition.Text = Ui.T("sec.position");
        SecTexts.Text = Ui.T("sec.texts");
        SecSound.Text = Ui.T("sec.sound");
        SecSystem.Text = Ui.T("sec.system");
        SecPrivacy.Text = Ui.T("sec.privacy");

        LblFrequency.Text = Ui.T("lbl.frequency");
        LblBreak.Text = Ui.T("lbl.break");
        LblCountdown.Text = Ui.T("lbl.countdown");
        LblPauseAfter.Text = Ui.T("lbl.pauseAfter");
        LblAccent.Text = Ui.T("lbl.accent");
        LblAnimation.Text = Ui.T("lbl.animation");
        LblSize.Text = Ui.T("lbl.size");
        LblOpacity.Text = Ui.T("lbl.opacity");
        LblVolume.Text = Ui.T("lbl.volume");
        LblTextTitle.Text = Ui.T("lbl.textTitle");
        LblTextCountdown.Text = Ui.T("lbl.textCountdown");
        LblTextBreak.Text = Ui.T("lbl.textBreak");

        HintCountdown.Text = Ui.T("hint.countdown");
        HintIdle.Text = Ui.T("hint.idle");
        HintCustomText.Text = Ui.T("hint.customText");
        HintSound.Text = Ui.T("hint.sound");
        HintFullscreen.Text = Ui.T("hint.fullscreen");
        HintCapture.Text = Ui.T("hint.capture");
        StartupHint.Text = Ui.T("hint.startup", Environment.ProcessPath);

        IdleCheck.Content = Ui.T("chk.idle");
        StartupNoticeCheck.Content = Ui.T("chk.startupNotice");
        AllScreensCheck.Content = Ui.T("chk.allScreens");
        SoundCheck.Content = Ui.T("chk.soundStart");
        SoundFinishCheck.Content = Ui.T("chk.soundEnd");
        CustomTextCheck.Content = Ui.T("chk.customText");
        StartupCheck.Content = Ui.T("chk.startup");
        FullscreenCheck.Content = Ui.T("chk.fullscreen");
        CaptureCheck.Content = Ui.T("chk.capture");

        BrowseButton.Content = Ui.T("btn.browse");
        TestSoundButton.Content = Ui.T("btn.test");
        PreviewButton.Content = Ui.T("btn.preview");
        CancelButton.Content = Ui.T("btn.cancel");
        SaveButton.Content = Ui.T("btn.save");
        CustomiseButton.Content = Ui.T("btn.customise");
        RestoreButton.Content = Ui.T("btn.restoreDefaults");

        if (_autoLanguageChip is not null) _autoLanguageChip.Content = Ui.T("lang.auto");
    }

    /// <summary>Re-reads the language chip, retranslates the chrome and relabels the chips.</summary>
    private void ApplyUiLanguage()
    {
        Ui.Use(ReadChip(LanguagePanel, OverlayText.Auto));

        // Theme, position and accent chips carry translated captions, so they get rebuilt.
        var theme = ReadChip(ThemePanel, "Dark");
        var position = ReadChip(PositionPanel, "TopCenter");
        var accent = ReadChip(AccentPanel, "");
        var animation = ReadChip(AnimationPanel, OverlayAnimation.Fade);

        BuildOptionChips();

        SelectChip(ThemePanel, theme);
        SelectChip(PositionPanel, position);
        SelectChip(AccentPanel, accent);
        SelectChip(AnimationPanel, animation);

        ApplyUiText();
        UpdateReadouts();
        RefreshTextPreview();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Win32.UseDarkTitleBar(new WindowInteropHelper(this).Handle);
    }

    // ---- building ---------------------------------------------------------

    /// <summary>Language names stay in their own language, so these are built once.</summary>
    private void BuildLanguageChips()
    {
        _autoLanguageChip = AddChip(LanguagePanel, "language", OverlayText.Auto, Ui.T("lang.auto"));

        foreach (var pack in OverlayText.All)
        {
            AddChip(LanguagePanel, "language", pack.Code, pack.Label);
        }
    }

    private void BuildOptionChips()
    {
        ThemePanel.Children.Clear();
        PositionPanel.Children.Clear();
        AccentPanel.Children.Clear();

        AnimationPanel.Children.Clear();

        foreach (var theme in Themes.All)
        {
            AddChip(ThemePanel, "theme", theme.Id, theme.Caption);
        }

        foreach (var (id, key) in Themes.Positions)
        {
            AddChip(PositionPanel, "position", id, Ui.T(key));
        }

        foreach (var (id, key) in OverlayAnimation.All)
        {
            AddChip(AnimationPanel, "animation", id, Ui.T(key));
        }

        foreach (var accent in Themes.Accents)
        {
            var label = accent.Caption;
            var hex = accent.Hex;

            // Each accent chip carries its own swatch so the colour is picked by sight.
            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(new Ellipse
            {
                Width = 11,
                Height = 11,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Fill = string.IsNullOrEmpty(hex)
                    ? Brushes.Transparent
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!),
                Stroke = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                StrokeThickness = 1
            });
            content.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });

            AddChip(AccentPanel, "accent", hex, label, content);
        }
    }

    private RadioButton AddChip(Panel panel, string group, string value, string label, object? content = null)
    {
        var chip = new RadioButton
        {
            Style = (Style)FindResource("Chip"),
            GroupName = group,
            Tag = value,
            Content = content ?? label
        };

        panel.Children.Add(chip);
        return chip;
    }

    private static void SelectChip(Panel panel, string value)
    {
        var chips = panel.Children.OfType<RadioButton>().ToList();
        var match = chips.FirstOrDefault(c =>
            string.Equals((string)c.Tag, value, StringComparison.OrdinalIgnoreCase));

        (match ?? chips.FirstOrDefault())?.SetCurrentValue(RadioButton.IsCheckedProperty, true);
    }

    private static string ReadChip(Panel panel, string fallback)
    {
        var checkedChip = panel.Children.OfType<RadioButton>().FirstOrDefault(c => c.IsChecked == true);
        return checkedChip is null ? fallback : (string)checkedChip.Tag;
    }

    // ---- values -----------------------------------------------------------

    private void LoadValues()
    {
        IntervalSlider.Value = Math.Round(_working.IntervalMinutes);
        BreakSlider.Value = _working.BreakSeconds;
        CountdownSlider.Value = _working.CountdownSeconds;
        IdleSlider.Value = _working.IdleMinutes;
        ScaleSlider.Value = _working.Scale;
        OpacitySlider.Value = _working.Opacity;
        VolumeSlider.Value = _working.SoundVolume;

        SelectChip(ThemePanel, _working.Theme);
        SelectChip(PositionPanel, _working.Position);
        SelectChip(AnimationPanel, OverlayAnimation.Normalise(_working.Animation));
        SelectChip(AccentPanel, _working.AccentColor ?? "");

        AllScreensCheck.IsChecked = _working.AllScreens;
        IdleCheck.IsChecked = _working.PauseWhenIdle;
        StartupNoticeCheck.IsChecked = _working.ShowStartupNotice;
        FullscreenCheck.IsChecked = _working.PauseWhenFullscreen;
        CaptureCheck.IsChecked = _working.HideFromScreenShare;
        SoundCheck.IsChecked = _working.Sound;
        SoundFinishCheck.IsChecked = _working.SoundOnFinish;
        SoundFileBox.Text = _working.SoundFile ?? "";

        SelectChip(LanguagePanel, _working.Language);

        var hasOverrides = !string.IsNullOrWhiteSpace(_working.TitleOverride)
                           || !string.IsNullOrWhiteSpace(_working.CountdownOverride)
                           || !string.IsNullOrWhiteSpace(_working.MessageOverride);

        CustomTextCheck.IsChecked = hasOverrides;

        if (hasOverrides)
        {
            // Fill any field they left on the default so all three are editable side by side.
            TitleInput.Text = Or(_working.TitleOverride, OverlayText.TitleTemplate(WithoutOverrides()));
            CountdownInput.Text = Or(_working.CountdownOverride, OverlayText.CountdownTemplate(WithoutOverrides()));
            MessageInput.Text = Or(_working.MessageOverride, OverlayText.MessageTemplate(WithoutOverrides()));
        }

        RefreshTextPreview();

        // Only the state here: the caption below it is set by ApplyUiText, which must not
        // be undone by a hardcoded string.
        StartupCheck.IsChecked = StartupManager.IsEnabled();

        UpdateReadouts();
    }

    private static string Or(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    /// <summary>A copy of the working settings with overrides stripped, to read language defaults.</summary>
    private Settings WithoutOverrides()
    {
        var bare = _working.Clone();
        bare.Language = ReadChip(LanguagePanel, OverlayText.Auto);
        bare.BreakSeconds = (int)BreakSlider.Value;
        bare.TitleOverride = "";
        bare.CountdownOverride = "";
        bare.MessageOverride = "";
        return bare;
    }

    /// <summary>
    /// When the user is not writing their own copy, the boxes show (read-only) exactly what
    /// the chosen language and break length will put on the overlay.
    /// </summary>
    private void RefreshTextPreview()
    {
        var custom = CustomTextCheck.IsChecked == true;

        TitleInput.IsEnabled = custom;
        CountdownInput.IsEnabled = custom;
        MessageInput.IsEnabled = custom;

        if (custom) return;

        // Templates, not resolved text: ticking the box must hand the user a {0} to keep.
        var defaults = WithoutOverrides();
        TitleInput.Text = OverlayText.TitleTemplate(defaults);
        CountdownInput.Text = OverlayText.CountdownTemplate(defaults);
        MessageInput.Text = OverlayText.MessageTemplate(defaults);
    }

    private void UpdateReadouts()
    {
        IntervalValue.Text = $"{IntervalSlider.Value:0} {Ui.T("unit.min")}";
        BreakValue.Text = $"{BreakSlider.Value:0} {Ui.T("unit.sec")}";
        CountdownValue.Text = CountdownSlider.Value < 1
            ? Ui.T("opt.noCountdown")
            : $"{CountdownSlider.Value:0} {Ui.T("unit.sec")}";
        IdleValue.Text = $"{IdleSlider.Value:0} {Ui.T("unit.min")}";
        ScaleValue.Text = $"{ScaleSlider.Value * 100:0} %";
        OpacityValue.Text = $"{OpacitySlider.Value * 100:0} %";
        VolumeValue.Text = $"{VolumeSlider.Value * 100:0} %";
    }

    /// <summary>Snapshots the form into a Settings instance.</summary>
    private Settings Collect()
    {
        var result = _working.Clone();

        result.IntervalMinutes = IntervalSlider.Value;
        result.BreakSeconds = (int)BreakSlider.Value;
        result.CountdownSeconds = (int)CountdownSlider.Value;
        result.IdleMinutes = IdleSlider.Value;
        result.Scale = ScaleSlider.Value;
        result.Opacity = OpacitySlider.Value;
        result.SoundVolume = VolumeSlider.Value;

        result.Theme = ReadChip(ThemePanel, "Dark");
        result.Position = ReadChip(PositionPanel, "TopCenter");
        result.Animation = ReadChip(AnimationPanel, OverlayAnimation.Fade);
        result.AccentColor = ReadChip(AccentPanel, "");

        result.AllScreens = AllScreensCheck.IsChecked == true;
        result.PauseWhenIdle = IdleCheck.IsChecked == true;
        result.ShowStartupNotice = StartupNoticeCheck.IsChecked == true;
        result.PauseWhenFullscreen = FullscreenCheck.IsChecked == true;
        result.HideFromScreenShare = CaptureCheck.IsChecked == true;
        result.Sound = SoundCheck.IsChecked == true;
        result.SoundOnFinish = SoundFinishCheck.IsChecked == true;
        result.SoundFile = SoundFileBox.Text.Trim();

        result.Language = ReadChip(LanguagePanel, OverlayText.Auto);

        // Overrides are only stored when the user opted in; otherwise the language decides.
        var custom = CustomTextCheck.IsChecked == true;
        result.TitleOverride = custom ? TitleInput.Text.Trim() : "";
        result.CountdownOverride = custom ? CountdownInput.Text.Trim() : "";
        result.MessageOverride = custom ? MessageInput.Text.Trim() : "";

        return result.Sanitized();
    }

    // ---- events -----------------------------------------------------------

    private void WireEvents()
    {
        IntervalSlider.ValueChanged += (_, _) => { if (_loaded) UpdateReadouts(); };
        // The break length shows up inside the message, so the preview follows the slider.
        BreakSlider.ValueChanged += (_, _) =>
        {
            if (!_loaded) return;
            UpdateReadouts();
            RefreshTextPreview();
        };

        foreach (var chip in LanguagePanel.Children.OfType<RadioButton>())
        {
            chip.Checked += (_, _) => { if (_loaded) ApplyUiLanguage(); };
        }

        CustomTextCheck.Checked += (_, _) => { if (_loaded) RefreshTextPreview(); };
        CustomTextCheck.Unchecked += (_, _) => { if (_loaded) RefreshTextPreview(); };
        CountdownSlider.ValueChanged += (_, _) => { if (_loaded) UpdateReadouts(); };
        IdleSlider.ValueChanged += (_, _) => { if (_loaded) UpdateReadouts(); };
        ScaleSlider.ValueChanged += (_, _) => { if (_loaded) UpdateReadouts(); };
        OpacitySlider.ValueChanged += (_, _) => { if (_loaded) UpdateReadouts(); };
        VolumeSlider.ValueChanged += (_, _) => { if (_loaded) UpdateReadouts(); };

        BrowseButton.Click += (_, _) =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Ui.T("file.pickSound"),
                Filter = Ui.T("file.wavFilter"),
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Media"
            };

            if (dialog.ShowDialog(this) == true)
            {
                SoundFileBox.Text = dialog.FileName;
            }
        };

        // Plays with the volume and file currently on the form, saved or not.
        TestSoundButton.Click += (_, _) =>
        {
            var probe = Collect();
            probe.Sound = true;
            Chime.Play(probe);
        };

        CustomiseButton.Click += (_, _) => CustomiseTheme();
        RestoreButton.Click += (_, _) => RestoreDefaults();

        PreviewButton.Click += (_, _) => PreviewRequested?.Invoke(Collect());

        CancelButton.Click += (_, _) => Close();

        SaveButton.Click += (_, _) => Apply();
    }

    /// <summary>
    /// Puts every setting back to the factory values and drops any customised themes, after
    /// asking, since there is no undo for it.
    /// </summary>
    private void RestoreDefaults()
    {
        var answer = MessageBox.Show(this, Ui.T("dlg.restoreDefaults"), AppInfo.Name,
            MessageBoxButton.OKCancel, MessageBoxImage.Question);

        if (answer != MessageBoxResult.OK) return;

        // Applied and written in one go. Deleting the theme file but leaving the settings
        // pending until Save would leave the two halves out of step if Cancel followed.
        Themes.RestoreDefaults();

        var fresh = new Settings();
        fresh.Save();

        Ui.Use(fresh.Language);
        _working.CopyFrom(fresh);

        BuildOptionChips();
        ApplyUiText();
        LoadValues();

        Saved?.Invoke(fresh.Clone());
    }

    /// <summary>Opens the theme editor seeded from whichever theme is selected right now.</summary>
    private void CustomiseTheme()
    {
        var id = ReadChip(ThemePanel, Themes.All[0].Id);
        var seed = Themes.All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? Themes.All[0];

        // Only a set the user already owns can have entries removed from it.
        var editor = new ThemeEditorWindow(seed, canDelete: Themes.HasCustomFile && Themes.All.Count > 1)
        {
            Owner = this
        };

        if (editor.ShowDialog() != true) return;

        BuildOptionChips();
        SelectChip(ThemePanel, editor.SavedId ?? id);
        SelectChip(PositionPanel, ReadChip(PositionPanel, "TopCenter"));
    }

    private void Apply()
    {
        var updated = Collect();

        if (!StartupManager.SetEnabled(StartupCheck.IsChecked == true))
        {
            MessageBox.Show(this, Ui.T("dlg.registryFailed"),
                AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        if (!updated.Save())
        {
            MessageBox.Show(this, Ui.T("dlg.saveFailed", Settings.FilePath),
                AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        Saved?.Invoke(updated);
        Close();
    }
}
