using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

// Column's unweighted children are measured in declaration order against the
// remaining finite height. StackPanel's infinite main axis prevents a scroll body.
internal sealed class BottomSheetContentLayout : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0d; var height = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - height)));
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var y = 0d;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return finalSize;
    }
}

internal sealed class BottomSheetLayout : Panel
{
    internal BottomSheet? Owner { get; set; }
    private Size _sheetSize;
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0 || Owner is null) return default;
        if (!double.IsFinite(availableSize.Height) || !double.IsFinite(availableSize.Width))
            throw new InvalidOperationException("BottomSheet requires a finite-width, finite-height host.");
        var scale = LayoutHelper.GetLayoutScale(this);
        var maximum = Owner.SheetMaxWidth;
        var width = double.IsNaN(maximum) || double.IsPositiveInfinity(maximum) ? availableSize.Width
            : Math.Min(availableSize.Width, MathUtils.DipToPx((float)maximum, (float)scale) / scale);
        Children[0].MinHeight = Owner.StandardPeekHeight is { } peek ? MathUtils.DipToPx((float)peek, (float)scale) / scale : 0;
        if (Children[0] is MaterialSurface { Child: Border padding } && padding.Child is Control column)
        {
            var requested = Owner.WindowInsets; var inherited = Owner.GetValue(MaterialWindowInsets.ConsumedPaddingProperty);
            double Round(double d) => MathUtils.DipToPx((float)d, (float)scale) / scale;
            var rtl = FlowDirection == FlowDirection.RightToLeft;
            var physical = new Thickness(Math.Max(0, Round(rtl ? requested.Right : requested.Left) - inherited.Left),
                Math.Max(0, Round(requested.Top) - inherited.Top), Math.Max(0, Round(rtl ? requested.Left : requested.Right) - inherited.Right), Math.Max(0, Round(requested.Bottom) - inherited.Bottom));
            padding.Padding = physical;
            MaterialWindowInsets.SetConsumePadding(column, rtl ? new Thickness(physical.Right, physical.Top, physical.Left, physical.Bottom) : physical);
        }
        Children[0].Measure(new Size(width, availableSize.Height));
        _sheetSize = new Size(width, Children[0].DesiredSize.Height);
        return availableSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0 || Owner is null) return finalSize;
        var scale = LayoutHelper.GetLayoutScale(this);
        Owner.State.UpdateAnchors(finalSize.Height, _sheetSize.Height, scale, Owner.StandardPeekHeight);
        var offset = Owner.State.OffsetPx;
        var surface = Children[0];
        surface.Arrange(new Rect((finalSize.Width - _sheetSize.Width) / 2, MathUtils.RoundToInt(offset) / scale, _sheetSize.Width, _sheetSize.Height));
        // Keep graphicsLayer's Float pixel arithmetic and operation order before adapting
        // the resulting scale to Avalonia's Double transform matrix.
        var overflow = Math.Max(0, Owner.State.MinimumAnchor - offset);
        var surfaceHeight = (float)(_sheetSize.Height * scale);
        surface.RenderTransformOrigin = new RelativePoint(.5, 0, RelativeUnit.Relative);
        surface.RenderTransform = new ScaleTransform(1, overflow > 0 && surfaceHeight > 0 ? (surfaceHeight + overflow) / surfaceHeight : 1);
        if (surface is MaterialSurface { Child: Border { Child: Control column } })
        {
            column.RenderTransformOrigin = new RelativePoint(.5, 0, RelativeUnit.Relative);
            var contentHeight = (float)(column.Bounds.Height * scale);
            column.RenderTransform = new ScaleTransform(1, overflow > 0 && contentHeight > 0 ? 1f / ((contentHeight + overflow) / contentHeight) : 1);
        }
        return finalSize;
    }
}
