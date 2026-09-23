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

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled
            ? Opaque(_palette.Title)
            : Blend(_palette.Message, _palette.Background);

        base.OnRenderItemText(e);
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
