namespace Material3Expressive.Foundation;

/// <summary>Numeric helpers shared by Material components.</summary>
internal static class MathUtils
{
    /// <summary>Rounds to the nearest integer; midpoints go toward positive infinity.</summary>
    /// <remarks>The input must be finite, and the rounded result must fit in <see cref="int"/>.</remarks>
    internal static int RoundToInt(double value) => (int)Math.Floor(value + 0.5);

    /// <summary>Rounds a single-precision value; midpoints go toward positive infinity.</summary>
    /// <remarks>The input must be finite, and the rounded result must fit in <see cref="int"/>.</remarks>
    internal static int RoundToInt(float value) => (int)MathF.Floor(value + 0.5f);

    /// <summary>Converts DIP to px using double-precision multiplication and nearest-pixel rounding.</summary>
    /// <remarks>The scale must be finite and positive; the product must be finite and round to an <see cref="int"/>.</remarks>
    internal static int DipToPx(double dip, double layoutScale) => RoundToInt(dip * layoutScale);

    /// <summary>Converts DIP to px using single-precision multiplication and nearest-pixel rounding.</summary>
    /// <remarks>The scale must be finite and positive; the product must be finite and round to an <see cref="int"/>.</remarks>
    internal static int DipToPx(float dip, float layoutScale) => RoundToInt(dip * layoutScale);

    /// <summary>Converts px to DIP.</summary>
    /// <remarks><paramref name="layoutScale"/> must be finite and positive.</remarks>
    internal static double PxToDip(double px, double layoutScale) => px / layoutScale;

    /// <summary>Linearly interpolates values without clamping the fraction.</summary>
    internal static double Lerp(double start, double end, double fraction) => start + (end - start) * fraction;

    /// <summary>Linearly interpolates single-precision values without clamping the fraction.</summary>
    internal static float Lerp(float start, float end, float fraction) => start + (end - start) * fraction;

    /// <summary>Interpolates integer endpoints and rounds the offset; midpoints go toward positive infinity.</summary>
    /// <remarks>The fraction is not clamped; the offset must be finite, and its rounded value and the result must fit in <see cref="int"/>.</remarks>
    internal static int LerpRoundToInt(int start, int end, float fraction) =>
        start + RoundToInt(((double)end - start) * fraction);
}
