using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

// Checkbox.kt: minimumInteractiveComponentSize -> padding(2) -> wrapContentSize -> requiredSize(20).
// The optional native Content is a local composition alongside the indicator, not part of its canvas.
internal sealed class CheckBoxLayout : Panel
{
    static CheckBoxLayout() => AffectsMeasure<CheckBoxLayout>(MaterialInteractiveSize.MinimumProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        Children[0].Measure(new Size(24, 24));
        var minimum = Math.Max(24, MaterialInteractiveSize.Resolve(this));
        var owner = (CheckBox)TemplatedParent!;
        var padding = owner.Content is null ? default : owner.Padding;
        Children[1].Measure(new Size(Math.Max(0, availableSize.Width - minimum - padding.Left - padding.Right), availableSize.Height));
        var content = owner.Content is null ? default : Children[1].DesiredSize;
        return new Size(minimum + content.Width + padding.Left + padding.Right,
            Math.Max(minimum, content.Height + padding.Top + padding.Bottom));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var minimum = Math.Max(24, MaterialInteractiveSize.Resolve(this));
        var owner = (CheckBox)TemplatedParent!;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Center(double slot, double child) =>
            (MathUtils.RoundToInt((Math.Max(slot, minimum) - child) * scale / 2)
             + Math.Truncate((slot - Math.Max(slot, minimum)) * scale / 2)) / scale;
        var slotWidth = owner.Content is null ? finalSize.Width : Math.Min(minimum, finalSize.Width);
        // Let Avalonia mirror the row in RTL, but keep the checkmark's own canvas unmirrored.
        Children[0].Arrange(new Rect(Center(slotWidth, 24), Center(finalSize.Height, 24), 24, 24));
        var padding = owner.Content is null ? default : owner.Padding;
        Children[1].Arrange(new Rect(Math.Min(minimum, finalSize.Width) + padding.Left, padding.Top,
            Math.Max(0, finalSize.Width - minimum - padding.Left - padding.Right),
            Math.Max(0, finalSize.Height - padding.Top - padding.Bottom)));
        return finalSize;
    }
}
