using Avalonia;
using Avalonia.Media;

namespace ComposeAnimation.Runtime;

/// <summary>
/// Default spring visibility thresholds by converter type. Each number is per component: a
/// <see cref="Point"/> threshold of 0.5 DIP means both X and Y must be within 0.5 of the target.
///
/// Lookup is by converter type, not instance — converters are not singletons.
/// Avalonia geometry is DIP, so spatial types use <see cref="Dip"/> rather than Compose Offset's
/// 1 physical pixel. <see cref="ColorConverter"/> is omitted from <see cref="For{T}"/> because a
/// <see cref="Color"/> cannot losslessly encode an Oklab-channel threshold; springs then use
/// <see cref="Default"/> per channel.
/// </summary>
public static class VisibilityThresholds
{
    /// <summary>Per-component cutoff for float, double, and Color (Oklab) springs.</summary>
    public const double Default = Spring.DefaultDisplacementThreshold;

    /// <summary>
    /// Per-component cutoff for Avalonia DIP geometry
    /// (<see cref="Point"/>, <see cref="Vector"/>, <see cref="Size"/>, <see cref="Rect"/>,
    /// <see cref="Thickness"/>, <see cref="CornerRadius"/>).
    /// </summary>
    public const double Dip = 0.5;

    public const float Float = (float)Default;
    public const double Double = Default;
    public const int Int32 = 1;

    public static readonly Point Point = new(Dip, Dip);
    public static readonly Vector Vector = new(Dip, Dip);
    public static readonly Size Size = new(Dip, Dip);
    /// <summary>Each edge has a Dip threshold; zero width/height encode equal opposite edges.</summary>
    public static readonly Rect Rect = new(Dip, Dip, 0, 0);
    public static readonly Thickness Thickness = new(Dip);
    public static readonly CornerRadius CornerRadius = new(Dip);

    /// <summary>
    /// Typed threshold for <see cref="SpringSpec{T}"/>, or <c>default</c> when the converter is
    /// unknown or <see cref="ColorConverter"/> (so the spec does not round-trip through sRGB).
    /// </summary>
    public static T? For<T>(ITwoWayConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        object? boxed = converter switch
        {
            DoubleConverter => Double,
            FloatConverter => Float,
            Int32Converter => Int32,
            PointConverter => Point,
            VectorConverter => Vector,
            SizeConverter => Size,
            RectConverter => Rect,
            ThicknessConverter => Thickness,
            CornerRadiusConverter => CornerRadius,
            _ => null
        };
        return boxed is T value ? value : default;
    }
}
