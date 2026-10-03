using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// RadioButton.kt: minimumInteractiveComponentSize -> wrapContentSize -> padding(2) -> requiredSize(20).
// The optional native Content is a local composition alongside the indicator, not part of its canvas.
internal sealed class RadioButtonLayout : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        Children[0].Measure(new Size(24, 24));
        var owner = (RadioButton)TemplatedParent!;
        var padding = owner.Content is null ? default : owner.Padding;
        Children[1].Measure(new Size(Math.Max(0, availableSize.Width - 48 - padding.Left - padding.Right), availableSize.Height));
        var content = owner.Content is null ? default : Children[1].DesiredSize;
        return new Size(48 + content.Width + padding.Left + padding.Right,
            Math.Max(48, content.Height + padding.Top + padding.Bottom));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var owner = (RadioButton)TemplatedParent!;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Center(double slot, double child) =>
            (MathUtils.RoundToInt((Math.Max(slot, 48) - child) * scale / 2)
             + Math.Truncate((slot - Math.Max(slot, 48)) * scale / 2)) / scale;
        var slotWidth = owner.Content is null ? finalSize.Width : Math.Min(48, finalSize.Width);
        // Avalonia mirrors the content row in RTL; the circular indicator is symmetric.
        Children[0].Arrange(new Rect(Center(slotWidth, 24), Center(finalSize.Height, 24), 24, 24));
        var padding = owner.Content is null ? default : owner.Padding;
        Children[1].Arrange(new Rect(Math.Min(48, finalSize.Width) + padding.Left, padding.Top,
            Math.Max(0, finalSize.Width - 48 - padding.Left - padding.Right),
            Math.Max(0, finalSize.Height - padding.Top - padding.Bottom)));
        return finalSize;
    }
}
