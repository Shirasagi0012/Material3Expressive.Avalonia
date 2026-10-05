using Avalonia;
using Avalonia.Controls;

namespace ComposeAnimation;

/// <summary>
/// Experimental stacking panel with separate target and actual layout.
/// Children share the same available constraints and content slot; desired size is the
/// per-axis maximum. Child Margin and Alignment are applied by the builder and placement helper.
/// Layout follows Avalonia Panel stacking, not Compose Box contentAlignment.
/// </summary>
public sealed class LookaheadBox : Panel, ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost
{
    private readonly LookaheadEntry _entry;
    public LookaheadBox() => _entry = new(this);
    /// <summary>Whether full-constraint measurement passes its minima to children (otherwise only the box consumes them).
    /// Has no effect on the legacy Size path. Legacy children cannot receive positive minima.</summary>
    public static readonly StyledProperty<bool> PropagateMinConstraintsProperty =
        AvaloniaProperty.Register<LookaheadBox, bool>(nameof(PropagateMinConstraints));
    public bool PropagateMinConstraints
    {
        get => GetValue(PropagateMinConstraintsProperty);
        set => SetValue(PropagateMinConstraintsProperty, value);
    }
    static LookaheadBox() => AffectsMeasure<LookaheadBox>(PropagateMinConstraintsProperty);
    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Children;
    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties => [PropagateMinConstraintsProperty];

    protected override Size MeasureOverride(Size availableSize) => MeasureBox(availableSize, (child, constraints) =>
    {
        return LookaheadMeasure.Measure(child, constraints);
    });

    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
        => MeasureBox(available, builder.Measure);

    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        => MeasureConstrainedBox(constraints, builder.MeasureChild);
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints)
        => MeasureConstrainedBox(constraints, LookaheadMeasure.MeasureChild);

    private Size MeasureConstrainedBox(LookaheadConstraints constraints, Func<Control, LookaheadConstraints, Size> measure)
    {
        var childConstraints = PropagateMinConstraints ? constraints : constraints.Loosen();
        return MeasureBox(childConstraints.MaxSize, (child, _) => measure(child, childConstraints));
    }

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

    private Size MeasureBox(Size available, Func<Control, Size, Size> measure)
    {
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            var size = measure(child, available);
            if (!child.IsVisible) continue;
            width = Math.Max(width, size.Width);
            height = Math.Max(height, size.Height);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        PlaceBox(new Rect(finalSize), (child, rect) => LookaheadPlacement.ArrangeChild(this, child, rect));
        return finalSize;
    }

    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
        => PlaceBox(bounds, builder.Place);

    private void PlaceBox(Rect slot, Action<Control, Rect> place)
    {
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            place(child, slot);
        }
    }
}
