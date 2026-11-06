using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Switch's wrapContentSize(Center).requiredSize(52.dp, 32.dp), without the interactive slot.
internal sealed class SwitchDisplayLayout : Decorator
{
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(new Size(52, 32));
        return new Size(52, 32);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        Child?.Arrange(new Rect(MathUtils.RoundToInt((finalSize.Width - 52) * scale / 2) / scale,
            MathUtils.RoundToInt((finalSize.Height - 32) * scale / 2) / scale, 52, 32));
        return finalSize;
    }
}
