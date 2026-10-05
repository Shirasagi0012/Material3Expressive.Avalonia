using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace ComposeAnimation;

/// <summary>
/// Experimental actual-placement contract: arrange every child with LookaheadPlacement.ArrangeChild
/// and return the supplied finalSize from ArrangeOverride. This lets descendants use the current
/// parent's origin before Avalonia commits Bounds. Target measurement alone does not satisfy it.
/// </summary>
public interface ILookaheadPlacementHost { }

internal interface ILookaheadApproach
{
    void ResetApproach();
}

internal interface ILookaheadPlacementApproach : ILookaheadApproach
{
    bool AppliesPositionApproach => true;
    Point ApproachPosition(LookaheadContext root, Point target);
}

// Avalonia commits parent Bounds after ArrangeOverride returns. Carry the current pass's
// origin explicitly so nested placement never subtracts last frame's parent position.
public static class LookaheadPlacement
{
    [ThreadStatic] private static Frame? _current;
    private sealed record Frame(Control Owner, LookaheadContext Context, Point Origin, Frame? Previous = null);

    internal static void WithOrigin(Control owner, LookaheadContext context, Point origin, Action arrange)
    {
        var previous = _current;
        _current = new Frame(owner, context, origin, previous);
        try { arrange(); }
        finally { _current = previous; }
    }

    internal static Point RootPosition(Control control) => FrameFor(control)?.Origin ??
        throw new InvalidOperationException("A lookahead placement scope is required.");

    // Virtual inner bounds boundary: convert its root-space slot using the current parent
    // origin, then retain ordinary child alignment and nested position-approach compensation.
    internal static void ArrangeChildInRoot(Control parent, Control child, Rect slot)
    {
        var frame = FrameFor(parent) ?? throw new InvalidOperationException("A lookahead placement scope is required.");
        ArrangeChild(parent, child, new Rect(slot.TopLeft - new Vector(frame.Origin.X, frame.Origin.Y), slot.Size));
    }

    /// <summary>Arranges a direct visual child in a parent-local slot, propagating approach coordinates.</summary>
    public static void ArrangeChild(Control parent, Control child, Rect slot)
    {
        if (child.GetVisualParent() != parent)
            throw new InvalidOperationException("ArrangeChild requires a direct visual child; template bridges must participate explicitly.");
        slot = new Rect(slot.TopLeft + LookaheadCoordinates.EdgeOffset(parent, child), slot.Size);
        var frame = FrameFor(parent);
        if (frame is null) { child.Arrange(slot); return; }
        var constraints = LookaheadMeasure.ActualConstraints(child);
        var local = LookaheadLayoutBuilder.ResolveBounds(child, slot, child.DesiredSize, default, constraints);
        if (child is ILookaheadPlacementApproach { AppliesPositionApproach: true } approach &&
            frame.Context.Snapshot?.Results.TryGetValue(child, out var target) == true && child.IsVisible)
        {
            var position = approach.ApproachPosition(frame.Context, target.Bounds.TopLeft);
            var offset = position - (frame.Origin + new Vector(local.X, local.Y));
            slot = new Rect(slot.TopLeft + offset, slot.Size);
            local = LookaheadLayoutBuilder.ResolveBounds(child, slot, child.DesiredSize, default, constraints);
        }
        var previous = _current;
        _current = new Frame(child, frame.Context, frame.Origin + new Vector(local.X, local.Y), previous);
        // A same local slot can still have a different root origin. Revisit cooperative
        // descendants (including template bridges) even when Avalonia's arrange cache is valid.
        try { child.InvalidateArrange(); child.Arrange(slot); }
        finally { _current = previous; }
    }

    /// <summary>
    /// Arranges ordinary content of a LookaheadBoundary without leaking the outer placement frame.
    /// </summary>
    internal static void ArrangeNativeChild(Control parent, Control child, Rect slot)
    {
        if (child.GetVisualParent() != parent)
            throw new InvalidOperationException("ArrangeChild requires a direct visual child; template bridges must participate explicitly.");
        var previous = _current;
        _current = null;
        try { child.Arrange(slot); }
        finally { _current = previous; }
    }

    private static Frame? FrameFor(Control parent)
    {
        // Template/presentation bridges are not target members themselves; an active member frame
        // authorizes them to carry the same context through the current recursive arrange.
        var context = LookaheadScope.FindContext(parent);
        if (context is null) return null;
        if (ActiveFrame(parent) is { } active && active.Context == context) return active;
        if (parent == context.CoordinateReference) return new Frame(parent, context, default);
        // A direct framework arrange outside our recursive pass can use committed ancestors.
        // During a recursive pass, stop at the explicit origin rather than reading its Bounds.
        var origin = new Point();
        for (Visual? visual = parent; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual == context.CoordinateReference) return new Frame(parent, context, origin);
            if (LookaheadEnvironment.GetContext(visual) != context) return null;
            if (ActiveFrame(visual) is { } ancestor && ancestor.Context == context)
                return new Frame(parent, ancestor.Context, origin + new Vector(ancestor.Origin.X, ancestor.Origin.Y));
            origin += new Vector(visual.Bounds.X, visual.Bounds.Y);
        }
        return null;
    }

    private static Frame? ActiveFrame(Visual visual)
    {
        for (var frame = _current; frame is not null; frame = frame.Previous)
            if (frame.Owner == visual) return frame;
        return null;
    }
}
