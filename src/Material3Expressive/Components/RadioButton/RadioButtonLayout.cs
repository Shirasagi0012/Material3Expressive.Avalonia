using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

// RadioButton.kt: minimumInteractiveComponentSize -> wrapContentSize -> padding(2) -> requiredSize(20).
// The optional native Content is a local composition alongside the indicator, not part of its canvas.
internal sealed class RadioButtonLayout : Panel
{
    public static readonly StyledProperty<double> IndicatorSizeProperty =
        AvaloniaProperty.Register<RadioButtonLayout, double>(nameof(IndicatorSize), 20);
    public double IndicatorSize { get => GetValue(IndicatorSizeProperty); set => SetValue(IndicatorSizeProperty, value); }
    public static readonly StyledProperty<Thickness> IndicatorPaddingProperty =
        AvaloniaProperty.Register<RadioButtonLayout, Thickness>(nameof(IndicatorPadding), new Thickness(2));
    public Thickness IndicatorPadding { get => GetValue(IndicatorPaddingProperty); set => SetValue(IndicatorPaddingProperty, value); }

    private Size IndicatorSlot => new(IndicatorSize + IndicatorPadding.Left + IndicatorPadding.Right,
        IndicatorSize + IndicatorPadding.Top + IndicatorPadding.Bottom);

    static RadioButtonLayout() => AffectsMeasure<RadioButtonLayout>(MaterialInteractiveSize.MinimumProperty,
        IndicatorSizeProperty, IndicatorPaddingProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        var indicator = IndicatorSlot;
        Children[0].Measure(indicator);
        var interactive = MaterialInteractiveSize.Resolve(this);
        var minimum = Math.Max(IndicatorSlot.Width, interactive);
        var minimumHeight = Math.Max(IndicatorSlot.Height, interactive);
        var owner = (RadioButton)TemplatedParent!;
        var padding = owner.Content is null ? default : owner.Padding;
        Children[1].Measure(new Size(Math.Max(0, availableSize.Width - minimum - padding.Left - padding.Right), availableSize.Height));
        var content = owner.Content is null ? default : Children[1].DesiredSize;
        return new Size(minimum + content.Width + padding.Left + padding.Right,
            Math.Max(minimumHeight, content.Height + padding.Top + padding.Bottom));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var interactive = MaterialInteractiveSize.Resolve(this);
        var minimum = Math.Max(IndicatorSlot.Width, interactive);
        var minimumHeight = Math.Max(IndicatorSlot.Height, interactive);
        var owner = (RadioButton)TemplatedParent!;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Center(double slot, double child, double minimum) =>
            (MathUtils.RoundToInt((Math.Max(slot, minimum) - child) * scale / 2)
             + Math.Truncate((slot - Math.Max(slot, minimum)) * scale / 2)) / scale;
        var slotWidth = owner.Content is null ? finalSize.Width : Math.Min(minimum, finalSize.Width);
        // Avalonia mirrors the content row in RTL; the circular indicator is symmetric.
        var indicator = IndicatorSlot;
        Children[0].Arrange(new Rect(Center(slotWidth, indicator.Width, minimum),
            Center(finalSize.Height, indicator.Height, minimumHeight), indicator.Width, indicator.Height));
        var padding = owner.Content is null ? default : owner.Padding;
        Children[1].Arrange(new Rect(Math.Min(minimum, finalSize.Width) + padding.Left, padding.Top,
            Math.Max(0, finalSize.Width - minimum - padding.Left - padding.Right),
            Math.Max(0, finalSize.Height - padding.Top - padding.Bottom)));
        return finalSize;
    }
}
