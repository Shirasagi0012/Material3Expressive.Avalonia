using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ComposeAnimation;

/// <summary>
/// Host-space rectangles for shared content. Layout bounds strip library enter/exit presentation
/// (EnterExitSurface transforms and expand alignment) and keep user RenderTransforms. Display
/// bounds are the current on-screen AABB, including those effects. Animated drawing uses the
/// Runtime rect, never these layout samples.
/// </summary>
internal static class SharedLayoutBounds
{
    // Independent targets inside a Root are already root-relative. Only translate its stable
    // external layout origin; user scaling/rotation/mirroring cannot become fixed DIP constraints.
    internal static Point? LookaheadTranslation(Visual visual, Visual host)
    {
        if (TopLevel.GetTopLevel(visual) is not { } top || top != TopLevel.GetTopLevel(host)) return null;
        var origin = new Point();
        for (var current = visual; current != host;)
        {
            if (current.HasMirrorTransform || current is not EnterExitSurface &&
                current.RenderTransform?.Value is { } render && render != Matrix.Identity)
                throw new NotSupportedException("SharedBounds lookahead currently requires layout translations only (no user render transforms or mirroring) between Root and Host.");
            if (!(current is EnterExitSurface && current.GetVisualParent() is EnterExitLayout { Motion.ChangesSize: true }))
                origin += new Vector(current.Bounds.X, current.Bounds.Y);
            if (current.GetVisualParent() is not { } parent) return null;
            current = parent;
        }
        return origin;
    }
    internal static Rect? ReadLayout(Visual visual, Visual host) => Read(visual, host, display: false);
    internal static Rect? ReadDisplay(Visual visual, Visual host) => Read(visual, host, display: true);

    private static Rect? Read(Visual visual, Visual host, bool display)
    {
        if (visual is not Layoutable { IsMeasureValid: true, IsArrangeValid: true } ||
            host is not Layoutable { IsMeasureValid: true, IsArrangeValid: true })
            return null;
        if (TopLevel.GetTopLevel(visual) is not { } top || !ReferenceEquals(top, TopLevel.GetTopLevel(host)))
            return null;
        if (visual.FindCommonVisualAncestor(host) is null) return null;
        if (display)
        {
            var transform = visual.TransformToVisual(host);
            return transform is { } matrix && matrix.HasInverse
                ? Valid(new Rect(visual.Bounds.Size).TransformToAABB(matrix)) : null;
        }
        var result = Matrix.Identity;
        Visual? current = visual;
        while (current is not null && current != host)
        {
            var skipRender = !display && current is EnterExitSurface;
            var skipOffset = !display && current is EnterExitSurface &&
                current.GetVisualParent() is EnterExitLayout { Motion.ChangesSize: true };
            if (current.HasMirrorTransform)
                result *= new Matrix(-1, 0, 0, 1, current.Bounds.Width, 0);
            if (!skipRender && current.RenderTransform?.Value is { } render)
            {
                var origin = current.RenderTransformOrigin.ToPixels(current.Bounds.Size);
                var offset = Matrix.CreateTranslation(origin);
                var transform = -offset * render * offset;
                if (!transform.HasInverse) return null;
                result *= transform;
            }
            if (!skipOffset && current.Bounds.TopLeft != default)
                result *= Matrix.CreateTranslation(current.Bounds.TopLeft);
            current = current.GetVisualParent();
        }
        return current == host ? Valid(new Rect(visual.Bounds.Size).TransformToAABB(result)) : null;
    }

    internal static Rect? Valid(Rect bounds) =>
        double.IsFinite(bounds.X) && double.IsFinite(bounds.Y) &&
        double.IsFinite(bounds.Width) && double.IsFinite(bounds.Height) &&
        bounds.Width > 0 && bounds.Height > 0
            ? bounds : null;
}
