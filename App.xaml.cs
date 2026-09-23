using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using WF = System.Windows.Forms;

namespace EyeReminder;

public partial class App : System.Windows.Application
{
    private static Mutex? _singleInstance;

    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly List<OverlayWindow> _overlays = new();

    private Settings _settings = new();
    private WF.NotifyIcon? _tray;
    private WF.ToolStripMenuItem? _statusItem;
    private WF.ToolStripMenuItem? _pauseItem;
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
        _singleInstance = new Mutex(true, @"Local\EyeReminder.SingleInstance", out var isFirst);
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

        // "EyeReminder.exe --settings" goes straight to the configuration window.
        if (e.Args.Any(arg => string.Equals(arg, "--settings", StringComparison.OrdinalIgnoreCase)))
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
    /// Same click-through card as the reminder, silent, and it fades out by itself.
    /// </summary>
    private void ShowStartupNotice()
    {
        var screen = WF.Screen.PrimaryScreen ?? WF.Screen.AllScreens[0];

        var notice = OverlayWindow.Notice(
            _settings,
            screen,
            AppInfo.Name,
            Strings.Background(_settings),
            seconds: 5);

        _overlays.Add(notice);
        notice.Closed += (_, _) => _overlays.Remove(notice);
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
        if (_pauseItem is not null) _pauseItem.Text = Ui.T("tray.resume");
    }

    private void Resume()
    {
        _pausedUntil = null;
        if (_pauseItem is not null) _pauseItem.Text = Ui.T("tray.pause");
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
            Icon = CreateEyeIcon(),
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

        _statusItem = new WF.ToolStripMenuItem("—") { Enabled = false };
        _pauseItem = new WF.ToolStripMenuItem(
            _pausedUntil is null ? Ui.T("tray.pause") : Ui.T("tray.resume"), null, (_, _) =>
            {
                if (_pausedUntil is null) Pause(TimeSpan.FromHours(1));
                else Resume();
            });

        menu.Items.Add(_statusItem);
        menu.Items.Add(new WF.ToolStripSeparator());
        menu.Items.Add(new WF.ToolStripMenuItem(Ui.T("tray.test"), null, (_, _) => ShowBreak()));
        menu.Items.Add(new WF.ToolStripMenuItem(Ui.T("tray.reset"), null, (_, _) =>
        {
            DismissAll();
            ScheduleNext();
        }));
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new WF.ToolStripSeparator());
        menu.Items.Add(new WF.ToolStripMenuItem(Ui.T("tray.settings"), null, (_, _) => OpenSettings()));
        menu.Items.Add(new WF.ToolStripMenuItem(Ui.T("tray.exit"), null, (_, _) => Shutdown()));

        menu.Opening += (_, _) => _statusItem.Text = StatusLine();

        _tray.ContextMenuStrip?.Dispose();
        _tray.ContextMenuStrip = menu;
        _tray.Text = Ui.T("tray.tooltip");
    }

    private string StatusLine()
    {
        if (_pausedUntil is { } until)
        {
            return Ui.T("tray.pausedUntil", until.ToString("HH:mm"));
        }

        if (_idle)
        {
            return Ui.T("tray.idle");
        }

        var remaining = _nextReminder - DateTime.Now;
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

        var formatted = remaining.TotalHours >= 1
            ? remaining.ToString(@"h\:mm\:ss")
            : remaining.ToString(@"mm\:ss");

        return Ui.T("tray.next", formatted);
    }

    /// <summary>Draws the tray icon at runtime so the app ships without binary assets.</summary>
    private static Icon CreateEyeIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var pen = new Pen(Color.FromArgb(240, 127, 212, 196), 2.6f);
            using var brush = new SolidBrush(Color.FromArgb(240, 127, 212, 196));

            using var eye = new GraphicsPath();
            eye.AddBezier(2, 16, 10, 5, 22, 5, 30, 16);
            eye.AddBezier(30, 16, 22, 27, 10, 27, 2, 16);
            g.DrawPath(pen, eye);

            g.FillEllipse(brush, 11, 11, 10, 10);
        }

        // Copy the handle-backed icon so the GDI handle can be released immediately.
        var handle = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }
}

internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
