using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using WF = System.Windows.Forms;

namespace EyeReminder;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Reliability", "CA1001:Types that own disposable fields should be disposable",
    Justification = "The tray icon lives as long as the process and is disposed in OnExit. " +
                    "Application has no Dispose for the framework to call.")]
public partial class App : System.Windows.Application
{
    private static Mutex? _singleInstance;

    /// <summary>Command line switch that opens the settings window on launch.</summary>
    private const string SettingsArgument = "--settings";

    /// <summary>How long the "running in the background" card stays up at launch.</summary>
    private const int StartupNoticeSeconds = 5;

    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ManualPause = TimeSpan.FromHours(1);

    private readonly DispatcherTimer _ticker = new() { Interval = TickInterval };
    private readonly List<OverlayWindow> _overlays = new();

    private Settings _settings = new();
    private WF.NotifyIcon? _tray;
    private SettingsWindow? _settingsWindow;

    private DateTime _nextReminder;
    private DateTime? _pausedUntil;
    private bool _breakInProgress;
    private bool _idle;

    /// <summary>
    /// Identifies the current break. A break that gets replaced (by a preview or by
    /// "Test now") must not run the scheduling logic when its windows finally close.
    /// </summary>
    private int _breakGeneration;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _singleInstance = new Mutex(true, AppInfo.SingleInstanceMutex, out var isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }

        _settings = Settings.Load();
        Ui.Use(_settings.Language);

        BuildTray();
        ScheduleNext();

        _ticker.Tick += OnTick;
        _ticker.Start();

        if (e.Args.Any(arg => string.Equals(arg, SettingsArgument, StringComparison.OrdinalIgnoreCase)))
        {
            OpenSettings();
            return;
        }

