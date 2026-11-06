using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Material3Expressive.Foundation.Interaction;

// Checkbox/RadioButton null-callback branches keep padding and required icon size,
// but omit minimumInteractiveComponentSize. The indicator can overflow tight constraints.
internal sealed class SelectionDisplayLayout : Decorator
{
    public static readonly StyledProperty<double> IndicatorSizeProperty =
        AvaloniaProperty.Register<SelectionDisplayLayout, double>(nameof(IndicatorSize));
    public double IndicatorSize { get => GetValue(IndicatorSizeProperty); set => SetValue(IndicatorSizeProperty, value); }

    static SelectionDisplayLayout() => AffectsMeasure<SelectionDisplayLayout>(IndicatorSizeProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(new Size(IndicatorSize, IndicatorSize));
        return new Size(IndicatorSize + Padding.Left + Padding.Right, IndicatorSize + Padding.Top + Padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var density = LayoutHelper.GetLayoutScale(this);
        var width = IndicatorSize + Padding.Left + Padding.Right;
        var height = IndicatorSize + Padding.Top + Padding.Bottom;
        Child?.Arrange(new Rect(MathUtils.RoundToInt((finalSize.Width - width) * density / 2) / density + Padding.Left,
            MathUtils.RoundToInt((finalSize.Height - height) * density / 2) / density + Padding.Top, IndicatorSize, IndicatorSize));
        return finalSize;
    }
}
