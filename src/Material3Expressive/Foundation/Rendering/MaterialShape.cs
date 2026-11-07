// CornerBasedShape.kt / CutCornerShape.kt, pinned AndroidX baseline; Apache-2.0.
using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace Material3Expressive.Foundation.Rendering;

/// <summary>A size-dependent, closed container outline. Changes to Avalonia properties invalidate its consumers.</summary>
public abstract class MaterialShape : AvaloniaObject
{
    /// <summary>Creates a new closed path in physical left-to-right DIP coordinates for the supplied layout direction.</summary>
    /// <remarks>Density is supplied for algorithms evaluated in physical pixels. Return enclosed, non-overlapping contours.</remarks>
    public abstract PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density);
}

/// <summary>A rectangle with four logical cut corners. Absolute lengths are DIP; percentages use the smaller side.</summary>
public sealed class CutCornerShape : MaterialShape
{
    public static readonly StyledProperty<RelativeScalar> TopStartProperty =
        AvaloniaProperty.Register<CutCornerShape, RelativeScalar>(nameof(TopStart), new(0, RelativeUnit.Absolute), validate: ValidCorner);
    public static readonly StyledProperty<RelativeScalar> TopEndProperty =
        AvaloniaProperty.Register<CutCornerShape, RelativeScalar>(nameof(TopEnd), new(0, RelativeUnit.Absolute), validate: ValidCorner);
    public static readonly StyledProperty<RelativeScalar> BottomEndProperty =
        AvaloniaProperty.Register<CutCornerShape, RelativeScalar>(nameof(BottomEnd), new(0, RelativeUnit.Absolute), validate: ValidCorner);
    public static readonly StyledProperty<RelativeScalar> BottomStartProperty =
        AvaloniaProperty.Register<CutCornerShape, RelativeScalar>(nameof(BottomStart), new(0, RelativeUnit.Absolute), validate: ValidCorner);
    private static bool ValidCorner(RelativeScalar value) => double.IsFinite(value.Scalar) && value.Scalar >= 0
        && (value.Unit != RelativeUnit.Relative || value.Scalar <= 1);

    public RelativeScalar TopStart { get => GetValue(TopStartProperty); set => SetValue(TopStartProperty, value); }
    public RelativeScalar TopEnd { get => GetValue(TopEndProperty); set => SetValue(TopEndProperty, value); }
    public RelativeScalar BottomEnd { get => GetValue(BottomEndProperty); set => SetValue(BottomEndProperty, value); }
    public RelativeScalar BottomStart { get => GetValue(BottomStartProperty); set => SetValue(BottomStartProperty, value); }

    public CutCornerShape() { }
    public CutCornerShape(double size) : this(new RelativeScalar(size, RelativeUnit.Absolute)) { }
    public CutCornerShape(RelativeScalar size) => TopStart = TopEnd = BottomEnd = BottomStart = size;
    internal bool IsRectangle => TopStart.Scalar + TopEnd.Scalar + BottomEnd.Scalar + BottomStart.Scalar == 0;

    public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
    {
        var scale = (float)density;
        var width = (float)size.Width * scale;
        var height = (float)size.Height * scale;
        var minimum = MathF.Min(width, height);
        float Pixels(RelativeScalar value) => value.Unit == RelativeUnit.Relative
            ? minimum * (((float)value.Scalar * 100f) / 100f) : (float)value.Scalar * scale;
        var start = Pixels(TopStart); var end = Pixels(TopEnd);
        var bottomEnd = Pixels(BottomEnd); var bottomStart = Pixels(BottomStart);
        return CreateOutline(size, layoutDirection, density, new(start / scale, end / scale, bottomEnd / scale, bottomStart / scale));
    }

    internal static PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density, CornerRadius corners)
    {
        var scale = (float)density;
        var width = (float)size.Width * scale; var height = (float)size.Height * scale;
        var minimum = MathF.Min(width, height);
        var start = (float)corners.TopLeft * scale; var end = (float)corners.TopRight * scale;
        var bottomEnd = (float)corners.BottomRight * scale; var bottomStart = (float)corners.BottomLeft * scale;
        if (start + bottomStart > minimum)
        {
            var fit = minimum / (start + bottomStart);
            start *= fit; bottomStart *= fit;
        }
        if (end + bottomEnd > minimum)
        {
            var fit = minimum / (end + bottomEnd);
            end *= fit; bottomEnd *= fit;
        }
        if (layoutDirection == FlowDirection.RightToLeft)
        {
            (start, end) = (end, start);
            (bottomStart, bottomEnd) = (bottomEnd, bottomStart);
        }
        Point Dip(float x, float y) => new(x / scale, y / scale);
        return new PathGeometry
        {
            FillRule = FillRule.NonZero,
            Figures = new PathFigures
            {
                new PathFigure
                {
                    StartPoint = Dip(0, start), IsClosed = true, IsFilled = true,
                    Segments = new PathSegments
                    {
                        new LineSegment { Point = Dip(start, 0) },
                        new LineSegment { Point = Dip(width - end, 0) },
                        new LineSegment { Point = Dip(width, end) },
                        new LineSegment { Point = Dip(width, height - bottomEnd) },
                        new LineSegment { Point = Dip(width - bottomEnd, height) },
                        new LineSegment { Point = Dip(bottomStart, height) },
                        new LineSegment { Point = Dip(0, height - bottomStart) },
                    }
                }
            }
        };
    }
}

internal static class ShapeGeometry
{
    // PathGeometry's invariant SVG representation preserves curves and fill rules without
    // depending on Avalonia's private backend geometry. Same bridge as TooltipCaret.
    internal static SKPath ToSkiaPath(PathGeometry geometry, float density = 1)
    {
        var data = geometry.ToString();
        if (data.StartsWith("F0 ", StringComparison.Ordinal) || data.StartsWith("F1 ", StringComparison.Ordinal)) data = data[3..];
        using var source = string.IsNullOrWhiteSpace(data) ? new SKPath() : SKPath.ParseSvgPathData(data)
            ?? throw new InvalidOperationException("Unable to read the material shape path.");
        source.FillType = geometry.FillRule == FillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
        var matrix = (geometry.Transform?.Value ?? Matrix.Identity) * Matrix.CreateScale(density, density);
        using var builder = new SKPathBuilder();
        builder.AddPath(source, new SKMatrix((float)matrix.M11, (float)matrix.M21, (float)matrix.M31,
            (float)matrix.M12, (float)matrix.M22, (float)matrix.M32, 0, 0, 1));
        var path = builder.Detach();
        path.FillType = source.FillType;
        return path;
    }
}
