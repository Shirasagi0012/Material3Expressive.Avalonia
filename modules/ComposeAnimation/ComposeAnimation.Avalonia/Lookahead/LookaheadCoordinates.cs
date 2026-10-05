using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace ComposeAnimation;

/// <summary>
/// Experimental continuous-placement contribution on direct child edges. Return a finite physical
/// DIP offset from current input state (never animation samples or Bounds). Target policies place
/// children WITHOUT this offset; LookaheadPlacement.ArrangeChild applies it automatically.
/// Raise MotionFrameChanged for every input change, even when arrange is already invalid.
/// </summary>
public interface ILookaheadMotionFrameProvider : ILookaheadPlacementHost
{
    Vector GetMotionFrameOffset(Control child);
    event EventHandler? MotionFrameChanged;
}

internal interface ILookaheadMotionFrameObserver
{
    void InvalidateMotionFrame();
}

/// <summary>Experimental translation-only coordinates within one cooperative target tree.
/// Render transforms, RTL mirroring and unregistered native scrolling are not included.</summary>
public static class LookaheadCoordinates
{
    /// <summary>Tries to read committed target bounds in the participant's local domain.</summary>
    public static bool TryGetTargetBounds(Control element, out Rect bounds,
        Visual? relativeTo = null, bool includeMotionFrameOfReference = false)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.VerifyAccess();
        bounds = default;
        var context = LookaheadScope.FindContext(element);
        var snapshot = context is null || context.IsTargetDirty ? null : context.Snapshot;
        if (snapshot is null || !snapshot.Results.TryGetValue(element, out var result) ||
            !TryResolveReference(element, context!, snapshot, relativeTo, out var reference))
            return false;
        var origin = ReferenceEquals(reference, snapshot.CoordinateReference)
            ? default
            : snapshot.Results[(Control)reference].Bounds.TopLeft;
        var position = new Point(result.Bounds.X - origin.X, result.Bounds.Y - origin.Y);
        if (includeMotionFrameOfReference)
            position += MotionOffset(element, reference);
        bounds = new Rect(position, result.Bounds.Size);
        return true;
    }

    /// <summary>Tries to read actual layout bounds in the participant's local domain.</summary>
    public static bool TryGetActualBounds(Control element, out Rect bounds,
        Visual? relativeTo = null, bool includeMotionFrameOfReference = false)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.VerifyAccess();
        bounds = default;
        var context = LookaheadScope.FindContext(element);
        var snapshot = context is null || context.IsTargetDirty ? null : context.Snapshot;
        if (snapshot is null || !snapshot.Results.ContainsKey(element) ||
            !TryResolveReference(element, context!, snapshot, relativeTo, out var reference))
            return false;
        var position = LookaheadPlacement.RootPosition(element);
        var origin = ReferenceEquals(reference, snapshot.CoordinateReference)
            ? default
            : LookaheadPlacement.RootPosition((Control)reference);
        position = new Point(position.X - origin.X, position.Y - origin.Y);
        if (!includeMotionFrameOfReference)
            position -= MotionOffset(element, reference);
        bounds = new Rect(position, element.Bounds.Size);
        return true;
    }

    /// <summary>Target position relative to an ancestor (the domain coordinate reference by default).
    /// Included continuous offsets are live inputs; the immutable target snapshot is never changed.</summary>
    public static Point TargetPosition(Control control, Control? reference = null, bool includeMotionFrameOfReference = true)
    {
        var root = ContextFor(control);
        reference = ResolveReference(control, root, reference);
        var position = root.Snapshot!.Results[control].Bounds.TopLeft;
        var origin = reference == root.CoordinateReference ? default : root.Snapshot.Results[reference].Bounds.TopLeft;
        return new Point(position.X - origin.X, position.Y - origin.Y) +
            (includeMotionFrameOfReference ? MotionOffset(control, reference) : default);
    }

    /// <summary>Actual position, including the current recursive arrange context before Bounds is committed.</summary>
    public static Point ActualPosition(Control control, Control? reference = null, bool includeMotionFrameOfReference = true)
    {
        var root = ContextFor(control);
        reference = ResolveReference(control, root, reference);
        var position = LookaheadPlacement.RootPosition(control);
        var origin = reference == root.CoordinateReference ? default : LookaheadPlacement.RootPosition(reference);
        return new Point(position.X - origin.X, position.Y - origin.Y) -
            (includeMotionFrameOfReference ? default : MotionOffset(control, reference));
    }

    internal static LookaheadContext ContextFor(Control control)
    {
        control.VerifyAccess();
        if (LookaheadScope.FindParticipatingContext(control) is { } root) return root;
        if (LookaheadScope.FindEnclosingBoundary(control) is { } boundary)
            throw new NotSupportedException(
                $"{control.GetType().Name} is inside LookaheadBoundary ({boundary.GetType().Name}) and is outside the cooperative target tree.");
        throw new InvalidOperationException("The control is not a registered member of a cooperative lookahead domain.");
    }

    internal static Control ResolveReference(Control control, LookaheadContext root, Control? reference)
    {
        reference ??= root.CoordinateReference;
        if (root.Snapshot?.Results.ContainsKey(control) != true ||
            !control.GetVisualAncestors().Contains(reference) && !(reference == control && reference == root.CoordinateReference) ||
            reference != root.CoordinateReference && (reference is not ILookaheadLayoutNode || reference is not ILookaheadPlacementHost ||
                !root.Snapshot.Results.ContainsKey(reference)))
            throw new NotSupportedException("ReferenceScope must be the domain coordinate reference or a registered cooperative ancestor in that domain.");
        return reference;
    }

    private static bool TryResolveReference(Control control, LookaheadContext context,
        LookaheadLayoutSnapshot snapshot, Visual? requested, out Control reference)
    {
        var candidate = requested ?? snapshot.CoordinateReference;
        if (candidate is not Control candidateControl ||
            !ReferenceEquals(candidate, snapshot.CoordinateReference) &&
            (candidate is not ILookaheadLayoutNode || candidate is not ILookaheadPlacementHost ||
             !snapshot.Results.ContainsKey(candidateControl)) ||
            !control.GetVisualAncestors().Contains(candidate) &&
            !(ReferenceEquals(candidate, control) && ReferenceEquals(candidate, snapshot.CoordinateReference)))
        {
            reference = null!;
            return false;
        }
        reference = candidateControl;
        return true;
    }

    internal static Vector EdgeOffset(Control parent, Control child)
    {
        var offset = parent is ILookaheadMotionFrameProvider provider ? provider.GetMotionFrameOffset(child) : default;
        if (!double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
            throw new InvalidOperationException("Motion-frame offsets must be finite.");
        return offset;
    }

    internal static Vector MotionOffset(Control control, Control reference)
    {
        var offset = new Vector();
        for (Visual? child = control; child != reference;)
        {
            var parent = child?.GetVisualParent() ?? throw new InvalidOperationException("The coordinate reference is not an ancestor.");
            if (parent is Control host && child is Control content) offset += EdgeOffset(host, content);
            child = parent;
        }
        if (!double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
            throw new InvalidOperationException("Accumulated motion-frame offsets must be finite.");
        return offset;
    }
}
