using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace ComposeAnimation;

public enum LookaheadLayoutPhase
{
    Native,
    Target,
    Approach
}

/// <summary>A single layout policy used for native, target, and approach layout.</summary>
public interface ILookaheadLayoutPolicy : ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost
{
    LookaheadMeasureResult MeasureLayout(LookaheadMeasureContext context, LookaheadConstraints constraints);

    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
        => builder.MeasurePolicy((Control)this, this, LookaheadConstraints.AtMost(available));

    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(
        LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        => builder.MeasurePolicy((Control)this, this, constraints);

    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints)
        => LookaheadPolicyRuntime.MeasureActual((Control)this, this, LookaheadLayoutPhase.Approach, constraints);

    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
        => builder.ArrangePolicy((Control)this, bounds.Size);
}

public sealed class LookaheadMeasureContext
{
    private readonly LookaheadPolicyPass _pass;

    internal LookaheadMeasureContext(LookaheadPolicyPass pass) => _pass = pass;

    public LookaheadLayoutPhase Phase => _pass.Phase;

    public LookaheadPlaceable Measure(Control child, LookaheadConstraints constraints)
        => _pass.Measure(child, constraints);

    public bool TryGetTarget(Control child, out LookaheadLayoutResult result)
        => _pass.TryGetTarget(child, out result);

    public LookaheadMeasureResult Layout(Size size,
        Action<LookaheadPlacementContext>? placement = null, object? layoutData = null)
        => _pass.Layout(size, placement, layoutData);
}

public sealed class LookaheadMeasureResult
{
    private readonly LookaheadPolicyPass _pass;

    internal LookaheadMeasureResult(LookaheadPolicyPass pass, Size size,
        Action<LookaheadPlacementContext>? placement, object? layoutData)
    {
        _pass = pass;
        Size = size;
        Placement = placement;
        LayoutData = layoutData;
    }

    public Size Size { get; }
    public object? LayoutData { get; }
    internal Action<LookaheadPlacementContext>? Placement { get; }
    internal bool BelongsTo(LookaheadPolicyPass pass) => ReferenceEquals(_pass, pass);
}

public sealed class LookaheadPlaceable
{
    private readonly LookaheadPolicyPass _pass;

    internal LookaheadPlaceable(LookaheadPolicyPass pass, Control child, Size desiredSize, int revision)
    {
        _pass = pass;
        Child = child;
        DesiredSize = desiredSize;
        Revision = revision;
    }

    public Size DesiredSize { get; }
    internal Control Child { get; }
    internal int Revision { get; }
    internal bool BelongsTo(LookaheadPolicyPass pass) => ReferenceEquals(_pass, pass);
}

public sealed class LookaheadPlacementContext
{
    private readonly LookaheadPolicyPass _pass;
    private readonly long _token;

    internal LookaheadPlacementContext(LookaheadPolicyPass pass, Size finalSize, long token)
    {
        _pass = pass;
        _token = token;
        FinalSize = finalSize;
    }

    public LookaheadLayoutPhase Phase => _pass.Phase;
    public Size FinalSize { get; }

    public void Arrange(LookaheadPlaceable child, Rect slot) => _pass.Arrange(child, slot, _token);
}

internal sealed class LookaheadPolicyPass
{
    private readonly Control _owner;
    private readonly ILookaheadLayoutPolicy _policy;
    private readonly Func<Control, LookaheadConstraints, Size> _measure;
    private readonly Action<Control, Rect> _arrange;
    private readonly Func<Control, LookaheadLayoutResult?>? _target;
    private readonly Control[] _children;
    private readonly Dictionary<Control, int> _revisions = [];
    private HashSet<Control>? _placed;
    private HashSet<Control> _lastPlaced = [];
    private bool _measuring = true;
    private bool _placing;
    private long _placementToken;

