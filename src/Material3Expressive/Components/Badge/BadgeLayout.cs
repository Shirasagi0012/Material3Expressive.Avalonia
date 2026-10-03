using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Badge.kt: defaultMinSize precedes background and content padding. Explicit owner
// dimensions/minima replace the default minimum, rather than being clamped to it.
internal sealed class BadgeLayout : Decorator
{
    public static readonly StyledProperty<double> MinimumSizeProperty = AvaloniaProperty.Register<BadgeLayout, double>(nameof(MinimumSize));
    public static readonly StyledProperty<CornerRadius> ShapeProperty = AvaloniaProperty.Register<BadgeLayout, CornerRadius>(nameof(Shape));
    public double MinimumSize { get => GetValue(MinimumSizeProperty); set => SetValue(MinimumSizeProperty, value); }
    public CornerRadius Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    static BadgeLayout()
    {
        AffectsMeasure<BadgeLayout>(MinimumSizeProperty);
        AffectsArrange<BadgeLayout>(ShapeProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize);
        var desired = Child?.DesiredSize ?? default;
        var scale = LayoutHelper.GetLayoutScale(this);
        var minimum = MathUtils.PxToDip(MathUtils.DipToPx(MinimumSize, scale), scale);
        return new Size(
            double.IsNaN(Width) ? Math.Max(desired.Width, Math.Min(MinWidth > 0 ? MinWidth : minimum, availableSize.Width)) : availableSize.Width,
            double.IsNaN(Height) ? Math.Max(desired.Height, Math.Min(MinHeight > 0 ? MinHeight : minimum, availableSize.Height)) : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is Border background)
            background.CornerRadius = double.IsPositiveInfinity(Shape.TopLeft)
                ? new CornerRadius(Math.Min(finalSize.Width, finalSize.Height) / 2) : Shape;
        Child?.Arrange(new Rect(finalSize));
        return finalSize;
    }
}
