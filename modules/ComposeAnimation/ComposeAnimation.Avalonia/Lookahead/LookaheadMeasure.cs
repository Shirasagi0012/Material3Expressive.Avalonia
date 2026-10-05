using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace ComposeAnimation;

/// <summary>
/// Experimental full-constraint participant. Implement the target and actual policies, forward
/// MeasureCore/ArrangeCore to LookaheadMeasure.TryMeasureCore/TryArrangeCore, and call Forget on
/// detach. Policies receive constraints after margin and local size preferences; return a finite
/// non-negative size. The helper applies minima, maxima and rounding. ArrangeOverride must return
/// the supplied finalSize. Use the measure helpers for children, including the legacy Size path.
/// </summary>
public interface ILookaheadConstrainedLayoutNode : ILookaheadLayoutNode
{
    Size MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints);
    Size MeasureApproach(LookaheadConstraints constraints);
}

/// <summary>
/// Experimental bridge into real Avalonia layout. Full constraints are opt-in per measurement;
/// Size overloads explicitly restore native layout. No live properties or framework caches are patched.
/// </summary>
public static class LookaheadMeasure
{
    private sealed class State
    {
        public LookaheadConstraints Requested;
        public LookaheadConstraints Effective;
        public Visual? Parent;
        public LookaheadContext? Context;
        public long MembershipEpoch;
        public bool Applied;
    }
    private static readonly ConditionalWeakTable<Control, State> States = new();