        if (_settings.ShowStartupNotice)
        {
            ShowStartupNotice();
        }
    }

    /// <summary>
    /// Tells the user the app is alive, since it otherwise starts with no window at all.
    /// Same card as the reminder, silent, and it fades out by itself.
    /// </summary>
    private void ShowStartupNotice()
    {
        var screen = WF.Screen.PrimaryScreen ?? WF.Screen.AllScreens[0];

        var notice = OverlayWindow.Notice(
            _settings,
            screen,
            AppInfo.Name,
            OverlayText.Background(_settings),
            seconds: StartupNoticeSeconds);

        notice.SettingsRequested += OpenSettings;
        notice.Closed += (_, _) => _overlays.Remove(notice);

        _overlays.Add(notice);
        notice.Show();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _ticker.Stop();

        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }

        _singleInstance?.Dispose();
    }

    // ---- scheduling -------------------------------------------------------

    private void OnTick(object? sender, EventArgs e)
    {
        if (_pausedUntil is { } until)
        {
            if (DateTime.Now < until) return;
            Resume();
        }

        if (IsIdle()) return;

        if (!_breakInProgress && DateTime.Now >= _nextReminder)
        {
            ShowBreak();
        }
    }

    /// <summary>
    /// Holds the schedule while the machine is untouched, and restarts the interval on return:
    /// time away from the keyboard is already time away from the screen.
    /// </summary>
    private bool IsIdle()
    {
        if (!_settings.PauseWhenIdle)
        {
            _idle = false;
            return false;
        }

        var idleFor = Win32.GetIdleTime();

        if (idleFor >= TimeSpan.FromMinutes(_settings.IdleMinutes))
        {
            if (!_idle)
            {
                _idle = true;
                DismissAll(); // nobody is there to read it
            }

            return true;
        }

        if (_idle)
        {
            _idle = false;
            ScheduleNext();
        }

        return false;
    }

    private void ScheduleNext()
    {
        _nextReminder = DateTime.Now.AddMinutes(_settings.IntervalMinutes);
    }

    /// <summary>
    /// Shows the break overlay. Any break already on screen is replaced, so this can be
    /// triggered repeatedly from the tray or from the settings preview.
    /// </summary>
    private void ShowBreak(Settings? with = null)
    {
        var settings = with ?? _settings;
        var generation = ++_breakGeneration;

        DismissAll();

        _breakInProgress = true;

        var screens = settings.AllScreens
            ? WF.Screen.AllScreens
            : new[] { WF.Screen.PrimaryScreen ?? WF.Screen.AllScreens[0] };

        var pending = screens.Length;

        var first = true;

        foreach (var screen in screens)
        {
            var overlay = new OverlayWindow(settings, screen);
            overlay.SettingsRequested += OpenSettings;

            // Only the first overlay drives the audio, so multi-monitor does not stack sounds.
            if (first && settings.SoundOnFinish)
            {
                overlay.BreakEnded += () => Chime.Play(settings);
                first = false;
            }

            overlay.Closed += (_, _) =>
            {
                _overlays.Remove(overlay);

                if (--pending > 0) return;

                // A newer break already took over; leave the schedule to that one.
                if (generation != _breakGeneration) return;

                _breakInProgress = false;
                ScheduleNext();
            };

            _overlays.Add(overlay);
            overlay.Show(); // ShowActivated=False + WS_EX_NOACTIVATE: never takes focus
        }

        Chime.Play(settings);
    }

    private void DismissAll()
    {
        foreach (var overlay in _overlays.ToArray())
        {
            overlay.Dismiss();
        }
    }

    private void Pause(TimeSpan duration)
    {
        DismissAll();
        _pausedUntil = DateTime.Now.Add(duration);
    }

    private void Resume()
    {
        _pausedUntil = null;
        ScheduleNext();
    }

    // ---- settings ---------------------------------------------------------

    private void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var window = new SettingsWindow(_settings);
        window.PreviewRequested += preview => ShowBreak(preview);
        window.Saved += ApplySettings;
        window.Closed += (_, _) => _settingsWindow = null;

        _settingsWindow = window;
        window.Show();
    }

    private void ApplySettings(Settings updated)
    {
        var languageChanged = !string.Equals(_settings.Language, updated.Language, StringComparison.OrdinalIgnoreCase);

        _settings = updated;

        if (languageChanged)
        {
            Ui.Use(updated.Language);
            RefreshTray();
        }

        // The new interval takes effect from now rather than from the last reminder.
        ScheduleNext();
    }

    // ---- tray -------------------------------------------------------------

    private void BuildTray()
    {
        _tray = new WF.NotifyIcon
        {
            Icon = EyeIcon.ForTray(),
            Visible = true
        };

        _tray.DoubleClick += (_, _) => OpenSettings();

        RefreshTray();
    }

    /// <summary>
    /// Rebuilds the menu in the current chrome language. Cheap enough to just recreate
    /// rather than track every item, and it runs only at startup and on a language change.
    /// </summary>
    private void RefreshTray()
    {
        if (_tray is null) return;

        var menu = new WF.ContextMenuStrip { ShowImageMargin = false };

        var status = new WF.ToolStripMenuItem { Enabled = false };
        var pause = new WF.ToolStripMenuItem(string.Empty, null, (_, _) =>
        {
            if (_pausedUntil is null) Pause(ManualPause);
            else Resume();
        });

        menu.Items.Add(status);
        menu.Items.Add(new WF.ToolStripSeparator());
        menu.Items.Add(new WF.ToolStripMenuItem(Ui.T("tray.test"), null, (_, _) => ShowBreak()));
        menu.Items.Add(new WF.ToolStripMenuItem(Ui.T("tray.reset"), null, (_, _) =>
        {
            DismissAll();
            ScheduleNext();
        }));
        menu.Items.Add(pause);
        menu.Items.Add(new WF.ToolStripSeparator());
        menu.Items.Add(new WF.ToolStripMenuItem(Ui.T("tray.settings"), null, (_, _) => OpenSettings()));
        menu.Items.Add(new WF.ToolStripMenuItem(Ui.T("tray.exit"), null, (_, _) => Shutdown()));

        // Both labels depend on live state, so they are filled in as the menu opens rather
        // than pushed from every place that changes that state.
        menu.Opening += (_, _) =>
        {
            status.Text = StatusLine();
            pause.Text = _pausedUntil is null ? Ui.T("tray.pause") : Ui.T("tray.resume");
        };

        var previous = _tray.ContextMenuStrip;
        _tray.ContextMenuStrip = menu;
        previous?.Dispose();

        _tray.Text = Ui.T("tray.tooltip");
    }

    private string StatusLine()
    {
        if (_pausedUntil is { } until)
        {
            return Ui.T("tray.pausedUntil", until.ToString("HH:mm", CultureInfo.CurrentCulture));
        }

        if (_idle)
        {
            return Ui.T("tray.idle");
        }

        var remaining = _nextReminder - DateTime.Now;
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

        var formatted = remaining.TotalHours >= 1
            ? remaining.ToString(@"h\:mm\:ss", CultureInfo.CurrentCulture)
            : remaining.ToString(@"mm\:ss", CultureInfo.CurrentCulture);

        return Ui.T("tray.next", formatted);
    }
}
