using Avalonia;
using Avalonia.Media;

namespace ComposeAnimation.Runtime;

/// <summary>
/// Typed factories for <see cref="Animatable{T}"/> with the matching converter and visibility
/// threshold. They bind nothing: runs name their scope (<c>AnimateToAsync(scope, ...)</c>).
/// </summary>
public static class Animatable
{
    public static Animatable<double> Create(double initialValue, string? label = null)
        => new(new DoubleConverter(), initialValue, VisibilityThresholds.Double, label);

    public static Animatable<float> Create(float initialValue, string? label = null)
        => new(new FloatConverter(), initialValue, VisibilityThresholds.Float, label);

    public static Animatable<int> Create(int initialValue, string? label = null)
        => new(new Int32Converter(), initialValue, VisibilityThresholds.Int32, label);

    public static Animatable<Point> Create(Point initialValue, string? label = null)
        => new(new PointConverter(), initialValue, VisibilityThresholds.Point, label);

    public static Animatable<Vector> Create(Vector initialValue, string? label = null)
        => new(new VectorConverter(), initialValue, VisibilityThresholds.Vector, label);

    public static Animatable<Size> Create(Size initialValue, string? label = null)
        => new(new SizeConverter(), initialValue, VisibilityThresholds.Size, label);

    public static Animatable<Rect> Create(Rect initialValue, string? label = null)
        => new(new RectConverter(), initialValue, VisibilityThresholds.Rect, label);

    public static Animatable<Thickness> Create(Thickness initialValue, string? label = null)
        => new(new ThicknessConverter(), initialValue, VisibilityThresholds.Thickness, label);

    public static Animatable<CornerRadius> Create(CornerRadius initialValue, string? label = null)
        => new(new CornerRadiusConverter(), initialValue, VisibilityThresholds.CornerRadius, label);

    public static Animatable<Color> Create(Color initialValue, string? label = null)
        => new(new ColorConverter(), initialValue, default, label);
}