    /// <summary>Real full-constraint measurement; rejects controls without the full protocol.</summary>
    public static Size Measure(Control control, LookaheadConstraints constraints)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.VerifyAccess();
        if (control is not ILookaheadConstrainedLayoutNode)
            throw new NotSupportedException($"{control.GetType().Name} does not implement ILookaheadConstrainedLayoutNode.");
        LookaheadScope.FindParticipatingContext(control)?.VerifyLayoutAllowed();
        LookaheadEntry.Ensure(control);
        var parent = control.GetVisualParent();
        var root = FindContext(control);
        if (!States.TryGetValue(control, out var state) || state.Requested != constraints ||
            state.Parent != parent || state.Context != root || state.MembershipEpoch != (root?.MembershipEpoch ?? 0))
        {
            state = new State { Requested = constraints, Parent = parent, Context = root,
                MembershipEpoch = root?.MembershipEpoch ?? 0 };
            States.Remove(control);
            States.Add(control, state);
            // Avalonia's native cache key contains only the maximum Size, not the minima/mode.
            control.InvalidateMeasure();
            control.InvalidateArrange();
        }
        try
        {
            control.Measure(constraints.MaxSize);
            if (!state.Applied)
                throw new InvalidOperationException($"{control.GetType().Name} must forward MeasureCore to LookaheadMeasure.TryMeasureCore.");
            return control.DesiredSize;
        }
        catch
        {
            Forget(control); // A failed Measure must not leave a valid native cache or active constraint mode.
            throw;
        }
    }

    /// <summary>Native maximum-size measurement, clearing any previous full-constraint request.</summary>
    public static Size Measure(Control control, Size available)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.VerifyAccess();
        LookaheadScope.FindParticipatingContext(control)?.VerifyLayoutAllowed();
        Forget(control);
        control.Measure(available);
        return control.DesiredSize;
    }

    /// <summary>
    /// Explicit compatibility boundary: capable children use the full protocol; legacy children
    /// may use native Measure only if no positive minimum would be lost. Their own Avalonia hard
    /// sizes/overflow semantics remain native. Use Measure(control, constraints) to require capability.
    /// </summary>
    public static Size MeasureChild(Control control, LookaheadConstraints constraints)
    {
        if (control is ILookaheadConstrainedLayoutNode) return Measure(control, constraints);
        if (constraints.HasMinimum)
            throw new NotSupportedException($"Cannot pass minimum constraints to legacy {control.GetType().Name}. " +
                "Use a full-constraint participant or explicitly loosen the child constraints.");
        return Measure(control, constraints.MaxSize);
    }

    /// <summary>Call at the start of an opted-in control's MeasureCore; false means call base.</summary>
    public static bool TryMeasureCore(Control control, Size available, out Size desired)
    {
        LookaheadScope.FindParticipatingContext(control)?.VerifyLayoutAllowed();
        desired = default;
        if (FindContext(control)?.TryPreparationMeasure(control, out var prepared) == true)
        {
            // The native layout manager may revisit a descendant between entry Measure and
            // Arrange. Supply the candidate size without publishing or animating its position.
            if (States.TryGetValue(control, out var pending))
            {
                pending.Effective = prepared.FullMeasureConstraints ?? LookaheadConstraints.AtMost(prepared.MeasureConstraints);
                pending.Applied = true;
            }
            desired = prepared.DesiredSize;
            return true;
        }
        if (!TryState(control, out var state)) return false;
        if (available != state.Requested.MaxSize)
        {
            // A native caller changed maximum constraints without using the explicit bridge.
            Forget(control);
            return false;
        }
        state.Applied = false;
        if (!control.IsVisible) { state.Applied = true; return true; }
        try
        {
            control.ApplyStyling();
            control.ApplyTemplate();
            var margin = Margin(control);
            state.Effective = state.Requested.Deflate(margin).ApplyTo(control);
            var measured = ((ILookaheadConstrainedLayoutNode)control).MeasureApproach(state.Effective);
            desired = Desired(control, measured, state.Effective, state.Requested, margin);
            state.Applied = true;
            return true;
        }
        catch
        {
            // Also covers layout-manager direct remeasurement, outside our Measure entry point.
            Forget(control);
            throw;
        }
    }

    /// <summary>
    /// Call from ArrangeCore; assign returned bounds to the protected Bounds property when true,
    /// otherwise call base. The callback is the real ArrangeOverride, not a speculative layout.
    /// </summary>
    public static bool TryArrangeCore(Control control, Rect slot, Func<Size, Size> arrange, out Rect bounds)
    {
        LookaheadScope.FindParticipatingContext(control)?.VerifyLayoutAllowed();
        bounds = control.Bounds;
        if (!TryState(control, out var state) || !state.Applied) return false;
        if (!control.IsVisible) return true;
        bounds = LookaheadLayoutBuilder.ResolveBounds(control, slot, control.DesiredSize, default, state.Effective);
        try
        {
            var actual = arrange(bounds.Size);
            if (actual != bounds.Size)
                throw new InvalidOperationException("Full-constraint ArrangeOverride must return its supplied finalSize.");
            return true;
        }
        catch
        {
            control.InvalidateArrange();
            throw;
        }
    }

    /// <summary>Forget constraint mode on detach or an explicit return to native layout.</summary>
    public static void Forget(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.VerifyAccess();
        if (!States.Remove(control)) return;
        control.InvalidateMeasure();
        control.InvalidateArrange();
    }

    internal static LookaheadConstraints? ActualConstraints(Control control)
        => TryState(control, out var state) && state.Applied ? state.Effective : null;

    internal static LookaheadConstraints? PendingConstraints(Control control, Size available)
        => TryState(control, out var state) && !state.Applied && state.Requested.MaxSize == available
            ? state.Requested : null;

    internal static void AcceptPrepared(Control control, LookaheadLayoutResult? prepared)
    {
        if (prepared?.FullMeasureConstraints is not { } constraints || !States.TryGetValue(control, out var state)) return;
        state.Parent = control.GetVisualParent();
        state.Context = FindContext(control);
        state.MembershipEpoch = state.Context?.MembershipEpoch ?? 0;
        state.Effective = constraints;
        state.Applied = true;
    }

    internal static Size Desired(Control control, Size measured, LookaheadConstraints effective,
        LookaheadConstraints incoming, Thickness margin)
    {
        if (!double.IsFinite(measured.Width) || !double.IsFinite(measured.Height) || measured.Width < 0 || measured.Height < 0)
            throw new InvalidOperationException("A full-constraint policy must return a finite non-negative size.");
        var size = effective.Constrain(measured);
        if (control.UseLayoutRounding)
            size = LayoutHelper.RoundLayoutSizeUp(size, LayoutHelper.GetLayoutScale(control));
        // DIP constraints take precedence if a fixed fractional size lies between physical pixels.
        return incoming.Constrain(effective.Constrain(size).Inflate(margin));
    }

    internal static Thickness Margin(Control control) => control.UseLayoutRounding
        ? LayoutHelper.RoundLayoutThickness(control.Margin, LayoutHelper.GetLayoutScale(control)) : control.Margin;

    private static LookaheadContext? FindContext(Control control) => LookaheadScope.FindContext(control);

    private static bool TryState(Control control, out State state)
    {
        if (!States.TryGetValue(control, out state!)) return false;
        if (state.Parent == control.GetVisualParent() && state.Context == FindContext(control) &&
            state.MembershipEpoch == (state.Context?.MembershipEpoch ?? 0)) return true;
        Forget(control);
        return false;
    }
}
