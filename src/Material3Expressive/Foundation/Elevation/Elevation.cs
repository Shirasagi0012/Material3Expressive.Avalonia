using Avalonia.Media;

namespace Material3Expressive.Foundation;

/// <summary>
/// Turns an elevation token into the shadow that expresses it.
/// </summary>
/// <remarks>
/// A documented approximation, and the largest deliberate deviation in the library. Android
/// composites two shadows from a real light source — a tight key shadow and a wide ambient one —
/// against the platform's shadow renderer. Avalonia has one offset-blur-spread box shadow per
/// layer, so the two are folded into a single shadow tuned to read the same at the six Material
/// elevation levels rather than derived from the same optics.
/// <para>
/// Elevation in Material 3 is also mostly vestigial: Expressive expresses hierarchy with surface
/// container colours, and only the elevated button, FAB and menus still raise a shadow.
/// </para>
/// </remarks>
public static class Elevation
{
    /// <summary>The shadow for an elevation, or none at level 0.</summary>
    public static BoxShadows ToBoxShadows(double elevation, Color shadowColor)
    {
        if (elevation <= 0)
            return default;

        return new BoxShadows(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = elevation * 0.5,
            Blur = elevation * 2.0,
            Spread = 0,
            Color = new Color((byte)(0xFF * 0.3), shadowColor.R, shadowColor.G, shadowColor.B)
        });
    }
}
