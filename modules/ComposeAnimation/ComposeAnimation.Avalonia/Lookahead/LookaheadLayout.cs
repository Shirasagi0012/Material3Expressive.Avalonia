using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace ComposeAnimation;

/// <summary>An experimental target layout result. Bounds exclude render transforms and use the snapshot's coordinate reference.</summary>
public sealed record LookaheadLayoutResult(
    Size MeasureConstraints, Size MeasuredSize, Size DesiredSize, Rect Bounds)
{
    /// <summary>Full effective constraints for an explicitly opted-in measurement; null on the legacy path.</summary>
    public LookaheadConstraints? FullMeasureConstraints { get; init; }
    /// <summary>Optional immutable policy data belonging to this target generation, never actual samples.</summary>
    public object? LayoutData { get; init; }
    internal bool HasLayoutFootprint { get; init; } = true;
}

/// <summary>A complete, immutable target layout generation; never contains animation samples.</summary>
public sealed class LookaheadLayoutSnapshot
{
    internal LookaheadLayoutSnapshot(long version, Size viewport, Control entry, Visual coordinateReference,
        Dictionary<Control, LookaheadLayoutResult> results)
    {
        Version = version;
        Viewport = viewport;
        Entry = entry;
        CoordinateReference = coordinateReference;
        Results = new ReadOnlyDictionary<Control, LookaheadLayoutResult>(results);
    }

    public long Version { get; }
    public Size Viewport { get; }
    /// <summary>The control that owns this local target-layout domain.</summary>
    public Control Entry { get; }
    /// <summary>The visual relative to which every result bound is expressed.</summary>
    public Visual CoordinateReference { get; }
    public IReadOnlyDictionary<Control, LookaheadLayoutResult> Results { get; }
}

/// <summary>
/// Experimental independent target policy. Ordinary implementations must not measure/arrange live
/// controls or read DesiredSize/Bounds in these callbacks. LookaheadBoundary is the built-in
/// exception: it measures its direct native Child through the builder's native entry. Child slots
/// and bounds use the domain's coordinate reference.
/// </summary>
public interface ILookaheadLayoutNode
{
    IEnumerable<Control> LookaheadChildren { get; }
    /// <summary>Stable collection notification source; override when children are projected by an iterator.</summary>
    INotifyCollectionChanged? LookaheadChildrenChanged => LookaheadChildren as INotifyCollectionChanged;
    /// <summary>Additional target inputs. Common Layoutable geometry properties are observed automatically.</summary>
    IEnumerable<AvaloniaProperty> LookaheadProperties => [];
    Size MeasureLookahead(LookaheadLayoutBuilder builder, Size available);
    void ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds);
}

/// <summary>
/// Experimental notification for non-property target inputs or child collection replacement.
/// Raise even when actual layout is already dirty, and never for animation samples.
/// </summary>
public interface ILookaheadTargetInvalidationSource
{
    event EventHandler? TargetMeasureInvalidated;
}

/// <summary>
/// Evaluates independent layout policies. Ordinary Measure/Place paths do not call
/// Control.Measure/Arrange, create templates, or read DesiredSize/Bounds. The native child
/// entry is reserved for LookaheadBoundary during an active domain target build.
/// LayoutHelper operations on the independent path are pure geometry.
/// </summary>
public sealed class LookaheadLayoutBuilder
{
    internal LookaheadLayoutBuilder(LookaheadContext? root = null) => _context = root;
    private readonly LookaheadContext? _context;
    private readonly Dictionary<Control, LookaheadLayoutResult> _results = [];
    private readonly Dictionary<Control, object?> _layoutData = [];
    private readonly Dictionary<Control, (LookaheadPolicyPass Pass, LookaheadMeasureResult Result)> _policyResults = [];
    private Point _parentOrigin;
    private Control? _standaloneEntry;

    /// <summary>Store immutable policy data during target measure/place. Later generations must not mutate it.</summary>
    public void SetLayoutData(Control control, object? data)
    {
        _layoutData[control] = data;
        if (_results.TryGetValue(control, out var result)) _results[control] = result with { LayoutData = data };
    }

    public object? GetLayoutData(Control control) => _layoutData.GetValueOrDefault(control);

