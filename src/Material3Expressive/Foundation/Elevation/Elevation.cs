using Avalonia.Media;

namespace Material3Expressive.Foundation;

/// <summary>
/// Turns an elevation token into the shadow that expresses it.
/// </summary>
/// <remarks>
/// Compose Material surfaces use <c>graphicsLayer(shadowElevation, shape, clip = false)</c>.
/// On Desktop that delegates to Skiko's render node, which draws separate ambient and spot
/// shadows. Its light configuration uses the platform ambient (0.039) and spot (0.19) opacity
/// constants. Avalonia's Skia backend instead exposes independently blurred
/// <see cref="BoxShadow"/> layers. The calibration below maps the Material elevation token levels
/// to those two Avalonia layers at the canonical 2x parity density. It was fitted against the
/// locked CMP renderer at Levels 1, 3, 6, 8, and 12, rather than treating elevation as one opaque
/// 30% shadow.
/// <para>
/// Elevation in Material 3 is also mostly vestigial: Expressive expresses hierarchy with surface
/// container colours, and only the elevated button, FAB and menus still raise a shadow.
/// </para>
/// </remarks>
public static class Elevation
{
    private static readonly ShadowPair[] s_calibration =
    [
        new(0, default, default),
        new(1, new ShadowLayer(0.055, 0.50, 3.3), new ShadowLayer(0.13, 0.25, 0.7)),
        new(3, new ShadowLayer(0.155, 0.50, 8.0), new ShadowLayer(0.065, 0.25, 0.7)),
        new(6, new ShadowLayer(0.12, 0.50, 20.0), new ShadowLayer(0.10, 0.50, 4.0)),
        new(8, new ShadowLayer(0.15, 0.50, 26.7), new ShadowLayer(0.01, 0.50, 0.7)),
        new(12, new ShadowLayer(0.16, 0.50, 40.0), new ShadowLayer(0.055, 0.50, 5.3))
    ];

    /// <summary>The shadow for an elevation, or none at level 0.</summary>
    public static BoxShadows ToBoxShadows(double elevation, Color shadowColor)
    {
        if (elevation <= 0)
            return default;

        var pair = Resolve(elevation);
        // RenderNode composites the broad ambient shadow before the tighter spot shadow.
        var first = CreateShadow(pair.Ambient, shadowColor);
        var second = CreateShadow(pair.Spot, shadowColor);
        if (first is null)
            return second is null ? default : new BoxShadows(second.Value);
        return second is null
            ? new BoxShadows(first.Value)
            : new BoxShadows(first.Value, [second.Value]);
    }

    private static ShadowPair Resolve(double elevation)
    {
        var upperIndex = Array.FindIndex(s_calibration, item => item.Elevation >= elevation);
        if (upperIndex < 0)
            return s_calibration[^1];
        if (upperIndex == 0)
        {
            var scale = elevation / s_calibration[1].Elevation;
            return s_calibration[1].Scale(scale);
        }

        var upper = s_calibration[upperIndex];
        var lower = s_calibration[upperIndex - 1];
        var progress = (elevation - lower.Elevation) / (upper.Elevation - lower.Elevation);
        return ShadowPair.Lerp(lower, upper, progress);
    }

    private static BoxShadow? CreateShadow(ShadowLayer layer, Color shadowColor)
    {
        if (layer.Opacity <= 0)
            return null;

        return new BoxShadow
        {
            OffsetX = 0,
            OffsetY = layer.OffsetY,
            Blur = layer.Blur,
            Spread = 0,
            Color = new Color(
                (byte)Math.Round(shadowColor.A * layer.Opacity),
                shadowColor.R,
                shadowColor.G,
                shadowColor.B)
        };
    }

    private readonly record struct ShadowPair(double Elevation, ShadowLayer Ambient, ShadowLayer Spot)
    {
        public ShadowPair Scale(double scale) => new(
            Elevation * scale,
            Ambient.Scale(scale),
            Spot.Scale(scale));

        public static ShadowPair Lerp(ShadowPair lower, ShadowPair upper, double progress) => new(
            lower.Elevation + ((upper.Elevation - lower.Elevation) * progress),
            ShadowLayer.Lerp(lower.Ambient, upper.Ambient, progress),
            ShadowLayer.Lerp(lower.Spot, upper.Spot, progress));
    }

    private readonly record struct ShadowLayer(double Opacity, double OffsetY, double Blur)
    {
        public ShadowLayer Scale(double scale) => new(Opacity * scale, OffsetY * scale, Blur * scale);

        public static ShadowLayer Lerp(ShadowLayer lower, ShadowLayer upper, double progress) => new(
            lower.Opacity + ((upper.Opacity - lower.Opacity) * progress),
            lower.OffsetY + ((upper.OffsetY - lower.OffsetY) * progress),
            lower.Blur + ((upper.Blur - lower.Blur) * progress));
    }
}
