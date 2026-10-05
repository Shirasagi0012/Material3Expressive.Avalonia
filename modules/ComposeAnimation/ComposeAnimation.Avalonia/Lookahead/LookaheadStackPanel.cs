using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ComposeAnimation;

/// <summary>
/// Experimental non-virtualizing stack with separate target and actual layout.
/// Layout follows Avalonia StackPanel's unbounded stacking axis, not Compose Row/Column weights.
/// </summary>
public sealed class LookaheadStackPanel : Panel, ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost
{
    private readonly LookaheadEntry _entry;
    public LookaheadStackPanel() => _entry = new(this);
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<LookaheadStackPanel, Orientation>(nameof(Orientation), Orientation.Vertical);
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<LookaheadStackPanel, double>(nameof(Spacing),
            validate: value => double.IsFinite(value) && value >= 0);

    static LookaheadStackPanel() => AffectsMeasure<LookaheadStackPanel>(OrientationProperty, SpacingProperty);

    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Children;
    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties => [OrientationProperty, SpacingProperty];

    protected override Size MeasureOverride(Size availableSize) => MeasureStack(availableSize, (child, constraints) =>
    {
        return LookaheadMeasure.Measure(child, constraints);
    });

    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
        => MeasureStack(available, builder.Measure);

    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        => MeasureStack(constraints.MaxSize, (child, size) => builder.MeasureChild(child, LookaheadConstraints.AtMost(size)));
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints)
        => MeasureStack(constraints.MaxSize, (child, size) => LookaheadMeasure.MeasureChild(child, LookaheadConstraints.AtMost(size)));

    protected override Size MeasureCore(Size availableSize)
        => _entry.MeasureCore(availableSize, () => base.MeasureCore(availableSize));

    protected override void ArrangeCore(Rect finalRect)
    {
        if (_entry.TryArrangeCore(finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty) _entry?.VisualParentChanged();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _entry.Detach();
        base.OnDetachedFromVisualTree(e);
    }

    private Size MeasureStack(Size available, Func<Control, Size, Size> measure)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var constraints = horizontal ? available.WithWidth(double.PositiveInfinity)
            : available.WithHeight(double.PositiveInfinity);
        double along = 0, across = 0;
        var count = 0;
        foreach (var child in Children)
        {
            var size = measure(child, constraints);
            if (!child.IsVisible) continue;
            along += horizontal ? size.Width : size.Height;
            across = Math.Max(across, horizontal ? size.Height : size.Width);
            count++;
        }
        along += Math.Max(0, count - 1) * Spacing;
        return horizontal ? new Size(along, across) : new Size(across, along);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        PlaceStack(new Rect(finalSize), child => child.DesiredSize,
            (child, rect) => LookaheadPlacement.ArrangeChild(this, child, rect));
        return finalSize;
    }

    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
        => PlaceStack(bounds, builder.Desired, builder.Place);

    private void PlaceStack(Rect bounds, Func<Control, Size> desired, Action<Control, Rect> place)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        double offset = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var size = desired(child);
            var slot = horizontal
                ? new Rect(bounds.X + offset, bounds.Y, size.Width, Math.Max(bounds.Height, size.Height))
                : new Rect(bounds.X, bounds.Y + offset, Math.Max(bounds.Width, size.Width), size.Height);
            place(child, slot);
            offset += (horizontal ? size.Width : size.Height) + Spacing;
        }
    }
}