    public Size Measure(Control control, Size available)
    {
        _standaloneEntry ??= control;
        if (!control.IsVisible)
        {
            _results.Add(control, new(available, default, default, default));
            return default;
        }
        var margin = Margin(control);
        var constraints = LayoutHelper.ApplyLayoutConstraints(control, available.Deflate(margin));
        Size measured;
        if (control is ILookaheadLayoutNode node)
            measured = node.MeasureLookahead(this, constraints);
        else
        {
            if (control is Panel || control is LookaheadRoot ||
                !double.IsFinite(control.Width) || !double.IsFinite(control.Height))
                throw new NotSupportedException($"Lookahead cannot independently measure {control.GetType().Name} " +
                    $"'{control.Name}'. Use cooperative Lookahead controls / AnimatedContentSize, or an opaque leaf with explicit Width and Height.");
            // Fixed-size ordinary content is an opaque leaf: its descendants are not predicted.
            measured = constraints;
        }
        var hasLayoutFootprint = HasLayoutFootprint(control);
        var desired = LayoutHelper.ApplyLayoutConstraints(control, measured);
        if (control.UseLayoutRounding)
            desired = LayoutHelper.RoundLayoutSizeUp(desired, LayoutHelper.GetLayoutScale(control));
        desired = desired.Inflate(margin).Constrain(available);
        if (!hasLayoutFootprint) desired = default;
        _results.Add(control, new(constraints, measured, desired, default)
            { LayoutData = GetLayoutData(control), HasLayoutFootprint = hasLayoutFootprint });
        return desired;
    }

    public Size Desired(Control control) => _results[control].DesiredSize;

    internal LookaheadLayoutResult? ResultFor(Control control) => _results.GetValueOrDefault(control);

    /// <summary>Independent full-constraint target measurement. No live Measure/Arrange is called.</summary>
    public Size Measure(Control control, LookaheadConstraints constraints)
    {
        _standaloneEntry ??= control;
        if (control is not ILookaheadConstrainedLayoutNode node)
            throw new NotSupportedException($"{control.GetType().Name} does not implement ILookaheadConstrainedLayoutNode.");
        if (!control.IsVisible)
        {
            _results.Add(control, new(constraints.MaxSize, default, default, default) { FullMeasureConstraints = constraints });
            return default;
        }
        var margin = LookaheadMeasure.Margin(control);
        var effective = constraints.Deflate(margin).ApplyTo(control);
        var measured = node.MeasureLookahead(this, effective);
        var hasLayoutFootprint = HasLayoutFootprint(control);
        var desired = hasLayoutFootprint ? LookaheadMeasure.Desired(control, measured, effective, constraints, margin, hasLayoutFootprint: true) : default;
        _results.Add(control, new(effective.MaxSize, measured, desired, default)
            { FullMeasureConstraints = effective, LayoutData = GetLayoutData(control), HasLayoutFootprint = hasLayoutFootprint });
        return desired;
    }

