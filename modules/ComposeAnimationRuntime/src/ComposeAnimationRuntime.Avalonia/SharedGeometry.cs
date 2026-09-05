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
            LerpTransform(from.Transform, to.Transform, progress),
            LerpClip(from.Clip, to.Clip, progress),
            from.Opacity + (to.Opacity - from.Opacity) * progress,
            progress < 1 ? from.ZIndex : to.ZIndex,
            new CornerRadius(
                from.CornerRadius.TopLeft + (to.CornerRadius.TopLeft - from.CornerRadius.TopLeft) * progress,
                from.CornerRadius.TopRight + (to.CornerRadius.TopRight - from.CornerRadius.TopRight) * progress,
                from.CornerRadius.BottomRight + (to.CornerRadius.BottomRight - from.CornerRadius.BottomRight) * progress,
                from.CornerRadius.BottomLeft + (to.CornerRadius.BottomLeft - from.CornerRadius.BottomLeft) * progress));
    }

    private static ITransform? LerpTransform(ITransform? from, ITransform? to, double progress)
    {
        if (from is MatrixTransform fromMatrix && to is MatrixTransform toMatrix)
            return new MatrixTransform(new Matrix(
                fromMatrix.Matrix.M11 + (toMatrix.Matrix.M11 - fromMatrix.Matrix.M11) * progress,
                fromMatrix.Matrix.M12 + (toMatrix.Matrix.M12 - fromMatrix.Matrix.M12) * progress,
                fromMatrix.Matrix.M21 + (toMatrix.Matrix.M21 - fromMatrix.Matrix.M21) * progress,
                fromMatrix.Matrix.M22 + (toMatrix.Matrix.M22 - fromMatrix.Matrix.M22) * progress,
                fromMatrix.Matrix.M31 + (toMatrix.Matrix.M31 - fromMatrix.Matrix.M31) * progress,
                fromMatrix.Matrix.M32 + (toMatrix.Matrix.M32 - fromMatrix.Matrix.M32) * progress));
        return progress < 1 ? from : to;
    }

    private static Geometry? LerpClip(Geometry? from, Geometry? to, double progress)
    {
        if (from is RectangleGeometry fromRect && to is RectangleGeometry toRect)
            return new RectangleGeometry(new Rect(
                fromRect.Rect.X + (toRect.Rect.X - fromRect.Rect.X) * progress,
                fromRect.Rect.Y + (toRect.Rect.Y - fromRect.Rect.Y) * progress,
                fromRect.Rect.Width + (toRect.Rect.Width - fromRect.Rect.Width) * progress,
                fromRect.Rect.Height + (toRect.Rect.Height - fromRect.Rect.Height) * progress));
        return progress < 1 ? from : to;
    }
}
