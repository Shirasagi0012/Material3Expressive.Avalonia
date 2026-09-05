using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ComposeAnimationRuntime.Avalonia;

public readonly record struct LayoutSnapshot(Rect LocalBounds, Rect RootBounds, Size DesiredSize, ITransform? Transform, Geometry? Clip, double Opacity, int ZIndex)
{
    public Matrix LocalToRootTransform { get; init; }
    public LayoutSnapshot(Rect bounds, Size desiredSize)
        : this(bounds, bounds, desiredSize, null, null, 1, 0)
        {
            LocalToRootTransform = Matrix.Identity;
        }
    public static LayoutSnapshot Capture(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        var root = control.FindAncestorOfType<SharedTransitionRoot>();
        return root is null ? Capture(control, control) : Capture(control, root);
    }

    public static LayoutSnapshot Capture(Control control, Visual root)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(root);

        var localBounds = new Rect(control.Bounds.Size);
        var transform = control.TransformToVisual(root) ?? Matrix.Identity;
        var rootBounds = TransformBounds(localBounds, transform);
        return new LayoutSnapshot(
            control.Bounds,
            rootBounds,
            control.DesiredSize,
            control.RenderTransform,
            control.Clip,
            control.Opacity,
            control.ZIndex)
        {
            LocalToRootTransform = transform
        };
    }

    public Point ToRoot(Point localPoint)
    {
        return LocalToRootTransform.Transform(localPoint - LocalBounds.Position);
    }

    public Point ToLocal(Point rootPoint)
    {
        return LocalToRootTransform.TryInvert(out var inverse)
            ? inverse.Transform(rootPoint) + LocalBounds.Position
            : LocalBounds.Position;
    }

    private static Rect TransformBounds(Rect bounds, Matrix matrix)
    {
        var p1 = matrix.Transform(bounds.TopLeft);
        var p2 = matrix.Transform(bounds.TopRight);
        var p3 = matrix.Transform(bounds.BottomRight);
        var p4 = matrix.Transform(bounds.BottomLeft);
        var minX = Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
        var minY = Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
        var maxX = Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
        var maxY = Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    public Rect Bounds => LocalBounds;
}
