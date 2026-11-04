using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class BottomSheetScaffoldLayout : Panel
{
    internal BottomSheetScaffold? Owner { get; set; }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 4 || Owner is null) return default;
        if (!double.IsFinite(availableSize.Width) || !double.IsFinite(availableSize.Height))
            throw new InvalidOperationException("BottomSheetScaffold requires a finite-width, finite-height host.");
        var scale = LayoutHelper.GetLayoutScale(this);
        Owner.UpdateContentPadding(new Thickness(0, 0, 0, MathUtils.DipToPx((float)Owner.SheetPeekHeight, (float)scale) / scale));
        Children[2].Measure(availableSize);
        Children[1].Measure(availableSize);
        Children[0].Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - Children[1].DesiredSize.Height)));
        Children[3].Measure(availableSize);
        return availableSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 4 || Owner is null) return finalSize;
        var topHeight = Children[1].DesiredSize.Height;
        Children[0].Arrange(new Rect(0, topHeight, finalSize.Width, Math.Max(0, finalSize.Height - topHeight)));
        Children[1].Arrange(new Rect(0, 0, finalSize.Width, topHeight));
        Children[2].Arrange(new Rect(finalSize));
        var snack = Children[3]; var size = snack.DesiredSize;
        var scale = LayoutHelper.GetLayoutScale(this);
        var state = Owner.State.BottomSheetState;
        var y = state.CurrentValue == SheetValue.PartiallyExpanded ? MathUtils.RoundToInt(state.OffsetPx) / scale : finalSize.Height;
        // Compose's positive integer division truncates the centered x coordinate.
        var x = Math.Floor((finalSize.Width - size.Width) * scale / 2) / scale;
        snack.Arrange(new Rect(x, y - size.Height, size.Width, size.Height));
        return finalSize;
    }
}
