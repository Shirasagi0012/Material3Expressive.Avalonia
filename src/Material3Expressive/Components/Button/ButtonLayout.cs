using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Button.kt's defaultMinSize inside Surface.kt's minimumInteractiveComponentSize.
// Avalonia exposes maximum space to MeasureOverride, so template bindings carry the
// owner's explicit dimensions/minima separately from the default container size.
internal sealed class ButtonLayout : Decorator, IMinimumInteractiveLayout
{
    public static readonly StyledProperty<double> ContainerMinHeightProperty =
        AvaloniaProperty.Register<ButtonLayout, double>(nameof(ContainerMinHeight));
    public static readonly StyledProperty<double> ContainerMinWidthProperty =
        AvaloniaProperty.Register<ButtonLayout, double>(nameof(ContainerMinWidth), 58);
    public static readonly StyledProperty<double> MinimumInteractiveSizeProperty =
        AvaloniaProperty.Register<ButtonLayout, double>(nameof(MinimumInteractiveSize), 48);
    public double ContainerMinWidth { get => GetValue(ContainerMinWidthProperty); set => SetValue(ContainerMinWidthProperty, value); }
    public double MinimumInteractiveSize { get => GetValue(MinimumInteractiveSizeProperty); set => SetValue(MinimumInteractiveSizeProperty, value); }

    private Size _containerSize;
    private double _interactiveSize;
    // Alignment-line consumers need the measured container, rather than recomputing its minima.
    internal Size ContainerSize => _containerSize;
    public Point MinimumInteractivePadding
    {
        get
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            return new Point(Math.Max(0, MathUtils.RoundToInt((_interactiveSize - _containerSize.Width) * scale / 2)) / scale,
                Math.Max(0, MathUtils.RoundToInt((_interactiveSize - _containerSize.Height) * scale / 2)) / scale);
        }
    }

    static ButtonLayout() => AffectsMeasure<ButtonLayout>(ContainerMinHeightProperty, ContainerMinWidthProperty, MinimumInteractiveSizeProperty);

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
        var minimumWidth = MinWidth > 0 ? MinWidth : Math.Min(MathUtils.PxToDip(MathUtils.DipToPx(ContainerMinWidth, scale), scale), availableSize.Width);
        var minimumHeight = MinHeight > 0 ? MinHeight : Math.Min(MathUtils.PxToDip(MathUtils.DipToPx(ContainerMinHeight, scale), scale), availableSize.Height);
        _containerSize = new Size(
            double.IsNaN(Width) ? Math.Max(content.Width, minimumWidth) : availableSize.Width,
            double.IsNaN(Height) ? Math.Max(content.Height, minimumHeight) : availableSize.Height);

        // Disabled components retain this spacing too. This reserves layout space;
        // touch target expansion is a separate input-layer responsibility upstream.
        _interactiveSize = this.IsAttachedToVisualTree() ? MathUtils.PxToDip(MathUtils.DipToPx(MinimumInteractiveSize, scale), scale) : 0;
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
        var x = (MathUtils.RoundToInt((layoutWidth - width) * scale / 2) + Math.Truncate((finalSize.Width - layoutWidth) * scale / 2)) / scale;
        var y = (MathUtils.RoundToInt((layoutHeight - height) * scale / 2) + Math.Truncate((finalSize.Height - layoutHeight) * scale / 2)) / scale;
        Child?.Arrange(new Rect(x, y, width, height));
        return finalSize;
    }
}
