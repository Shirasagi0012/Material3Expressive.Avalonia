using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Material3Expressive.Components;

/// <summary>
/// Card.kt's default Column: top-to-bottom placement, start alignment and no spacing.
/// Each child is measured with the remaining height, including zero when exhausted.
/// </summary>
public sealed class CardColumn : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double height = 0, width = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - height)));
            height += child.DesiredSize.Height;
            width = Math.Max(width, child.DesiredSize.Width);
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        foreach (var child in Children)
        {
            // Stretch is Avalonia's default. Here it means Column's default Start; an
            // explicit Center/Right uses the full allocation for ColumnScope.align.
            var width = child.HorizontalAlignment is HorizontalAlignment.Stretch or HorizontalAlignment.Left
                ? child.DesiredSize.Width : finalSize.Width;
            child.Arrange(new Rect(0, y, width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return finalSize;
    }
}
