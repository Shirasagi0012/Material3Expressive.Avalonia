using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ComposeAnimation;

/// <summary>
/// Experimental finite cooperative scroll viewport. Measures content unbounded on enabled axes,
/// clips it, and contributes -Offset as a motion frame. Supports wheel and programmatic scrolling;
/// it is not a ScrollViewer template adapter and provides no virtualization or scroll anchoring.
/// </summary>
public sealed class LookaheadScrollViewport : Decorator, ILookaheadConstrainedLayoutNode, ILookaheadMotionFrameProvider
{
    private readonly LookaheadEntry _entry;
    public LookaheadScrollViewport() => _entry = new(this);
    public static readonly StyledProperty<Vector> OffsetProperty = AvaloniaProperty.Register<LookaheadScrollViewport, Vector>(
        nameof(Offset), validate: value => double.IsFinite(value.X) && double.IsFinite(value.Y),
        coerce: (owner, value) => ((LookaheadScrollViewport)owner).Clamp(value));
    public static readonly StyledProperty<bool> CanHorizontallyScrollProperty =
        AvaloniaProperty.Register<LookaheadScrollViewport, bool>(nameof(CanHorizontallyScroll));
    public static readonly StyledProperty<bool> CanVerticallyScrollProperty =
        AvaloniaProperty.Register<LookaheadScrollViewport, bool>(nameof(CanVerticallyScroll), true);
    public static readonly DirectProperty<LookaheadScrollViewport, Size> ExtentProperty =
        AvaloniaProperty.RegisterDirect<LookaheadScrollViewport, Size>(nameof(Extent), owner => owner.Extent);
    public static readonly DirectProperty<LookaheadScrollViewport, Size> ViewportProperty =
        AvaloniaProperty.RegisterDirect<LookaheadScrollViewport, Size>(nameof(Viewport), owner => owner.Viewport);

    public Vector Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }
    public bool CanHorizontallyScroll { get => GetValue(CanHorizontallyScrollProperty); set => SetValue(CanHorizontallyScrollProperty, value); }
    public bool CanVerticallyScroll { get => GetValue(CanVerticallyScrollProperty); set => SetValue(CanVerticallyScrollProperty, value); }
    public Size Extent => _extent;
    public Size Viewport => _viewport;
    public event EventHandler? MotionFrameChanged;
    private Size _extent;
    private Size _viewport;
    private Size _contentSize;
    private bool _hasViewport;

    static LookaheadScrollViewport()
    {
        ClipToBoundsProperty.OverrideDefaultValue<LookaheadScrollViewport>(true);
        AffectsMeasure<LookaheadScrollViewport>(CanHorizontallyScrollProperty, CanVerticallyScrollProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty) _entry?.VisualParentChanged();
        if (change.Property == OffsetProperty)
        {
            InvalidateArrange();
            MotionFrameChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Vector GetMotionFrameOffset(Control child)
    {
        if (child != Child) throw new InvalidOperationException("Motion frames apply only to this viewport's direct Child.");
        return -Offset;
    }

    private Vector Clamp(Vector offset) => !_hasViewport ? new(Math.Max(0, offset.X), Math.Max(0, offset.Y)) : new(
        CanHorizontallyScroll ? Math.Clamp(offset.X, 0, Math.Max(0, Extent.Width - Viewport.Width)) : 0,
        CanVerticallyScroll ? Math.Clamp(offset.Y, 0, Math.Max(0, Extent.Height - Viewport.Height)) : 0);

    private Size MeasureContent(Size available, Func<Control, LookaheadConstraints, Size> measure)
    {
        if (Padding != default) throw new NotSupportedException("LookaheadScrollViewport does not support Padding; use an inner layout.");
        if (!double.IsFinite(available.Width) || !double.IsFinite(available.Height))
            throw new NotSupportedException("LookaheadScrollViewport requires a finite viewport. Set a size on unconstrained axes.");
        var childConstraints = LookaheadConstraints.AtMost(new Size(
            CanHorizontallyScroll ? double.PositiveInfinity : available.Width,
            CanVerticallyScroll ? double.PositiveInfinity : available.Height));
        return Child is { } child ? measure(child, childConstraints) : default;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Resolve an initially requested offset before descendant animations initialize. Otherwise
        // they would first snap to an out-of-range offset and animate the subsequent coercion.
        if (!_hasViewport && LookaheadRoot.TargetFor(this)?.LayoutData is Size targetExtent)
        {
            _contentSize = targetExtent;
            UpdateMetrics(availableSize);
        }
        _contentSize = MeasureContent(availableSize, LookaheadMeasure.MeasureChild);
        UpdateMetrics(availableSize);
        return availableSize;
    }

    private void UpdateMetrics(Size viewport)
    {
        SetAndRaise(ViewportProperty, ref _viewport, viewport);
        SetAndRaise(ExtentProperty, ref _extent, new Size(Math.Max(viewport.Width, _contentSize.Width), Math.Max(viewport.Height, _contentSize.Height)));
        _hasViewport = true;
        CoerceValue(OffsetProperty);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateMetrics(finalSize);
        if (Child is { } child) LookaheadPlacement.ArrangeChild(this, child, new Rect(Extent));
        return finalSize;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Handled) return;
        var before = Offset;
        SetCurrentValue(OffsetProperty, Clamp(before - new Vector(
            CanHorizontallyScroll ? e.Delta.X * 50 : 0, CanVerticallyScroll ? e.Delta.Y * 50 : 0)));
        if (Offset != before) e.Handled = true; // At an edge, let a containing viewport scroll.
    }

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Child is { } child ? [child] : [];
    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties => [CanHorizontallyScrollProperty, CanVerticallyScrollProperty];
    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
    {
        var content = MeasureContent(available, builder.MeasureChild);
        builder.SetLayoutData(this, new Size(Math.Max(available.Width, content.Width), Math.Max(available.Height, content.Height)));
        return available;
    }
    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        => ((ILookaheadLayoutNode)this).MeasureLookahead(builder, constraints.MaxSize);
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints) => MeasureOverride(constraints.MaxSize);
    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
    {
        if (Child is { } child)
        {
            var extent = (Size)builder.GetLayoutData(this)!;
            builder.Place(child, new Rect(bounds.TopLeft, new Size(Math.Max(bounds.Width, extent.Width), Math.Max(bounds.Height, extent.Height))));
        }
    }

    protected override Size MeasureCore(Size availableSize)
        => _entry.MeasureCore(availableSize, () => base.MeasureCore(availableSize));
    protected override void ArrangeCore(Rect finalRect)
    {
        if (_entry.TryArrangeCore(finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _entry.Detach();
        _hasViewport = false;
        base.OnDetachedFromVisualTree(e);
    }
}
