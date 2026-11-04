using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Components;

// Menu.kt expressive Row measures unweighted content in declaration order. A Grid's
// Auto,*,Auto columns instead reserve the trailing content before measuring the label.
// Used by the native item's main row and its combined custom/gesture/chevron trailing slot.
internal sealed class NativeMenuRow : Panel
{
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<NativeMenuRow, double>(nameof(Spacing));
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public bool AlignLastToEnd { get; set; }
    static NativeMenuRow() => AffectsMeasure<NativeMenuRow>(SpacingProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        var first = true;
        foreach (var child in Children)
        {
            if (!child.IsVisible) { child.Measure(default); continue; }
            if (!first) width += Math.Min(Spacing, Math.Max(0, availableSize.Width - width));
            child.Measure(new Size(Math.Max(0, availableSize.Width - width), availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
            first = false;
        }
        return new(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0d;
        var first = true;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            if (!first) x += Math.Min(Spacing, Math.Max(0, finalSize.Width - x));
            if (AlignLastToEnd && ReferenceEquals(child, Children[^1])) x = finalSize.Width - child.DesiredSize.Width;
            child.Arrange(new Rect(x, (finalSize.Height - child.DesiredSize.Height) / 2,
                child.DesiredSize.Width, child.DesiredSize.Height));
            x += child.DesiredSize.Width;
            first = false;
        }
        return finalSize;
    }
}
