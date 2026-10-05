using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MaterialShapes;

namespace MaterialShapes.Gallery.Controls;

public sealed class MorphDebugOverlay : Control
{
    public static readonly StyledProperty<Morph?> MorphProperty =
        AvaloniaProperty.Register<MorphDebugOverlay, Morph?>(nameof(Morph));

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<MorphDebugOverlay, double>(nameof(Progress), 0d);

    static MorphDebugOverlay()
    {
        AffectsRender<MorphDebugOverlay>(MorphProperty, ProgressProperty);
    }

    public MorphDebugOverlay()
    {
        IsHitTestVisible = false;
    }

    public Morph? Morph
    {
        get => GetValue(MorphProperty);
        set => SetValue(MorphProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var morph = Morph;
        if (morph is null)
            return;

        var scale = Math.Min(Bounds.Width, Bounds.Height);

        var translate = Math.Abs(Bounds.Width - Bounds.Height) / 2;
        var translateX = (Bounds.Width > Bounds.Height) ? translate : 0;
        var translateY = (Bounds.Height > Bounds.Width) ? translate : 0;

        foreach (var cubic in morph.EnumerateCubics(Progress))
        {
            DrawDebugCubic(context, cubic, scale, translateX, translateY);
        }
    }

    private static void DrawDebugCubic(
        DrawingContext context,
        CubicBezier cubic,
        double scale,
        double translateX,
        double translateY
    )
    {
        var a0 = ScaleTranslatePoint(cubic.Anchor0, scale, translateX, translateY);
        var a1 = ScaleTranslatePoint(cubic.Anchor1, scale, translateX, translateY);
        var c0 = ScaleTranslatePoint(cubic.Control0, scale, translateX, translateY);
        var c1 = ScaleTranslatePoint(cubic.Control1, scale, translateX, translateY);

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

    private static Point ScaleTranslatePoint(Point p, double scale, double translateX, double translateY)
    {
        return new Point(p.X * scale + translateX, p.Y * scale + translateY);
    }
}
