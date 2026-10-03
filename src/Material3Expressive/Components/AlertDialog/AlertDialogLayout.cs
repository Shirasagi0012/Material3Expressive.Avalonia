using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Components;

// sizeIn is inside the caller's dimensions. Explicit Width/MinWidth/MaxWidth replace
// the corresponding default, rather than forcing a caller's 100 DIP width back to 280.
internal sealed class BasicAlertDialogLayout : DialogLayout
{
    protected override bool BypassFlowDirectionPolicies => true;
    protected override Size MeasureOverride(Size availableSize)
    {
        var maximum = double.IsNaN(Width) && double.IsPositiveInfinity(MaxWidth) ? 560 : availableSize.Width;
        var size = base.MeasureOverride(new Size(Math.Min(maximum, availableSize.Width), availableSize.Height));
        var minimum = double.IsNaN(Width) && MinWidth == 0 ? 280 : MinWidth;
        return new Size(Math.Max(size.Width, Math.Min(minimum, Math.Min(maximum, availableSize.Width))), size.Height);
    }
}

// Column: unweighted icon/title/buttons are measured first. Supporting text uses
// weight(1, fill=false), so it receives remaining height but never stretches to fill it.
internal sealed class AlertDialogColumn : Panel
{
    // Alignment uses physical coordinates after resolving Compose start/end. Do not
    // mirror the entire arrangement again; slot content retains native RTL policies.
    protected override bool BypassFlowDirectionPolicies => true;
    protected override Size MeasureOverride(Size availableSize)
    {
        double height = 0, width = 0;
        foreach (var child in Children.Where(c => c.Name != "PART_Text"))
        {
            child.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - height)));
            height += child.DesiredSize.Height;
            width = Math.Max(width, child.DesiredSize.Width);
        }
        var text = Children.FirstOrDefault(c => c.Name == "PART_Text");
        if (text is not null)
        {
            text.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - height)));
            height += text.DesiredSize.Height;
            width = Math.Max(width, text.DesiredSize.Width);
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        foreach (var child in Children)
        {
            var height = child.DesiredSize.Height;
            child.Arrange(new Rect(0, y, finalSize.Width, height));
            y += height;
        }
        return finalSize;
    }
}

// AlertDialogFlowRow flips only the arrangement direction: confirm comes first on
// separate lines and last horizontally in LTR. Child content keeps the original RTL.
internal sealed class AlertDialogButtons : Panel
{
    protected override bool BypassFlowDirectionPolicies => true;
    private bool _stacked;
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(availableSize);
        var children = Children.Where(c => c.IsVisible).ToArray();
        var width = children.Sum(c => c.DesiredSize.Width) + Math.Max(0, children.Length - 1) * 8;
        _stacked = width > availableSize.Width;
        // Default LocalMinimumInteractiveComponentSize (48) - ButtonDefaults.MinHeight
        // (40) consumes the full 8 DIP cross-axis spacing, leaving zero.
        return _stacked
            ? new Size(children.Max(c => c.DesiredSize.Width), children.Sum(c => c.DesiredSize.Height))
            : new Size(width, children.Max(c => c.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rtl = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft;
        double x = rtl ? 0 : finalSize.Width, y = 0;
        foreach (var child in Children.Where(c => c.IsVisible))
        {
            var size = child.DesiredSize;
            var left = _stacked ? (rtl ? 0 : finalSize.Width - size.Width) : (rtl ? x : x - size.Width);
            child.Arrange(new Rect(left, y, size.Width, size.Height));
            if (_stacked) y += size.Height;
            else x += (rtl ? 1 : -1) * (size.Width + 8);
        }
        return finalSize;
    }
}
