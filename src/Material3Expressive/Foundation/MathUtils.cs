namespace Material3Expressive.Foundation;

/// <summary>
/// Numeric helpers shared by Material components.
/// </summary>
internal static class MathUtils
{
    /// <summary>
    /// Rounds to the nearest integer, with midpoint values rounded toward positive infinity.
    /// This matches the pixel-rounding rule used by Compose's <c>roundToInt</c>.
    /// </summary>
    /// <remarks>The value must be finite, and its rounded result must fit in <see cref="int"/>.</remarks>
    internal static int RoundToInt(double value) => (int)Math.Floor(value + 0.5);

    /// <summary>
    /// Rounds to the nearest integer, with midpoint values rounded toward positive infinity.
    /// The calculation remains single precision.
    /// </summary>
    /// <remarks>The value must be finite, and its rounded result must fit in <see cref="int"/>.</remarks>
    internal static int RoundToInt(float value) => (int)MathF.Floor(value + 0.5f);

    /// <summary>Converts device-independent pixels to physical pixels using nearest-pixel rounding.</summary>
    /// <remarks>The product must be finite and its rounded result must fit in <see cref="int"/>. The scale must be finite and positive.</remarks>
    internal static int DipToPx(double dip, double layoutScale) => RoundToInt(dip * layoutScale);

    /// <summary>Converts device-independent pixels to physical pixels using single-precision arithmetic.</summary>
    /// <remarks>The product must be finite and its rounded result must fit in <see cref="int"/>. The scale must be finite and positive.</remarks>
    internal static int DipToPx(float dip, float layoutScale) => RoundToInt(dip * layoutScale);

    /// <summary>Converts physical pixels to device-independent pixels.</summary>
    /// <remarks><paramref name="layoutScale"/> must be finite and positive.</remarks>
    internal static double PxToDip(double px, double layoutScale) => px / layoutScale;

    /// <summary>Linearly interpolates between two values without clamping the fraction.</summary>
    internal static double Lerp(double start, double end, double fraction) => start + (end - start) * fraction;

    /// <summary>Linearly interpolates between two single-precision values without clamping the fraction.</summary>
    internal static float Lerp(float start, float end, float fraction) => start + (end - start) * fraction;

    /// <summary>
    /// Linearly interpolates between integer values and rounds the interpolated offset to the nearest integer,
    /// with midpoint values rounded toward positive infinity.
    /// </summary>
    /// <remarks>The fraction is not clamped to the range [0, 1].</remarks>
    internal static int LerpRoundToInt(int start, int end, float fraction) =>
        start + RoundToInt(((double)end - start) * fraction);
}