    /// <summary>
    /// Measures a LookaheadBoundary's private content host with native Size semantics during a
    /// target build. The host and its Child remain outside the truncated target tree.
    /// </summary>
    internal Size MeasureNativeChild(LookaheadBoundary host, Control child, Size available)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(child);
        host.VerifyAccess();
        if (_context is null || !_context.IsBuildingTarget)
            throw new InvalidOperationException("Native boundary measurement requires an active lookahead domain target build.");
        if (!host.IsNativeContentHost(child) || !ReferenceEquals(child.GetVisualParent(), host))
            throw new InvalidOperationException("LookaheadBoundary can only natively measure its content host.");
        if (_results.ContainsKey(child))
            throw new InvalidOperationException("Native boundary content must remain outside the truncated target tree.");
        try { return host.MeasureNativeChild(child, available); }
        catch (Exception error) when (error is not NotSupportedException)
        {
            throw new InvalidOperationException(
                $"LookaheadBoundary native measure failed ({host.GetType().Name} / {child.GetType().Name}).", error);
        }
    }

    /// <summary>Explicit legacy boundary, matching LookaheadMeasure.MeasureChild on the actual pass.</summary>
    public Size MeasureChild(Control control, LookaheadConstraints constraints)
    {
        if (control is ILookaheadConstrainedLayoutNode) return Measure(control, constraints);
        if (constraints.HasMinimum)
            throw new NotSupportedException($"Cannot pass minimum constraints to legacy {control.GetType().Name}.");
        return Measure(control, constraints.MaxSize);
    }

    internal Size MeasurePolicy(Control owner, ILookaheadLayoutPolicy policy, LookaheadConstraints constraints)
    {
        var pass = new LookaheadPolicyPass(owner, policy, LookaheadLayoutPhase.Target,
            (child, childConstraints) =>
            {
                ResetPolicySubtree(child);
                return MeasureChild(child, childConstraints);
            },
            (child, slot) => Place(child, new Rect(
                new Point(slot.X + _parentOrigin.X, slot.Y + _parentOrigin.Y), slot.Size)),
            null);
        var result = pass.RunMeasure(constraints);
        SetLayoutData(owner, result.LayoutData);
        _policyResults[owner] = (pass, result);
        return result.Size;
    }

    internal void ArrangePolicy(Control owner, Size finalSize)
    {
        if (!_policyResults.TryGetValue(owner, out var state))
            throw new InvalidOperationException("Target placement requires the current policy measurement.");
        state.Pass.RunPlacement(state.Result, finalSize);
    }

    public void Place(Control control, Rect slot)
    {
        if (!control.IsVisible) return;
        var result = _results[control];
        var bounds = result.HasLayoutFootprint
            ? ResolveBounds(control, slot, result.DesiredSize, _parentOrigin, result.FullMeasureConstraints)
            : new Rect(_parentOrigin + new Vector(slot.X, slot.Y), default(Size));
        _results[control] = result with { Bounds = bounds };
        if (control is ILookaheadLayoutNode node)
        {
            var previousOrigin = _parentOrigin;
            _parentOrigin = bounds.TopLeft;
            try { node.ArrangeLookahead(this, bounds); }
            finally { _parentOrigin = previousOrigin; }
        }
    }

    private bool HasLayoutFootprint(Control control)
        => !_policyResults.TryGetValue(control, out var state) || state.Result.HasLayoutFootprint;

    private void ResetPolicySubtree(Control control)
    {
        if (control is ILookaheadLayoutNode node)
        {
            foreach (var child in node.LookaheadChildren.ToArray()) ResetPolicySubtree(child);
        }
        _results.Remove(control);
        _layoutData.Remove(control);
        _policyResults.Remove(control);
    }

    // Shared geometry for policies whose ArrangeOverride returns its supplied size.
    // Actual placement passes use the real DesiredSize; target passes use independent results.
    internal static Rect ResolveBounds(Control control, Rect slot, Size desiredSize, Point parentOrigin,
        LookaheadConstraints? constraints = null)
    {
        var margin = Margin(control);
        var available = slot.Size.Deflate(margin);
        var size = available;
        if (control.HorizontalAlignment != HorizontalAlignment.Stretch)
            size = size.WithWidth(Math.Min(size.Width, Math.Max(0, desiredSize.Width - margin.Left - margin.Right)));
        if (control.VerticalAlignment != VerticalAlignment.Stretch)
            size = size.WithHeight(Math.Min(size.Height, Math.Max(0, desiredSize.Height - margin.Top - margin.Bottom)));
        size = constraints?.Constrain(size) ?? LayoutHelper.ApplyLayoutConstraints(control, size);
        if (control.UseLayoutRounding)
        {
            size = LayoutHelper.RoundLayoutSizeUp(size, LayoutHelper.GetLayoutScale(control));
            if (constraints is { } full) size = full.Constrain(size);
            available = LayoutHelper.RoundLayoutSizeUp(available, LayoutHelper.GetLayoutScale(control));
        }
        var x = slot.X + control.Margin.Left;
        var y = slot.Y + control.Margin.Top;
        x += control.HorizontalAlignment switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Right => available.Width - size.Width,
            _ => (available.Width - size.Width) / 2
        };
        y += control.VerticalAlignment switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Bottom => available.Height - size.Height,
            _ => (available.Height - size.Height) / 2
        };
        var position = new Point(x, y);
        if (control.UseLayoutRounding)
        {
            // Avalonia rounds each child's local placement before accumulating ancestor
            // offsets. Rounding a root-space coordinate would be wrong below an unrounded parent.
            var local = position - parentOrigin;
            var rounded = LayoutHelper.RoundLayoutPoint(new Point(local.X, local.Y), LayoutHelper.GetLayoutScale(control));
            position = parentOrigin + new Vector(rounded.X, rounded.Y);
        }
        return new Rect(position, size);
    }

    internal LookaheadLayoutSnapshot Build(long version, Size viewport)
    {
        var entry = _context?.Owner ?? _standaloneEntry ??
            throw new InvalidOperationException("A lookahead snapshot requires at least one measured entry.");
        var reference = _context?.CoordinateReference ?? entry;
        var published = new Dictionary<Control, LookaheadLayoutResult>(_results);
        foreach (var state in _policyResults.Values)
        {
            foreach (var child in state.Pass.UnplacedMeasuredChildren())
                RemovePublishedSubtree(published, child);
        }
        return new(version, viewport, entry, reference,
            published);
    }

    private static void RemovePublishedSubtree(
        Dictionary<Control, LookaheadLayoutResult> published, Control control)
    {
        if (control is ILookaheadLayoutNode node)
        {
            foreach (var child in node.LookaheadChildren.ToArray())
                RemovePublishedSubtree(published, child);
        }
        published.Remove(control);
    }

    private static Thickness Margin(Control control) => control.UseLayoutRounding
        ? LayoutHelper.RoundLayoutThickness(control.Margin, LayoutHelper.GetLayoutScale(control)) : control.Margin;
}
