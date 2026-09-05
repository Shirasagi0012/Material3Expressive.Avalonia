using Avalonia;
using Avalonia.Media;

namespace ComposeAnimationRuntime.Avalonia;

public readonly record struct SharedGeometry(
    Rect Bounds,
    ITransform? Transform = null,
    Geometry? Clip = null,
    double Opacity = 1,
    int ZIndex = 0,
    CornerRadius CornerRadius = default)
{
    public Point Position => Bounds.Position;
    public Size Size => Bounds.Size;

    public SharedGeometry WithBounds(Rect bounds) => this with { Bounds = bounds };

    public static SharedGeometry Lerp(SharedGeometry from, SharedGeometry to, double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        var bounds = new Rect(
            from.Bounds.X + (to.Bounds.X - from.Bounds.X) * progress,
            from.Bounds.Y + (to.Bounds.Y - from.Bounds.Y) * progress,
            from.Bounds.Width + (to.Bounds.Width - from.Bounds.Width) * progress,
            from.Bounds.Height + (to.Bounds.Height - from.Bounds.Height) * progress);
        return new SharedGeometry(
            bounds,
            progress < 1 ? from.Transform : to.Transform,
            progress < 1 ? from.Clip : to.Clip,
            from.Opacity + (to.Opacity - from.Opacity) * progress,
            progress < 1 ? from.ZIndex : to.ZIndex,
            new CornerRadius(
                from.CornerRadius.TopLeft + (to.CornerRadius.TopLeft - from.CornerRadius.TopLeft) * progress,
                from.CornerRadius.TopRight + (to.CornerRadius.TopRight - from.CornerRadius.TopRight) * progress,
                from.CornerRadius.BottomRight + (to.CornerRadius.BottomRight - from.CornerRadius.BottomRight) * progress,
                from.CornerRadius.BottomLeft + (to.CornerRadius.BottomLeft - from.CornerRadius.BottomLeft) * progress));
    }
}
