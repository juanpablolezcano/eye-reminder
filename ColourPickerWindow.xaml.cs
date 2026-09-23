using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace EyeReminder;

/// <summary>
/// Hue, saturation, brightness and opacity, with the hex always in view. Written by hand
/// rather than using the Windows colour dialog, which cannot express the alpha channel that
/// half of a theme's palette depends on.
/// </summary>
public partial class ColourPickerWindow : Window
{
    public Color Chosen { get; private set; }

    private double _hue;          // 0 to 360
    private double _saturation;   // 0 to 1
    private double _brightness;   // 0 to 1
    private byte _alpha = 255;

    private bool _updating;

    public ColourPickerWindow(Color start, string caption)
    {
        InitializeComponent();

        Title = caption;
        LblHue.Text = Ui.T("lbl.hue");
        LblAlpha.Text = Ui.T("lbl.opacity");
        CancelButton.Content = Ui.T("btn.cancel");
        OkButton.Content = Ui.T("btn.ok");

        Icon = EyeIcon.ForWindow(System.Drawing.Color.FromArgb(start.A, start.R, start.G, start.B));

        Chosen = start;
        _alpha = start.A;
        (_hue, _saturation, _brightness) = ToHsv(start);

        Field.MouseLeftButtonDown += (_, e) => Track(Field, e, PickSaturationBrightness);
        Field.MouseMove += (_, e) => Drag(Field, e, PickSaturationBrightness);
        Field.MouseLeftButtonUp += (_, _) => Field.ReleaseMouseCapture();

        HueBar.MouseLeftButtonDown += (_, e) => Track(HueBar, e, PickHue);
        HueBar.MouseMove += (_, e) => Drag(HueBar, e, PickHue);
        HueBar.MouseLeftButtonUp += (_, _) => HueBar.ReleaseMouseCapture();

        AlphaBar.MouseLeftButtonDown += (_, e) => Track(AlphaBar, e, PickAlpha);
        AlphaBar.MouseMove += (_, e) => Drag(AlphaBar, e, PickAlpha);
        AlphaBar.MouseLeftButtonUp += (_, _) => AlphaBar.ReleaseMouseCapture();

        HexInput.TextChanged += (_, _) => ReadHex();

        CancelButton.Click += (_, _) => Close();
        OkButton.Click += (_, _) => { DialogResult = true; Close(); };

        Refresh();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Win32.UseDarkTitleBar(new WindowInteropHelper(this).Handle);
    }

    // ---- input ------------------------------------------------------------

    private static void Track(UIElement surface, MouseButtonEventArgs e, Action<Point> apply)
    {
        surface.CaptureMouse();
        apply(e.GetPosition(surface));
    }

    private static void Drag(UIElement surface, MouseEventArgs e, Action<Point> apply)
    {
        if (e.LeftButton == MouseButtonState.Pressed && surface.IsMouseCaptured)
        {
            apply(e.GetPosition(surface));
        }
    }

    private void PickSaturationBrightness(Point at)
    {
        _saturation = Math.Clamp(at.X / Field.Width, 0, 1);
        _brightness = Math.Clamp(1 - at.Y / Field.Height, 0, 1);
        Refresh();
    }

    private void PickHue(Point at)
    {
        _hue = Math.Clamp(at.X / HueBar.Width, 0, 1) * 360;
        Refresh();
    }

    private void PickAlpha(Point at)
    {
        _alpha = (byte)Math.Round(Math.Clamp(at.X / AlphaBar.Width, 0, 1) * 255);
        Refresh();
    }

    private void ReadHex()
    {
        if (_updating) return;

        var text = HexInput.Text.Trim();
        if (!text.StartsWith('#')) text = "#" + text;

        Color parsed;
        try
        {
            parsed = (Color)ColorConverter.ConvertFromString(text)!;
        }
        catch (FormatException)
        {
            HexInput.Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x94, 0xB4));
            return;
        }
        catch (InvalidOperationException)
        {
            HexInput.Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x94, 0xB4));
            return;
        }

        _alpha = parsed.A;
        (_hue, _saturation, _brightness) = ToHsv(parsed);
        Refresh(rewriteHex: false);
    }

    // ---- painting ---------------------------------------------------------

    private void Refresh(bool rewriteHex = true)
    {
        var colour = FromHsv(_hue, _saturation, _brightness, _alpha);
        Chosen = colour;

        _updating = true;

        FieldHue.Fill = new SolidColorBrush(FromHsv(_hue, 1, 1, 255));

        Canvas.SetLeft(FieldThumb, _saturation * Field.Width - FieldThumb.Width / 2);
        Canvas.SetTop(FieldThumb, (1 - _brightness) * Field.Height - FieldThumb.Height / 2);
        FieldThumb.Fill = new SolidColorBrush(FromHsv(_hue, _saturation, _brightness, 255));

        Canvas.SetLeft(HueThumb, _hue / 360 * HueBar.Width - HueThumb.Width / 2);

        AlphaFill.Fill = new LinearGradientBrush(
            Color.FromArgb(0, colour.R, colour.G, colour.B),
            Color.FromArgb(255, colour.R, colour.G, colour.B),
            0);

        Canvas.SetLeft(AlphaThumb, _alpha / 255.0 * AlphaBar.Width - AlphaThumb.Width / 2);

        Swatch.Background = new SolidColorBrush(colour);

        if (rewriteHex)
        {
            HexInput.Text = $"#{colour.A:X2}{colour.R:X2}{colour.G:X2}{colour.B:X2}";
        }

        HexInput.Foreground = new SolidColorBrush(Colors.White);
        _updating = false;
    }

    // ---- conversion -------------------------------------------------------

    private static (double Hue, double Saturation, double Brightness) ToHsv(Color colour)
    {
        double r = colour.R / 255.0, g = colour.G / 255.0, b = colour.B / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        double hue = 0;
        if (delta > 0)
        {
            if (Math.Abs(max - r) < double.Epsilon) hue = 60 * (((g - b) / delta) % 6);
            else if (Math.Abs(max - g) < double.Epsilon) hue = 60 * ((b - r) / delta + 2);
            else hue = 60 * ((r - g) / delta + 4);
        }

        if (hue < 0) hue += 360;

        return (hue, max <= 0 ? 0 : delta / max, max);
    }

    private static Color FromHsv(double hue, double saturation, double brightness, byte alpha)
    {
        var chroma = brightness * saturation;
        var second = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
        var offset = brightness - chroma;

        var (r, g, b) = hue switch
        {
            < 60 => (chroma, second, 0.0),
            < 120 => (second, chroma, 0.0),
            < 180 => (0.0, chroma, second),
            < 240 => (0.0, second, chroma),
            < 300 => (second, 0.0, chroma),
            _ => (chroma, 0.0, second)
        };

        return Color.FromArgb(
            alpha,
            (byte)Math.Round((r + offset) * 255),
            (byte)Math.Round((g + offset) * 255),
            (byte)Math.Round((b + offset) * 255));
    }

    /// <summary>Opens the picker over a window and returns the chosen colour, or null.</summary>
    public static Color? Pick(Window owner, Color start, string caption)
    {
        var picker = new ColourPickerWindow(start, caption) { Owner = owner };
        return picker.ShowDialog() == true ? picker.Chosen : null;
    }
}