    internal LookaheadPolicyPass(Control owner, ILookaheadLayoutPolicy policy, LookaheadLayoutPhase phase,
        Func<Control, LookaheadConstraints, Size> measure, Action<Control, Rect> arrange,
        Func<Control, LookaheadLayoutResult?>? target)
    {
        _owner = owner;
        _policy = policy;
        Phase = phase;
        _measure = measure;
        _arrange = arrange;
        _target = target;
        _children = policy.LookaheadChildren.ToArray();
        if (_children.Distinct(ReferenceEqualityComparer.Instance).Count() != _children.Length)
            throw new InvalidOperationException("LookaheadChildren must not contain the same control more than once.");
    }

    internal LookaheadLayoutPhase Phase { get; }

    internal LookaheadMeasureResult RunMeasure(LookaheadConstraints constraints)
    {
        LookaheadMeasureResult result;
        try { result = _policy.MeasureLayout(new LookaheadMeasureContext(this), constraints); }
        finally { _measuring = false; }
        if (result is null || !result.BelongsTo(this))
            throw new InvalidOperationException("MeasureLayout must return a result created by the current context.Layout call.");
        ValidateSize(result.Size);
        VerifyChildrenUnchanged();
        return result;
    }

    internal LookaheadPlaceable Measure(Control child, LookaheadConstraints constraints)
    {
        if (!_measuring || _placing)
            throw new InvalidOperationException("Children may only be measured while MeasureLayout is running.");
        ArgumentNullException.ThrowIfNull(child);
        if (!_children.Contains(child, ReferenceEqualityComparer.Instance))
            throw new InvalidOperationException("Measure can only be used with a declared direct LookaheadChild.");
        var desired = _measure(child, constraints);
        var revision = _revisions.GetValueOrDefault(child) + 1;
        _revisions[child] = revision;
        return new LookaheadPlaceable(this, child, desired, revision);
    }

    internal bool TryGetTarget(Control child, out LookaheadLayoutResult result)
    {
        ArgumentNullException.ThrowIfNull(child);
        result = null!;
        if (!_measuring || Phase != LookaheadLayoutPhase.Approach || _target is null ||
            !_children.Contains(child, ReferenceEqualityComparer.Instance)) return false;
        var value = _target(child);
        if (value is null) return false;
        result = value;
        return true;
    }

    internal LookaheadMeasureResult Layout(Size size,
        Action<LookaheadPlacementContext>? placement, object? layoutData)
    {
        if (!_measuring || _placing)
            throw new InvalidOperationException("Layout may only be created while MeasureLayout is running.");
        ValidateSize(size);
        return new LookaheadMeasureResult(this, size, placement, layoutData);
    }

    internal void RunPlacement(LookaheadMeasureResult result, Size finalSize)
    {
        if (!result.BelongsTo(this))
            throw new InvalidOperationException("A layout result cannot be used across phases or layout generations.");
        ValidateSize(finalSize);
        VerifyChildrenUnchanged();
        _placed = [];
        _placing = true;
        var token = ++_placementToken;
        try { result.Placement?.Invoke(new LookaheadPlacementContext(this, finalSize, token)); }
        finally
        {
            _lastPlaced = _placed is null ? [] : [.. _placed];
            _placing = false;
            _placed = null;
        }
        VerifyChildrenUnchanged();
    }

    internal IEnumerable<Control> UnplacedMeasuredChildren()
        => _revisions.Keys.Where(child => !_lastPlaced.Contains(child));

    internal void Arrange(LookaheadPlaceable placeable, Rect slot, long token)
    {
        if (!_placing || _placed is null || token != _placementToken)
            throw new InvalidOperationException("Arrange may only be called by the active placement callback.");
        ArgumentNullException.ThrowIfNull(placeable);
        if (!placeable.BelongsTo(this) ||
            !_revisions.TryGetValue(placeable.Child, out var current) || current != placeable.Revision)
            throw new InvalidOperationException("The placeable is stale or belongs to another phase or layout generation.");
        if (!_placed.Add(placeable.Child))
            throw new InvalidOperationException("A visible child may be arranged at most once per placement pass.");
        ValidateRect(slot);
        _arrange(placeable.Child, slot);
    }

