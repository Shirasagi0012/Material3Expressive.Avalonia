using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Compose's FAB Box uses Alignment.Center (roundToInt), including odd pixel sizes
// during the toolbar transition. Native ContentPresenter uses a fractional center.
internal sealed class FloatingToolbarFabContentLayout : Decorator
{
    protected override Size MeasureOverride(Size available)
    {
        Child?.Measure(available);
        return Child?.DesiredSize ?? default;
    }

    protected override Size ArrangeOverride(Size final)
    {
        var size = Child?.DesiredSize ?? default;
        var density = LayoutHelper.GetLayoutScale(this);
        Child?.Arrange(new Rect(
            MathUtils.RoundToInt((float)((final.Width - size.Width) * density / 2)) / density,
            MathUtils.RoundToInt((float)((final.Height - size.Height) * density / 2)) / density,
            size.Width, size.Height));
        return final;
    }
}
