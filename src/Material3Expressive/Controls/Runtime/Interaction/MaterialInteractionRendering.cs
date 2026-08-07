using System;

namespace Material3Expressive.Controls;

/// <summary>Pure rendering conversions shared by interaction layers and presenters.</summary>
internal static class MaterialInteractionRendering
{
    /// <summary>
    /// Quantizes an opacity to the same 8-bit value used by Compose's
    /// <c>Color.copy(alpha = ...)</c> path before Skia compositing.
    /// </summary>
    internal static double ComposeOpacity(double opacity)
    {
        if (!double.IsFinite(opacity))
            return 0;

        return Math.Clamp(
                   Math.Round(opacity * byte.MaxValue, MidpointRounding.AwayFromZero),
                   0,
                   byte.MaxValue)
               / byte.MaxValue;
    }
}
