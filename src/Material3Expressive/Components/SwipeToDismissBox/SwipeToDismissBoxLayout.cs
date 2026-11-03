using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class SwipeToDismissBoxLayout : Panel
{
    internal SwipeToDismissBox? Owner { get; set; }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        Children[1].Measure(availableSize);
        // matchParentSize does not participate in the Box's desired size.
        Children[0].Measure(Children[1].DesiredSize);
        return Children[1].DesiredSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2 || Owner is null) return finalSize;
        var scale = LayoutHelper.GetLayoutScale(this);
        Owner.State.UpdateAnchors(finalSize.Width, scale, Owner.EnableDismissFromStartToEnd, Owner.EnableDismissFromEndToStart);
        var offset = Owner.State.OffsetPx;
        if (float.IsNaN(offset))
            throw new InvalidOperationException($"Swipe anchors have no offset for {Owner.State.TargetValue}.");
        // DraggableAnchorsNode reverses the physical offset before roundToInt. Avalonia then
        // mirrors the logical placement, so reverse that integer once more for its input.
        var x = (FlowDirection == FlowDirection.RightToLeft ? -MathUtils.RoundToInt(-offset) : MathUtils.RoundToInt(offset)) / scale;
        Children[0].Arrange(new Rect(finalSize));
        // The background remains fixed; content placement does not clip the translated row.
        Children[1].Arrange(new Rect(x, 0, finalSize.Width, finalSize.Height));
        return finalSize;
    }
}
