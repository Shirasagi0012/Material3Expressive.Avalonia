using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MaterialShapes;

namespace MaterialShapes.Gallery.Controls;

public sealed class RoundedPolygonDebugOverlay : Control
{
    public static readonly StyledProperty<RoundedPolygon?> ShapeProperty =
        AvaloniaProperty.Register<RoundedPolygonDebugOverlay, RoundedPolygon?>(nameof(Shape));

    static RoundedPolygonDebugOverlay()
    {
        AffectsRender<RoundedPolygonDebugOverlay>(ShapeProperty);
    }

    public RoundedPolygonDebugOverlay()
    {
        IsHitTestVisible = false;
    }

    public RoundedPolygon? Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var shape = Shape;
        if (shape is null || shape.Cubics.Count == 0)
            return;

        var scale = Math.Min(Bounds.Width, Bounds.Height);

        foreach (var cubic in shape.Cubics)
        {
            DrawDebugCubic(context, cubic, scale);
        }
    }

    private static void DrawDebugCubic(DrawingContext context, CubicBezier cubic, double scale)
    {
        var a0 = ScalePoint(cubic.Anchor0, scale);
        var a1 = ScalePoint(cubic.Anchor1, scale);
        var c0 = ScalePoint(cubic.Control0, scale);
        var c1 = ScalePoint(cubic.Control1, scale);

        var redPen = new Pen(Brushes.Red, 2);
        var magentaPen = new Pen(Brushes.Magenta, 2);
        var yellowPen = new Pen(Brushes.Yellow, 1);

        context.DrawEllipse(null, redPen, a0, 6, 6);
        context.DrawEllipse(null, magentaPen, a1, 8, 8);

        context.DrawLine(yellowPen, a0, c0);
        context.DrawEllipse(null, yellowPen, c0, 4, 4);

        context.DrawLine(yellowPen, c1, a1);
        context.DrawEllipse(null, yellowPen, c1, 4, 4);
    }

    private static Point ScalePoint(Point p, double scale)
    {
        return new Point(p.X * scale, p.Y * scale);
    }
}