    private void VerifyChildrenUnchanged()
    {
        var current = _policy.LookaheadChildren.ToArray();
        if (current.Length != _children.Length ||
            current.Where((child, index) => !ReferenceEquals(child, _children[index])).Any())
            throw new InvalidOperationException("A layout policy must not modify its child tree during measurement or placement.");
    }

    private static void ValidateSize(Size size)
    {
        if (!double.IsFinite(size.Width) || !double.IsFinite(size.Height) || size.Width < 0 || size.Height < 0)
            throw new InvalidOperationException("A layout policy must return a finite non-negative size.");
    }

    private static void ValidateRect(Rect rect)
    {
        if (!double.IsFinite(rect.X) || !double.IsFinite(rect.Y) ||
            !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height) ||
            rect.Width < 0 || rect.Height < 0)
            throw new InvalidOperationException("A layout policy must arrange children into finite non-negative rectangles.");
    }
}

internal static class LookaheadPolicyRuntime
{
    private sealed class ActualState
    {
        internal required LookaheadPolicyPass Pass;
        internal required LookaheadMeasureResult Result;
    }

    private static readonly ConditionalWeakTable<Control, ActualState> ActualStates = new();

    internal static LookaheadLayoutPhase ActualPhase(Control owner)
    {
        var context = LookaheadScope.FindParticipatingContext(owner);
        return context?.Snapshot is not null
            ? LookaheadLayoutPhase.Approach
            : LookaheadLayoutPhase.Native;
    }

    internal static Size MeasureActual(Control owner, ILookaheadLayoutPolicy policy,
        LookaheadLayoutPhase phase, LookaheadConstraints constraints)
    {
        if (phase == LookaheadLayoutPhase.Target)
            throw new ArgumentOutOfRangeException(nameof(phase));
        ActualStates.Remove(owner);
        var context = LookaheadScope.FindParticipatingContext(owner);
        var pass = new LookaheadPolicyPass(owner, policy, phase,
            (child, childConstraints) =>
            {
                if (phase != LookaheadLayoutPhase.Native || child is ILookaheadConstrainedLayoutNode)
                    return LookaheadMeasure.MeasureChild(child, childConstraints);
                return childConstraints.Constrain(LookaheadMeasure.Measure(child, childConstraints.MaxSize));
            },
            (child, slot) => LookaheadPlacement.ArrangeChild(owner, child, slot),
            phase == LookaheadLayoutPhase.Approach && context?.Snapshot is { } snapshot
                ? child => snapshot.Results.GetValueOrDefault(child)
                : null);
        var result = pass.RunMeasure(constraints);
        ActualStates.Add(owner, new ActualState { Pass = pass, Result = result });
        return result.Size;
    }

    internal static Size ArrangeActual(Control owner, Size finalSize)
    {
        if (!ActualStates.TryGetValue(owner, out var state))
            throw new InvalidOperationException("Arrange requires a successful policy measurement in the same phase and generation.");
        state.Pass.RunPlacement(state.Result, finalSize);
        return finalSize;
    }

    internal static bool HasActualState(Control owner) => ActualStates.TryGetValue(owner, out _);

    internal static void Forget(Control owner) => ActualStates.Remove(owner);
}

/// <summary>Low-level bridge for controls that cannot derive from the supplied helper bases.</summary>
public static class LookaheadLayout
{
    private sealed class EntryHolder(Control owner)
    {
        internal readonly LookaheadEntry Entry = new(owner);
    }

    private static readonly ConditionalWeakTable<Control, EntryHolder> Entries = new();

    internal static void Register(Control owner)
        => Entries.GetValue(owner, static value => new EntryHolder(value));

