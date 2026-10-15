using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace ComposeAnimation;

/// <summary>
/// Experimental target-tree boundary. The boundary participates in lookahead measure and
/// placement; its ordinary Child is measured and arranged with native Avalonia using the
/// destination constraints and slot. Content is not scaled, frozen, or copied.
/// </summary>
/// <remarks>
/// When registered in a domain it is the target-tree endpoint; otherwise it remains an ordinary
/// single-child control (Padding and ClipToBounds still apply; ContentAlignment is unused).
/// Its private content host cuts off the outer lookahead context while preserving ordinary inherited
/// properties. Automatic entries in the Child subtree may therefore create an independent domain.
/// </remarks>
public sealed class LookaheadBoundary : Control,
    ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost, ILookaheadTargetInvalidationSource
{
    public static readonly StyledProperty<Control?> ChildProperty =
        Decorator.ChildProperty.AddOwner<LookaheadBoundary>();
    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<LookaheadBoundary>();
    public static readonly StyledProperty<VisibilityAlignment> ContentAlignmentProperty =
        AvaloniaProperty.Register<LookaheadBoundary, VisibilityAlignment>(
            nameof(ContentAlignment), VisibilityAlignment.TopStart);

    [Content]
    public Control? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    /// <summary>
    /// Aligns the destination content slot inside the current animated content area.
    /// Does not stretch or scale the slot. Default TopStart.
    /// </summary>
    public VisibilityAlignment ContentAlignment
    {
        get => GetValue(ContentAlignmentProperty);
        set => SetValue(ContentAlignmentProperty, value);
    }

    public event EventHandler? TargetMeasureInvalidated;

    internal long InputRevision { get; private set; }
    private int _nativeMeasureDepth;
    private readonly BoundaryContentHost _contentHost;

    internal sealed record TargetRecord(
        long Revision, Control? Child, Size ContentConstraints, Size ChildDesired, Size? ContentSlot);

    static LookaheadBoundary()
    {
        ClipToBoundsProperty.OverrideDefaultValue<LookaheadBoundary>(true);
        AffectsMeasure<LookaheadBoundary>(ChildProperty, PaddingProperty);
        AffectsArrange<LookaheadBoundary>(ContentAlignmentProperty, FlowDirectionProperty);
    }

    public LookaheadBoundary()
    {
        _contentHost = new BoundaryContentHost(this);
        ((ISetLogicalParent)_contentHost).SetParent(this);
        LookaheadEnvironment.SetContext(_contentHost, null);
        VisualChildren.Add(_contentHost);
    }

    /// <summary>
    /// Marks destination inputs that ordinary layout invalidation cannot observe.
    /// Property, Child, Padding, and descendant measure changes are handled automatically.
    /// </summary>
    public void InvalidateTargetMeasure()
    {
        VerifyAccess();
        BumpRevision();
        TargetMeasureInvalidated?.Invoke(this, EventArgs.Empty);
    }

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => [];
    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
        => MeasureTarget(builder, LookaheadConstraints.AtMost(available));
    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        => MeasureTarget(builder, constraints);
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints)
        => MeasureCurrent(constraints);

    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
    {
        var slot = bounds.Size.Deflate(EffectivePadding);
        if (builder.GetLayoutData(this) is TargetRecord record)
            builder.SetLayoutData(this, record with { ContentSlot = slot });
    }

    protected override Size MeasureOverride(Size availableSize)
        => MeasureCurrent(LookaheadConstraints.AtMost(availableSize));

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is null) return finalSize;
        if (LookaheadScope.FindParticipatingContext(this) is null)
            return LayoutHelper.ArrangeChild(_contentHost, finalSize, Padding);
        var target = LookaheadRoot.TargetFor(this) ??
            throw new InvalidOperationException("LookaheadBoundary requires a complete target layout.");
        if (target.LayoutData is not TargetRecord { ContentSlot: { } slot })
            throw new InvalidOperationException("LookaheadBoundary requires a target content slot.");
        var padding = EffectivePadding;
        var current = finalSize.Deflate(padding);
        var aligned = EnterExitMotion.Align(slot, current, ContentAlignment, FlowDirection);
        LookaheadPlacement.ArrangeNativeChild(this, _contentHost,
            new Rect(aligned.X + padding.Left, aligned.Y + padding.Top, slot.Width, slot.Height));
        return finalSize;
    }

    protected override Size MeasureCore(Size availableSize)
        => LookaheadMeasure.TryMeasureCore(this, availableSize, out var desired)
            ? desired : base.MeasureCore(availableSize);

    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadMeasure.TryArrangeCore(this, finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LookaheadMeasure.Forget(this);
        if (Child is { } child) LookaheadMeasure.Forget(child);
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChildProperty)
        {
            if (change.OldValue is Control oldChild)
            {
                oldChild.PropertyChanged -= OnChildPropertyChanged;
                _contentHost.Child = null;
                ((ISetInheritanceParent)oldChild).SetParent(oldChild.Parent);
                ((ISetLogicalParent)oldChild).SetParent(null);
                LogicalChildren.Remove(oldChild);
                LookaheadMeasure.Forget(oldChild);
            }
            if (change.NewValue is Control newChild)
            {
                ((ISetLogicalParent)newChild).SetParent(this);
                LogicalChildren.Add(newChild);
                ((ISetInheritanceParent)newChild).SetParent(_contentHost);
                _contentHost.Child = newChild;
                newChild.PropertyChanged += OnChildPropertyChanged;
            }
            BumpRevision();
        }
        else if (change.Property == PaddingProperty)
            BumpRevision();
    }

    internal bool IsNativeContentHost(Control child) => ReferenceEquals(child, _contentHost);

    internal Size MeasureNativeChild(Control child, Size available)
    {
        _nativeMeasureDepth++;
        try
        {
            // A dirty inner domain can have stable outward DesiredSize, leaving the host's
            // native cache valid. Revisit it at the stored destination constraints.
            if (Child?.IsMeasureValid == false) child.InvalidateMeasure();
            LookaheadMeasure.Measure(child, available);
            return child.DesiredSize;
        }
        catch
        {
            child.InvalidateMeasure();
            Child?.InvalidateMeasure();
            throw;
        }
        finally { _nativeMeasureDepth--; }
    }

    internal bool IsNativeContentStale(LookaheadLayoutSnapshot snapshot)
    {
        if (!snapshot.Results.TryGetValue(this, out var result) || result.LayoutData is not TargetRecord record)
            return true;
        if (record.Revision != InputRevision || !ReferenceEquals(record.Child, Child))
            return true;
        if (Child is not { } child) return false;
        if (!child.IsMeasureValid)
        {
            // An inner approach pass is not a new outer destination. Recheck native inputs
            // at the final constraints and rebuild only if their outward geometry changed.
            // Real content updates still get measured, including ordinary native subtrees.
            MeasureNativeChild(_contentHost, record.ContentConstraints);
            if (record.Revision != InputRevision || !ReferenceEquals(record.Child, Child)) return true;
        }
        return child.IsVisible && child.DesiredSize != record.ChildDesired;
    }

    private Size MeasureTarget(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
    {
        var padding = EffectivePadding;
        var contentConstraints = constraints.Deflate(padding).MaxSize;
        var childDesired = default(Size);
        if (Child is { IsVisible: true } child)
        {
            builder.MeasureNativeChild(this, _contentHost, contentConstraints);
            childDesired = child.DesiredSize;
        }
        var measured = childDesired.Inflate(padding);
        builder.SetLayoutData(this, new TargetRecord(InputRevision, Child, contentConstraints, childDesired, null));
        return measured;
    }

    private Size MeasureCurrent(LookaheadConstraints incoming)
    {
        var context = LookaheadScope.FindParticipatingContext(this);
        if (context is null)
            return LayoutHelper.MeasureChild(_contentHost, incoming.MaxSize, Padding);
        var target = LookaheadRoot.TargetFor(this) ??
            throw new InvalidOperationException("LookaheadBoundary requires a complete target layout.");
        if (target.LayoutData is not TargetRecord record)
            throw new InvalidOperationException("LookaheadBoundary requires complete target layout data.");
        if (Child is { } child)
        {
            try { MeasureNativeChild(_contentHost, record.ContentConstraints); }
            catch (Exception error) when (error is not NotSupportedException)
            {
                InvalidateMeasure();
                InvalidateArrange();
                throw new InvalidOperationException(
                    $"LookaheadBoundary native measure failed ({GetType().Name} / {child.GetType().Name}).", error);
            }
            if (record.Revision != InputRevision || !ReferenceEquals(record.Child, child) ||
                child.IsVisible && child.DesiredSize != record.ChildDesired)
            {
                if (!ReferenceEquals(record.Child, child) || child.DesiredSize != record.ChildDesired)
                    BumpRevision();
                context.RequestTargetRebuild();
            }
        }
        else if (record.Child is not null || record.Revision != InputRevision)
            context.RequestTargetRebuild();
        return incoming.Constrain(target.MeasuredSize);
    }

    private void OnChildPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Layoutable.DesiredSizeProperty || _nativeMeasureDepth > 0) return;
        NativeContentInvalidated();
    }

    private void NativeContentInvalidated()
    {
        if (_nativeMeasureDepth > 0) return;
        BumpRevision();
        InvalidateMeasure();
        InvalidateArrange();
        LookaheadScope.FindParticipatingContext(this)?.RequestTargetRebuild();
    }

    private void BumpRevision() => InputRevision++;

    private Thickness EffectivePadding => UseLayoutRounding
        ? LayoutHelper.RoundLayoutThickness(Padding, LayoutHelper.GetLayoutScale(this)) : Padding;

    private sealed class BoundaryContentHost(LookaheadBoundary owner) : Control
    {
        private Control? _child;

        internal Control? Child
        {
            get => _child;
            set
            {
                if (ReferenceEquals(_child, value)) return;
                if (_child is not null) VisualChildren.Remove(_child);
                _child = value;
                if (_child is not null) VisualChildren.Add(_child);
                InvalidateMeasure();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Child?.Measure(availableSize);
            return Child?.DesiredSize ?? default;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child is { } child) LayoutHelper.ArrangeChild(child, finalSize, default);
            return finalSize;
        }

        protected override void OnMeasureInvalidated()
        {
            base.OnMeasureInvalidated();
            owner.NativeContentInvalidated();
        }
    }
}
