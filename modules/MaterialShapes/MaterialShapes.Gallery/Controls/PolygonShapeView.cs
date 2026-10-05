using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MaterialShapes;

namespace MaterialShapes.Gallery.Controls;

public sealed class PolygonShapeView : Control
{
    public static readonly StyledProperty<RoundedPolygon?> ShapeProperty =
        AvaloniaProperty.Register<PolygonShapeView, RoundedPolygon?>(nameof(Shape));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<PolygonShapeView, IBrush?>(nameof(Fill));

    static PolygonShapeView()
    {
        AffectsRender<PolygonShapeView>(ShapeProperty, FillProperty);
    }

    public RoundedPolygon? Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var shape = Shape;
        if (shape is null || shape.Cubics.Count == 0)
            return;

        var scale = Math.Min(Bounds.Width, Bounds.Height);
        var geometry = new StreamGeometry();
        using (var gctx = geometry.Open())
        {
            var first = shape.Cubics[0];
            gctx.BeginFigure(ScalePoint(first.Anchor0, scale), true);
            foreach (var cubic in shape.Cubics)
            {
                gctx.CubicBezierTo(
                    ScalePoint(cubic.Control0, scale),
                    ScalePoint(cubic.Control1, scale),
                    ScalePoint(cubic.Anchor1, scale));
            }
            gctx.EndFigure(true);
        }

        context.DrawGeometry(Fill, null, geometry);
    }

    private static Point ScalePoint(Point p, double scale)
    {
        return new Point(p.X * scale, p.Y * scale);
    }
}