    public static bool TryMeasureCore(Control owner, Size availableSize, out Size desiredSize)
    {
        ArgumentNullException.ThrowIfNull(owner);
        owner.VerifyAccess();
        desiredSize = default;
        if (owner is not ILookaheadLayoutPolicy) return false;
        var fellBack = false;
        desiredSize = Entries.GetValue(owner, static value => new EntryHolder(value)).Entry.MeasureCore(
            availableSize, () => { fellBack = true; return default; });
        if (!fellBack) return true;
        if (LookaheadScope.FindParticipatingContext(owner)?.Snapshot is null) return false;

        owner.ApplyStyling();
        owner.ApplyTemplate();
        var incoming = LookaheadConstraints.AtMost(availableSize);
        var margin = LookaheadMeasure.Margin(owner);
        var effective = incoming.Deflate(margin).ApplyTo(owner);
        var measured = LookaheadPolicyRuntime.MeasureActual(
            owner, (ILookaheadLayoutPolicy)owner, LookaheadLayoutPhase.Approach, effective);
        desiredSize = LookaheadMeasure.Desired(owner, measured, effective, incoming, margin);
        return true;
    }

    public static bool TryArrangeCore(Control owner, Rect finalRect, out Rect bounds)
    {
        ArgumentNullException.ThrowIfNull(owner);
        owner.VerifyAccess();
        bounds = owner.Bounds;
        if (owner is not ILookaheadLayoutPolicy || !Entries.TryGetValue(owner, out var holder)) return false;
        if (holder.Entry.TryArrangeCore(finalRect,
            size => LookaheadPolicyRuntime.ArrangeActual(owner, size), out bounds)) return true;
        if (!LookaheadPolicyRuntime.HasActualState(owner) ||
            LookaheadScope.FindParticipatingContext(owner)?.Snapshot is null) return false;
        bounds = LookaheadLayoutBuilder.ResolveBounds(owner, finalRect, owner.DesiredSize, default);
        var actual = LookaheadPolicyRuntime.ArrangeActual(owner, bounds.Size);
        if (actual != bounds.Size)
            throw new InvalidOperationException("Lookahead policy arrangement must return its supplied finalSize.");
        return true;
    }

    public static void Forget(Control owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        owner.VerifyAccess();
        LookaheadPolicyRuntime.Forget(owner);
        if (!Entries.TryGetValue(owner, out var holder)) return;
        holder.Entry.Dispose();
        Entries.Remove(owner);
    }

    internal static void VisualParentChanged(Control owner)
    {
        if (Entries.TryGetValue(owner, out var holder)) holder.Entry.VisualParentChanged();
        LookaheadPolicyRuntime.Forget(owner);
    }
}

public abstract class LookaheadPanel : Panel, ILookaheadLayoutPolicy, ILookaheadPlacementHost
{
    protected LookaheadPanel() => LookaheadLayout.Register(this);
    protected virtual IEnumerable<Control> LookaheadChildren => Children;
    protected virtual INotifyCollectionChanged? LookaheadChildrenChanged => Children;
    protected virtual IEnumerable<AvaloniaProperty> LookaheadProperties => [];
    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => LookaheadChildren;
    INotifyCollectionChanged? ILookaheadLayoutNode.LookaheadChildrenChanged => LookaheadChildrenChanged;
    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties => LookaheadProperties;
    LookaheadMeasureResult ILookaheadLayoutPolicy.MeasureLayout(
        LookaheadMeasureContext context, LookaheadConstraints constraints) => MeasureLayout(context, constraints);

    protected abstract LookaheadMeasureResult MeasureLayout(
        LookaheadMeasureContext context, LookaheadConstraints constraints);

    protected override Size MeasureOverride(Size availableSize)
        => LookaheadPolicyRuntime.MeasureActual(this, this, LookaheadPolicyRuntime.ActualPhase(this),
            LookaheadConstraints.AtMost(availableSize));

    protected override Size ArrangeOverride(Size finalSize) => LookaheadPolicyRuntime.ArrangeActual(this, finalSize);

