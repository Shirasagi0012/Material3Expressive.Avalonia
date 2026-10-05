using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;

namespace ComposeAnimation;

// Transparent default-template bridges. The target tree projects directly to user content;
// the actual tree still measures and arranges every real wrapper with the full constraints.
internal abstract class SharedLayoutControl : Control, ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost
{
    protected abstract Control? LayoutChild { get; }
    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => LayoutChild is { } child ? [child] : [];
    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
        => LayoutChild is { } child ? builder.Measure(child, available) : default;
    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        => LayoutChild is { } child ? builder.Measure(child, constraints) : constraints.Constrain(default(Size));
    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
    { if (LayoutChild is { } child) builder.Place(child, bounds); }
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints)
        => LayoutChild is { } child ? LookaheadMeasure.Measure(child, constraints) : constraints.Constrain(default(Size));
    protected override Size MeasureOverride(Size availableSize)
        => LayoutChild is { } child ? LookaheadMeasure.Measure(child, availableSize) : default;
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (LayoutChild is { } child) LookaheadPlacement.ArrangeChild(this, child, new Rect(finalSize));
        return finalSize;
    }
    protected override Size MeasureCore(Size availableSize)
        => LookaheadMeasure.TryMeasureCore(this, availableSize, out var desired) ? desired : base.MeasureCore(availableSize);
    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadMeasure.TryArrangeCore(this, finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { LookaheadMeasure.Forget(this); base.OnDetachedFromVisualTree(e); }
}

internal sealed class SharedContentPresenter : ContentPresenter, ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost
{
    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Child is { } child ? [child] : [];
    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
        => Child is { } child ? builder.Measure(child, available) : default;
    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        => Child is { } child ? builder.Measure(child, constraints) : constraints.Constrain(default(Size));
    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
    { if (Child is { } child) builder.Place(child, bounds); }
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints)
    {
        UpdateChild();
        return Child is { } child ? LookaheadMeasure.Measure(child, constraints) : constraints.Constrain(default(Size));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (LookaheadMeasure.ActualConstraints(this) is null) return base.ArrangeOverride(finalSize);
        if (Child is { } child) LookaheadPlacement.ArrangeChild(this, child, new Rect(finalSize));
        return finalSize;
    }
    protected override Size MeasureCore(Size availableSize)
        => LookaheadMeasure.TryMeasureCore(this, availableSize, out var desired) ? desired : base.MeasureCore(availableSize);
    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadMeasure.TryArrangeCore(this, finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { LookaheadMeasure.Forget(this); base.OnDetachedFromVisualTree(e); }
}
