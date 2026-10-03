using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Switch.kt: minimumInteractiveComponentSize -> wrapContentSize(Center) -> requiredSize(52,32).
// Content / OnContent / OffContent are native Avalonia composition outside the Material track.
internal sealed class SwitchLayout : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 4) return default;
        Children[0].Measure(new Size(52, 32));
        var owner = (ToggleSwitch)TemplatedParent!;
        var contentWidth = 0d;
        var contentHeight = 0d;
        foreach (var presenter in Children.Skip(1).Cast<ContentPresenter>())
        {
            presenter.Measure(new Size(Math.Max(0, availableSize.Width - 52), availableSize.Height));
            if (presenter.IsVisible && presenter.Child is not null)
            {
                contentWidth += presenter.DesiredSize.Width;
                contentHeight = Math.Max(contentHeight, presenter.DesiredSize.Height);
            }
        }
        var padding = contentWidth == 0 ? default : owner.Padding;
        return new Size(52 + contentWidth + padding.Left + padding.Right,
            Math.Max(48, contentHeight + padding.Top + padding.Bottom));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 4) return finalSize;
        var scale = LayoutHelper.GetLayoutScale(this);
        var hasContent = Children.Skip(1).Cast<ContentPresenter>().Any(p => p.IsVisible && p.Child is not null);
        var slotWidth = hasContent ? Math.Min(52, finalSize.Width) : finalSize.Width;
        double Center(double slot, double minimum, double child) =>
            (MathUtils.RoundToInt((Math.Max(slot, minimum) - child) * scale / 2)
             + Math.Truncate((slot - Math.Max(slot, minimum)) * scale / 2)) / scale;
        Children[0].Arrange(new Rect(Center(slotWidth, 52, 52), Center(finalSize.Height, 48, 32), 52, 32));
        var padding = hasContent ? ((ToggleSwitch)TemplatedParent!).Padding : default;
        var left = Math.Min(52, finalSize.Width) + padding.Left;
        foreach (var presenter in Children.Skip(1).Cast<ContentPresenter>())
        {
            var width = presenter.IsVisible ? Math.Min(presenter.DesiredSize.Width, Math.Max(0, finalSize.Width - left - padding.Right)) : 0;
            presenter.Arrange(new Rect(left, padding.Top, width, Math.Max(0, finalSize.Height - padding.Top - padding.Bottom)));
            left += width;
        }
        return finalSize;
    }
}