    protected override Size MeasureCore(Size availableSize)
        => LookaheadLayout.TryMeasureCore(this, availableSize, out var desired)
            ? desired : base.MeasureCore(availableSize);

    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadLayout.TryArrangeCore(this, finalRect, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty) LookaheadLayout.VisualParentChanged(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LookaheadLayout.Forget(this);
        base.OnDetachedFromVisualTree(e);
    }
}

public abstract class LookaheadDecorator : Decorator, ILookaheadLayoutPolicy, ILookaheadPlacementHost
{
    protected LookaheadDecorator() => LookaheadLayout.Register(this);
    protected virtual IEnumerable<Control> LookaheadChildren
        => Child is { } child ? [child] : [];
    protected virtual INotifyCollectionChanged? LookaheadChildrenChanged => null;
    protected virtual IEnumerable<AvaloniaProperty> LookaheadProperties => [];
    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => LookaheadChildren;
    INotifyCollectionChanged? ILookaheadLayoutNode.LookaheadChildrenChanged => LookaheadChildrenChanged;
    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties => LookaheadProperties;
    LookaheadMeasureResult ILookaheadLayoutPolicy.MeasureLayout(
        LookaheadMeasureContext context, LookaheadConstraints constraints) => MeasureLayout(context, constraints);

    protected abstract LookaheadMeasureResult MeasureLayout(
        LookaheadMeasureContext context, LookaheadConstraints constraints);

    protected override Size MeasureOverride(Size availableSize)
        => LookaheadPolicyRuntime.MeasureActual(this, this, LookaheadPolicyRuntime.ActualPhase(this),
            LookaheadConstraints.AtMost(availableSize));

    protected override Size ArrangeOverride(Size finalSize) => LookaheadPolicyRuntime.ArrangeActual(this, finalSize);

    protected override Size MeasureCore(Size availableSize)
        => LookaheadLayout.TryMeasureCore(this, availableSize, out var desired)
            ? desired : base.MeasureCore(availableSize);

    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadLayout.TryArrangeCore(this, finalRect, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty) LookaheadLayout.VisualParentChanged(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LookaheadLayout.Forget(this);
        base.OnDetachedFromVisualTree(e);
    }
}

public abstract class LookaheadControl : Control, ILookaheadLayoutPolicy, ILookaheadPlacementHost
{
    protected LookaheadControl() => LookaheadLayout.Register(this);
    protected virtual IEnumerable<Control> LookaheadChildren => [];
    protected virtual INotifyCollectionChanged? LookaheadChildrenChanged => null;
    protected virtual IEnumerable<AvaloniaProperty> LookaheadProperties => [];
    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => LookaheadChildren;
    INotifyCollectionChanged? ILookaheadLayoutNode.LookaheadChildrenChanged => LookaheadChildrenChanged;
    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties => LookaheadProperties;
    LookaheadMeasureResult ILookaheadLayoutPolicy.MeasureLayout(
        LookaheadMeasureContext context, LookaheadConstraints constraints) => MeasureLayout(context, constraints);

    protected abstract LookaheadMeasureResult MeasureLayout(
        LookaheadMeasureContext context, LookaheadConstraints constraints);

    protected override Size MeasureOverride(Size availableSize)
        => LookaheadPolicyRuntime.MeasureActual(this, this, LookaheadPolicyRuntime.ActualPhase(this),
            LookaheadConstraints.AtMost(availableSize));

    protected override Size ArrangeOverride(Size finalSize) => LookaheadPolicyRuntime.ArrangeActual(this, finalSize);

    protected override Size MeasureCore(Size availableSize)
        => LookaheadLayout.TryMeasureCore(this, availableSize, out var desired)
            ? desired : base.MeasureCore(availableSize);

    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadLayout.TryArrangeCore(this, finalRect, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty) LookaheadLayout.VisualParentChanged(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LookaheadLayout.Forget(this);
        base.OnDetachedFromVisualTree(e);
    }
}
