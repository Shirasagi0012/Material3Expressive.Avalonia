using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

// Button.kt's defaultMinSize inside Surface.kt's minimumInteractiveComponentSize.
// Avalonia exposes maximum space to MeasureOverride, so template bindings carry the
// owner's explicit dimensions/minima separately from the default container size.
internal sealed class ButtonLayout : Decorator
{
    public static readonly StyledProperty<double> ContainerMinHeightProperty =
        AvaloniaProperty.Register<ButtonLayout, double>(nameof(ContainerMinHeight));

    private Size _containerSize;
    private double _interactiveSize;

    static ButtonLayout() => AffectsMeasure<ButtonLayout>(ContainerMinHeightProperty);

    public double ContainerMinHeight
    {
        get => GetValue(ContainerMinHeightProperty);
        set => SetValue(ContainerMinHeightProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize);
        var content = Child?.DesiredSize ?? default;
        var scale = LayoutHelper.GetLayoutScale(this);
        var minimumWidth = MinWidth > 0 ? MinWidth : Math.Min(Math.Floor(58 * scale + 0.5) / scale, availableSize.Width);
        var minimumHeight = MinHeight > 0 ? MinHeight : Math.Min(Math.Floor(ContainerMinHeight * scale + 0.5) / scale, availableSize.Height);
        _containerSize = new Size(
            double.IsNaN(Width) ? Math.Max(content.Width, minimumWidth) : availableSize.Width,
            double.IsNaN(Height) ? Math.Max(content.Height, minimumHeight) : availableSize.Height);

        // Disabled components retain this spacing too. This reserves layout space;
        // touch target expansion is a separate input-layer responsibility upstream.
        _interactiveSize = this.IsAttachedToVisualTree() ? Math.Floor(48 * scale + 0.5) / scale : 0;
        return new Size(Math.Max(_containerSize.Width, _interactiveSize), Math.Max(_containerSize.Height, _interactiveSize));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = double.IsNaN(Width) && HorizontalAlignment != HorizontalAlignment.Stretch && finalSize.Width <= _interactiveSize
            ? Math.Min(_containerSize.Width, finalSize.Width) : finalSize.Width;
        var height = double.IsNaN(Height) && VerticalAlignment != VerticalAlignment.Stretch && finalSize.Height <= _interactiveSize
            ? Math.Min(_containerSize.Height, finalSize.Height) : finalSize.Height;
        var scale = LayoutHelper.GetLayoutScale(this);
        var layoutWidth = Math.Max(finalSize.Width, _interactiveSize);
        var layoutHeight = Math.Max(finalSize.Height, _interactiveSize);
        // Compose rounds the positive centering offset to the nearest pixel, ties up.
        // Placeable's apparentToRealOffset uses integer division if the parent coerces
        // the minimum interactive layout below its reported size; keep both rounding steps separate.
        var x = (Math.Floor((layoutWidth - width) * scale / 2 + 0.5) + Math.Truncate((finalSize.Width - layoutWidth) * scale / 2)) / scale;
        var y = (Math.Floor((layoutHeight - height) * scale / 2 + 0.5) + Math.Truncate((finalSize.Height - layoutHeight) * scale / 2)) / scale;
        Child?.Arrange(new Rect(x, y, width, height));
        return finalSize;
    }
}
