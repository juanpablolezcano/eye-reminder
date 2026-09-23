using System.Drawing;
using System.Windows.Forms;

using WpfColor = System.Windows.Media.Color;

namespace EyeReminder;

/// <summary>
/// Paints the tray menu with the card's palette. Without this the right-click menu stays the
/// default light grey strip, which looks wrong next to a dark card.
///
/// Menu colours are forced opaque: the card can be translucent over the desktop, a drop-down
/// cannot, and a half-transparent colour would just render muddy.
/// </summary>
internal sealed class ThemedMenu : ToolStripProfessionalRenderer
{
    private readonly Palette _palette;

    private ThemedMenu(Palette palette) : base(new ThemedColors(palette))
    {
        _palette = palette;
        RoundedEdges = false;
    }

    /// <summary>Applies the palette to a menu, renderer and item colours together.</summary>
    public static void Apply(ContextMenuStrip menu, Palette palette)
    {
        menu.RenderMode = ToolStripRenderMode.Professional;
        menu.Renderer = new ThemedMenu(palette);
        menu.BackColor = Opaque(palette.Background);
        menu.ForeColor = Opaque(palette.Title);
        menu.ShowImageMargin = false;

        foreach (ToolStripItem item in menu.Items)
        {
            item.BackColor = Opaque(palette.Background);
            item.ForeColor = item.Enabled ? Opaque(palette.Title) : Blend(palette.Message, palette.Background);
        }
    }

    /// <summary>
    /// Painted here rather than left to the base renderer, which reaches for the Windows
    /// accent colour and would put a blue bar on an orange theme.
    /// </summary>
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var bounds = new Rectangle(Point.Empty, e.Item.Size);

        using var background = new SolidBrush(Opaque(_palette.Background));
        e.Graphics.FillRectangle(background, bounds);

        if (!e.Item.Selected || !e.Item.Enabled) return;

        using var highlight = new SolidBrush(Opaque(_palette.Accent));
        e.Graphics.FillRectangle(highlight, Rectangle.Inflate(bounds, -2, 0));
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (!e.Item.Enabled)
        {
            e.TextColor = Blend(_palette.Message, _palette.Background);
        }
        else if (e.Item.Selected)
        {
            // The row is filled with the accent, so the label has to read against that.
            e.TextColor = Readable(Opaque(_palette.Accent));
        }
        else
        {
            e.TextColor = Opaque(_palette.Title);
        }

        base.OnRenderItemText(e);
    }

    /// <summary>
    /// Black or white, whichever has more contrast against the given fill. Uses the WCAG
    /// relative luminance so a mid-tone accent flips at the right point.
    /// </summary>
    internal static Color Readable(Color background)
    {
        static double Channel(int value)
        {
            var v = value / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        var luminance = 0.2126 * Channel(background.R)
                      + 0.7152 * Channel(background.G)
                      + 0.0722 * Channel(background.B);

        // Contrast against white is (1.05 / (L + 0.05)); against black, ((L + 0.05) / 0.05).
        return (1.05 / (luminance + 0.05)) >= ((luminance + 0.05) / 0.05)
            ? Color.White
            : Color.Black;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Opaque(_palette.Background));
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Blend(_palette.Border, _palette.Background));

        var bounds = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        e.Graphics.DrawRectangle(pen, bounds);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Blend(_palette.Track, _palette.Background));

        var y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
    }

    /// <summary>Drops the alpha channel, keeping the colour as painted at full opacity.</summary>
    private static Color Opaque(WpfColor color) => Color.FromArgb(255, color.R, color.G, color.B);

    /// <summary>Composites a translucent colour over an opaque one, the way the card would.</summary>
    private static Color Blend(WpfColor foreground, WpfColor background)
    {
        var alpha = foreground.A / 255.0;

        return Color.FromArgb(
            255,
            (int)(foreground.R * alpha + background.R * (1 - alpha)),
            (int)(foreground.G * alpha + background.G * (1 - alpha)),
            (int)(foreground.B * alpha + background.B * (1 - alpha)));
    }

    private sealed class ThemedColors : ProfessionalColorTable
    {
        private readonly Palette _palette;

        public ThemedColors(Palette palette)
        {
            _palette = palette;
            UseSystemColors = false;
        }

        private Color Surface => Opaque(_palette.Background);
        private Color Highlight => Blend(
            System.Windows.Media.Color.FromArgb(0x38, _palette.Accent.R, _palette.Accent.G, _palette.Accent.B),
            _palette.Background);

        public override Color ToolStripDropDownBackground => Surface;
        public override Color MenuBorder => Blend(_palette.Border, _palette.Background);

        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;

        public override Color MenuItemSelected => Highlight;
        public override Color MenuItemSelectedGradientBegin => Highlight;
        public override Color MenuItemSelectedGradientEnd => Highlight;
        public override Color MenuItemBorder => Blend(
            System.Windows.Media.Color.FromArgb(0x80, _palette.Accent.R, _palette.Accent.G, _palette.Accent.B),
            _palette.Background);

        public override Color MenuItemPressedGradientBegin => Highlight;
        public override Color MenuItemPressedGradientMiddle => Highlight;
        public override Color MenuItemPressedGradientEnd => Highlight;

        public override Color SeparatorDark => Blend(_palette.Track, _palette.Background);
        public override Color SeparatorLight => Blend(_palette.Track, _palette.Background);
    }
}
