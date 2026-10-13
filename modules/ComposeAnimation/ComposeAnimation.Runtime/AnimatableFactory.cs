using Avalonia;
using Avalonia.Media;

namespace ComposeAnimation.Runtime;

/// <summary>
/// Typed factories for <see cref="Animatable{T}"/> with the matching converter and visibility
/// threshold. They bind nothing: runs name their scope (<c>AnimateToAsync(scope, ...)</c>).
/// </summary>
public static class Animatable
{
    // Converters are stateless, so each factory shares one cached instance.
    private static readonly DoubleConverter SharedDouble = new();
    private static readonly FloatConverter SharedFloat = new();
    private static readonly Int32Converter SharedInt32 = new();
    private static readonly PointConverter SharedPoint = new();
    private static readonly VectorConverter SharedVector = new();
    private static readonly SizeConverter SharedSize = new();
    private static readonly RectConverter SharedRect = new();
    private static readonly ThicknessConverter SharedThickness = new();
    private static readonly CornerRadiusConverter SharedCornerRadius = new();
    private static readonly ColorConverter SharedColor = new();

    /// <summary>Creates an <see cref="Animatable{T}"/> of <see cref="double"/> starting at <paramref name="initialValue"/>, using a shared <see cref="DoubleConverter"/>.</summary>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="label">Optional label for diagnostics.</param>
    public static Animatable<double> Create(double initialValue, string? label = null)
        => new(SharedDouble, initialValue, VisibilityThresholds.Double, label);

    /// <summary>Creates an <see cref="Animatable{T}"/> of <see cref="float"/> starting at <paramref name="initialValue"/>, using a shared <see cref="FloatConverter"/>.</summary>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="label">Optional label for diagnostics.</param>
    public static Animatable<float> Create(float initialValue, string? label = null)
        => new(SharedFloat, initialValue, VisibilityThresholds.Float, label);

    /// <summary>Creates an <see cref="Animatable{T}"/> of <see cref="int"/> starting at <paramref name="initialValue"/>, using a shared <see cref="Int32Converter"/>.</summary>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="label">Optional label for diagnostics.</param>
    public static Animatable<int> Create(int initialValue, string? label = null)
        => new(SharedInt32, initialValue, VisibilityThresholds.Int32, label);

    /// <summary>Creates an <see cref="Animatable{T}"/> of <see cref="Point"/> starting at <paramref name="initialValue"/>, using a shared <see cref="PointConverter"/>.</summary>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="label">Optional label for diagnostics.</param>
    public static Animatable<Point> Create(Point initialValue, string? label = null)
        => new(SharedPoint, initialValue, VisibilityThresholds.Point, label);

    /// <summary>Creates an <see cref="Animatable{T}"/> of <see cref="Vector"/> starting at <paramref name="initialValue"/>, using a shared <see cref="VectorConverter"/>.</summary>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="label">Optional label for diagnostics.</param>
    public static Animatable<Vector> Create(Vector initialValue, string? label = null)
        => new(SharedVector, initialValue, VisibilityThresholds.Vector, label);

    /// <summary>Creates an <see cref="Animatable{T}"/> of <see cref="Size"/> starting at <paramref name="initialValue"/>, using a shared <see cref="SizeConverter"/>.</summary>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="label">Optional label for diagnostics.</param>
    public static Animatable<Size> Create(Size initialValue, string? label = null)
        => new(SharedSize, initialValue, VisibilityThresholds.Size, label);

    /// <summary>Creates an <see cref="Animatable{T}"/> of <see cref="Rect"/> starting at <paramref name="initialValue"/>, using a shared <see cref="RectConverter"/>.</summary>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="label">Optional label for diagnostics.</param>
    public static Animatable<Rect> Create(Rect initialValue, string? label = null)
        => new(SharedRect, initialValue, VisibilityThresholds.Rect, label);

    /// <summary>Creates an <see cref="Animatable{T}"/> of <see cref="Thickness"/> starting at <paramref name="initialValue"/>, using a shared <see cref="ThicknessConverter"/>.</summary>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="label">Optional label for diagnostics.</param>
    public static Animatable<Thickness> Create(Thickness initialValue, string? label = null)
        => new(SharedThickness, initialValue, VisibilityThresholds.Thickness, label);

    /// <summary>Creates an <see cref="Animatable{T}"/> of <see cref="CornerRadius"/> starting at <paramref name="initialValue"/>, using a shared <see cref="CornerRadiusConverter"/>.</summary>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="label">Optional label for diagnostics.</param>
    public static Animatable<CornerRadius> Create(CornerRadius initialValue, string? label = null)
        => new(SharedCornerRadius, initialValue, VisibilityThresholds.CornerRadius, label);

    /// <summary>Creates an <see cref="Animatable{T}"/> of <see cref="Color"/> starting at <paramref name="initialValue"/>, using a shared <see cref="ColorConverter"/>.</summary>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="label">Optional label for diagnostics.</param>
    public static Animatable<Color> Create(Color initialValue, string? label = null)
        => new(SharedColor, initialValue, default, label);
}
