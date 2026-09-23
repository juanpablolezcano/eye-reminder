using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WF = System.Windows.Forms;

namespace EyeReminder;

public partial class OverlayWindow : Window
{
    /// <summary>The break itself is over (fired once, before the fade out).</summary>
    public event Action? BreakEnded;

    private enum Phase
    {
        /// <summary>The 3-2-1 heads-up before looking away.</summary>
        Leadin,
        Break,

        /// <summary>A plain message that fades away on its own, with no counter or bar.</summary>
        Notice
    }

    private const int FadeOutMs = 300;

    private readonly Settings _settings;
    private readonly WF.Screen _screen;
    private readonly DispatcherTimer _countdown = new() { Interval = TimeSpan.FromSeconds(1) };

    private Phase _phase;
    private int _remaining;
    private bool _closing;

    public OverlayWindow(Settings settings, WF.Screen screen)
    {
        InitializeComponent();

        _settings = settings;
        _screen = screen;

        if (settings.CountdownSeconds > 0)
        {
            _phase = Phase.Leadin;
            _remaining = settings.CountdownSeconds;
        }
        else
        {
            _phase = Phase.Break;
            _remaining = settings.BreakSeconds;
        }

        TitleText.Text = Strings.Title(settings);
        MessageText.Text = _phase == Phase.Leadin ? Strings.Countdown(settings) : Strings.Message(settings);
        CountdownText.Text = _remaining.ToString();

        // Arabic and friends need the whole card mirrored, not just the text.
        Card.FlowDirection = Strings.Resolve(settings.Language).RightToLeft
            ? System.Windows.FlowDirection.RightToLeft
            : System.Windows.FlowDirection.LeftToRight;

        // The bar tracks the break only, so it stays hidden during the lead-in.
        ProgressRow.Visibility = _phase == Phase.Leadin ? Visibility.Hidden : Visibility.Visible;

        ApplyTheme();

        _countdown.Tick += OnCountdownTick;
    }

    /// <summary>
    /// Builds the "running in the background" card: same look and same click-through
    /// behaviour, but it just shows a line of text and dismisses itself.
    /// </summary>
    public static OverlayWindow Notice(Settings settings, WF.Screen screen, string title, string message, int seconds)
    {
        var window = new OverlayWindow(settings, screen)
        {
            _phase = Phase.Notice,
            _remaining = Math.Max(seconds, 1)
        };

        window.TitleText.Text = title;
        window.MessageText.Text = message;
        window.CountdownText.Visibility = Visibility.Collapsed;
        window.ProgressRow.Visibility = Visibility.Collapsed;

        return window;
    }

    private void ApplyTheme()
    {
        var palette = Themes.For(_settings);

        Card.Background = Themes.Brush(palette.Background);
        Card.BorderBrush = Themes.Brush(palette.Border);

        TitleText.Foreground = Themes.Brush(palette.Title);
        MessageText.Foreground = Themes.Brush(palette.Message);

        // The lead-in number uses the accent so the two phases read differently at a glance.
        CountdownText.Foreground = Themes.Brush(_phase == Phase.Leadin ? palette.Accent : palette.Title);

        EyeOutline.Stroke = Themes.Brush(palette.Accent);
        EyePupil.Fill = Themes.Brush(palette.Accent);

        ProgressTrack.Background = Themes.Brush(palette.Track);
        ProgressFill.Background = Themes.Brush(palette.Accent);

        CardScale.ScaleX = _settings.Scale;
        CardScale.ScaleY = _settings.Scale;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Must happen before the window is ever shown so it never steals the foreground.
        Win32.MakeClickThroughOverlay(new WindowInteropHelper(this).Handle);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        PlaceOnScreen();
        FadeTo(_settings.Opacity, 350);

        if (_phase == Phase.Break)
        {
            StartProgressAnimation();
        }

        _countdown.Start();
    }

    private void OnNoticeTick()
    {
        _countdown.Stop();
        Dismiss();
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        _remaining--;

        if (_remaining > 0)
        {
            // The notice has no visible counter; it just waits its turn out.
            if (_phase != Phase.Notice) CountdownText.Text = _remaining.ToString();
            return;
        }

        if (_phase == Phase.Notice)
        {
            OnNoticeTick();
            return;
        }

        if (_phase == Phase.Leadin)
        {
            StartBreakPhase();
            return;
        }

        _countdown.Stop();
        CountdownText.Text = "0";

        BreakEnded?.Invoke();
        Dismiss();
    }

    private void StartBreakPhase()
    {
        _phase = Phase.Break;
        _remaining = _settings.BreakSeconds;

        MessageText.Text = Strings.Message(_settings);
        CountdownText.Text = _remaining.ToString();
        CountdownText.Foreground = Themes.Brush(Themes.For(_settings).Title);

        ProgressRow.Visibility = Visibility.Visible;
        StartProgressAnimation();
    }

    /// <summary>Fades out and then closes. Safe to call more than once.</summary>
    public void Dismiss()
    {
        if (_closing) return;
        _closing = true;

        _countdown.Stop();
        FadeTo(0, FadeOutMs, onCompleted: Close);

        // Watchdog: if the animation clock never reports completion the window would linger
        // invisible forever and the scheduler would never resume. Force the close.
        var guard = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FadeOutMs + 500) };
        guard.Tick += (_, _) =>
        {
            guard.Stop();
            Close();
        };
        guard.Start();
    }

    private void FadeTo(double target, int milliseconds, Action? onCompleted = null)
    {
        var animation = new DoubleAnimation(target, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        // Completed MUST be wired before BeginAnimation: WPF builds the animation clock there,
        // and handlers attached afterwards are silently never invoked.
        if (onCompleted is not null)
        {
            animation.Completed += (_, _) => onCompleted();
        }

        BeginAnimation(OpacityProperty, animation);
    }

    private void StartProgressAnimation()
    {
        var width = ProgressTrack.ActualWidth;
        if (width <= 0) return;

        ProgressFill.Width = width;
        ProgressFill.BeginAnimation(WidthProperty,
            new DoubleAnimation(width, 0, TimeSpan.FromSeconds(_settings.BreakSeconds)));
    }

    private void PlaceOnScreen()
    {
        var hWnd = new WindowInteropHelper(this).Handle;
        if (!Win32.GetWindowRect(hWnd, out var rect)) return;

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        var area = _screen.WorkingArea; // physical pixels, matching GetWindowRect
        const int margin = 16;

        var (x, y) = _settings.Position.Trim().ToLowerInvariant() switch
        {
            "center"      => (area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2),
            "topright"    => (area.Right - width - margin, area.Top + margin),
            "bottomright" => (area.Right - width - margin, area.Bottom - height - margin),
            "bottomleft"  => (area.Left + margin, area.Bottom - height - margin),
            "topleft"     => (area.Left + margin, area.Top + margin),
            _             => (area.Left + (area.Width - width) / 2, area.Top + margin), // TopCenter
        };

        Win32.PlaceTopMost(hWnd, x, y);
    }
}
