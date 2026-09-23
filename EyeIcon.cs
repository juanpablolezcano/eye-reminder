using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace EyeReminder;

/// <summary>
/// The eye mark, drawn at runtime so the app ships without binary assets. The tray and the
/// settings window both come from here, which is what keeps them identical.
/// </summary>
internal static class EyeIcon
{
    private static readonly Color DefaultInk = Color.FromArgb(240, 127, 212, 196);

    /// <summary>The drawing is authored on a 32x32 grid and scaled to whatever is asked for.</summary>
    private const float DesignSize = 32f;

    /// <summary>The tray mark, in the theme accent so it matches the card.</summary>
    public static Icon ForTray(Color? ink = null, int size = 32)
    {
        using var bitmap = Draw(size, ink ?? DefaultInk);

        // Copy the handle-backed icon so the GDI handle can be released straight away.
        var handle = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    public static BitmapSource ForWindow(Color? ink = null, int size = 64)
    {
        using var bitmap = Draw(size, ink ?? DefaultInk);
        using var stream = new MemoryStream();

        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad; // so the stream can be disposed
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();

        return image;
    }

    private static Bitmap Draw(int size, Color ink)
    {
        var bitmap = new Bitmap(size, size);

        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        g.ScaleTransform(size / DesignSize, size / DesignSize);

        using var pen = new Pen(ink, 2.6f);
        using var brush = new SolidBrush(ink);
        using var outline = new GraphicsPath();

        outline.AddBezier(2, 16, 10, 5, 22, 5, 30, 16);
        outline.AddBezier(30, 16, 22, 27, 10, 27, 2, 16);

        g.DrawPath(pen, outline);
        g.FillEllipse(brush, 11, 11, 10, 10);

        return bitmap;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
