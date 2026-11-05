using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

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
        double intrinsicExtra = 0;
        var first = true;
        foreach (var child in Children)
        {
            if (!child.IsVisible) { child.Measure(default); continue; }
            if (!first) width += Math.Min(Spacing, Math.Max(0, availableSize.Width - width));
            child.Measure(new Size(Math.Max(0, availableSize.Width - width), availableSize.Height));
            width += child.DesiredSize.Width;
            if (child is DropdownMenuLeadingIcon icon && this.FindAncestorOfType<MenuIntrinsicWidth>() is not null)
                intrinsicExtra += icon.IntrinsicWidth - child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
            first = false;
        }
        // The menu queries the retained icon's full width; actual row placement still
        // uses the animated width, matching Column.width(IntrinsicSize.Max).
        if (intrinsicExtra < 0)
        {
            // During spring overshoot the intrinsic allocation stays at the full
            // icon width. Remeasure the row's contents within that allocation, but
            // retain its intrinsic request for the popup's unbounded measurement.
            var remaining = width + intrinsicExtra;
            first = true;
            height = 0;
            foreach (var child in Children.Where(child => child.IsVisible))
            {
                if (!first) remaining = Math.Max(0, remaining - Spacing);
                child.Measure(new Size(remaining, availableSize.Height));
                remaining = Math.Max(0, remaining - child.DesiredSize.Width);
                height = Math.Max(height, child.DesiredSize.Height);
                first = false;
            }
        }
        return new(width + intrinsicExtra, height);
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
