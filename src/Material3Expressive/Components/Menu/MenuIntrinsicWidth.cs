using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Components;

// Menu.kt: Column.width(IntrinsicSize.Max). Avalonia has no separate intrinsic
// query: measure without a width bound, so fillMaxWidth content (notably
// Divider) cannot make the popup request the entire screen/window width.
internal sealed class MenuIntrinsicWidth : Decorator
{
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child) return default;
        child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        // Retain the unbounded measurement for content-driven growth. Only a
        // tighter caller constraint requires remeasurement (e.g. MaxWidth=80).
        if (child.DesiredSize.Width > availableSize.Width)
            child.Measure(availableSize);
        return child.DesiredSize.Constrain(availableSize);
    }
}
