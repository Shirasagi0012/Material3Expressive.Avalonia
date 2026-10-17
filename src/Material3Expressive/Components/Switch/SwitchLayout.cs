using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

// Switch.kt: minimumInteractiveComponentSize -> wrapContentSize(Center) -> requiredSize(52,32).
// Content / OnContent / OffContent are native Avalonia composition outside the Material track.
internal sealed class SwitchLayout : Panel
{
    static SwitchLayout() => AffectsMeasure<SwitchLayout>(MaterialInteractiveSize.MinimumProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 4) return default;
        Children[0].Measure(new Size(52, 32));
        var owner = (ToggleSwitch)TemplatedParent!;
        var trackSlotWidth = Math.Max(52, MaterialInteractiveSize.Resolve(this));
        var contentWidth = 0d;
        var contentHeight = 0d;
        foreach (var presenter in Children.Skip(1).Cast<ContentPresenter>())
        {
            presenter.Measure(new Size(Math.Max(0, availableSize.Width - trackSlotWidth), availableSize.Height));
            if (presenter.IsVisible && presenter.Child is not null)
            {
                contentWidth += presenter.DesiredSize.Width;
                contentHeight = Math.Max(contentHeight, presenter.DesiredSize.Height);
            }
        }
        var padding = contentWidth == 0 ? default : owner.Padding;
        return new Size(trackSlotWidth + contentWidth + padding.Left + padding.Right,
            Math.Max(Math.Max(32, MaterialInteractiveSize.Resolve(this)), contentHeight + padding.Top + padding.Bottom));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 4) return finalSize;
        var scale = LayoutHelper.GetLayoutScale(this);
        var hasContent = Children.Skip(1).Cast<ContentPresenter>().Any(p => p.IsVisible && p.Child is not null);
        var trackSlotWidth = Math.Max(52, MaterialInteractiveSize.Resolve(this));
        var slotWidth = hasContent ? Math.Min(trackSlotWidth, finalSize.Width) : finalSize.Width;
        double Center(double slot, double minimum, double child) =>
            (MathUtils.RoundToInt((Math.Max(slot, minimum) - child) * scale / 2)
             + Math.Truncate((slot - Math.Max(slot, minimum)) * scale / 2)) / scale;
        Children[0].Arrange(new Rect(Center(slotWidth, trackSlotWidth, 52), Center(finalSize.Height, Math.Max(32, MaterialInteractiveSize.Resolve(this)), 32), 52, 32));
        var padding = hasContent ? ((ToggleSwitch)TemplatedParent!).Padding : default;
        var left = Math.Min(trackSlotWidth, finalSize.Width) + padding.Left;
        foreach (var presenter in Children.Skip(1).Cast<ContentPresenter>())
        {
            var width = presenter.IsVisible ? Math.Min(presenter.DesiredSize.Width, Math.Max(0, finalSize.Width - left - padding.Right)) : 0;
            presenter.Arrange(new Rect(left, padding.Top, width, Math.Max(0, finalSize.Height - padding.Top - padding.Bottom)));
            left += width;
        }
        return finalSize;
    }
}
