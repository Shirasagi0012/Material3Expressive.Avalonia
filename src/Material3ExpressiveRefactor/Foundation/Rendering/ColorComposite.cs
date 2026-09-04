using Avalonia.Media;

namespace Material3ExpressiveRefactor.Foundation.Rendering;

/// <summary>
/// Source-over alpha compositing (upstream: <c>Color.compositeOver</c>). The tonal elevation
/// tint uses it to overlay <c>md.sys.color.surface-tint</c> at a computed alpha onto the
/// surface color.
/// </summary>
internal static class ColorComposite
{
    public static Color Over(Color foreground, double foregroundAlpha, Color background)
    {
        var fgAlpha = Math.Clamp((foreground.A / 255d) * foregroundAlpha, 0, 1);
        var bgAlpha = background.A / 255d;
        var outAlpha = fgAlpha + (bgAlpha * (1 - fgAlpha));
        if (outAlpha <= 0)
            return Colors.Transparent;

        byte Channel(byte foregroundChannel, byte backgroundChannel) =>
            (byte)Math.Round(
                (((foregroundChannel / 255d) * fgAlpha) + ((backgroundChannel / 255d) * bgAlpha * (1 - fgAlpha))) / outAlpha * 255,
                MidpointRounding.AwayFromZero);

        return Color.FromArgb(
            (byte)Math.Round(outAlpha * 255, MidpointRounding.AwayFromZero),
            Channel(foreground.R, background.R),
            Channel(foreground.G, background.G),
            Channel(foreground.B, background.B));
    }
}
