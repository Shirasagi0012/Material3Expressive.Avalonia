using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// SplitButton.kt measurePolicy: intrinsic queries, shared height, trailing first, remaining leading width.
internal sealed class SplitButtonLayout : Panel
{
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<SplitButtonLayout, double>(nameof(Spacing), 2);
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    private Size _leading, _trailing;
    private double _spacing;
    static SplitButtonLayout() => AffectsMeasure<SplitButtonLayout>(SpacingProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        var scale = LayoutHelper.GetLayoutScale(this);
        _spacing = MathUtils.DipToPx(Spacing, scale) / scale;
        // Avalonia has no general intrinsic interface. Actual independent controls provide
        // intrinsic desired sizes by measuring at the corresponding width/height constraint.
        Children[1].Measure(new Size(double.PositiveInfinity, availableSize.Height));
        var intrinsicWidth = Children[1].DesiredSize.Width;
        var leadingWidth = Math.Max(0, availableSize.Width - intrinsicWidth - _spacing);
        Children[0].Measure(new Size(leadingWidth, double.PositiveInfinity));
        Children[1].Measure(new Size(intrinsicWidth, double.PositiveInfinity));
        var height = Math.Min(availableSize.Height, Math.Max(Children[0].DesiredSize.Height, Children[1].DesiredSize.Height));
        Children[1].Measure(new Size(availableSize.Width, height));
        _trailing = new Size(Children[1].DesiredSize.Width, height);
        Children[0].Measure(new Size(Math.Max(0, availableSize.Width - _trailing.Width - _spacing), height));
        _leading = new Size(Children[0].DesiredSize.Width, height);
        return new Size(Math.Min(availableSize.Width, _leading.Width + _trailing.Width + _spacing), height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 2)
        {
            Children[0].Arrange(new Rect(0, (finalSize.Height - _leading.Height) / 2, _leading.Width, _leading.Height));
            Children[1].Arrange(new Rect(_leading.Width + _spacing, (finalSize.Height - _trailing.Height) / 2, _trailing.Width, _trailing.Height));
        }
        return finalSize;
    }
}
