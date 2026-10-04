using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Material3Expressive.Components;

internal sealed class LoadingIndicatorDrawOperation(Size size, float scale, SKPath path, Color color,
    float rotation, float x, float y, float side) : ICustomDrawOperation
{
    public Rect Bounds { get; } = new(size);
    public bool HitTest(Point point) => false;
    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
    public void Dispose() => path.Dispose();
    public void Render(ImmediateDrawingContext context)
    {
        using var lease = (context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
            ?? throw new NotSupportedException("Material Loading Indicator requires Avalonia.Skia.")).Lease();
        using var paint = new SKPaint
        {
            IsAntialias = true, Style = SKPaintStyle.Fill,
            Color = new(color.R, color.G, color.B,
                (byte)Math.Clamp(Math.Round(color.A * lease.CurrentOpacity, MidpointRounding.AwayFromZero), 0, 255))
        };
        var canvas = lease.SkCanvas;
        canvas.Save();
        try
        {
            canvas.Scale(1 / scale); canvas.Translate(x, y);
            canvas.RotateDegrees(rotation, side / 2, side / 2);
            canvas.DrawPath(path, paint);
        }
        finally { canvas.Restore(); }
    }
}
