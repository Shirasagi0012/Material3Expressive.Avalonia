using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Surface.minimumInteractiveComponentSize around ChipContent.defaultMinSize.
internal sealed class ChipLayout : Decorator
{
    public static readonly StyledProperty<double> ContainerMinHeightProperty = AvaloniaProperty.Register<ChipLayout, double>(nameof(ContainerMinHeight));
    public double ContainerMinHeight { get => GetValue(ContainerMinHeightProperty); set => SetValue(ContainerMinHeightProperty, value); }
    private Size _container;
    private double _interactive;
    static ChipLayout() => AffectsMeasure<ChipLayout>(ContainerMinHeightProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        // Chip.kt limits selectable content to 1000dp. An explicit width/minimum is an
        // incoming tight constraint and takes precedence over widthIn's maximum.
        var constraint = TemplatedParent is SelectableChip && double.IsNaN(Width)
            ? availableSize.WithWidth(Math.Min(availableSize.Width, Math.Max(1000, MinWidth))) : availableSize;
        Child?.Measure(constraint);
        var desired = Child?.DesiredSize ?? default;
        var scale = LayoutHelper.GetLayoutScale(this);
        var minimumHeight = MinHeight > 0 ? MinHeight : Math.Min(MathUtils.PxToDip(MathUtils.DipToPx(ContainerMinHeight, scale), scale), availableSize.Height);
        _container = new Size(double.IsNaN(Width) ? Math.Max(desired.Width, MinWidth) : availableSize.Width,
            double.IsNaN(Height) ? Math.Max(desired.Height, minimumHeight) : availableSize.Height);
        _interactive = this.IsAttachedToVisualTree() ? MathUtils.PxToDip(MathUtils.DipToPx(48, scale), scale) : 0;
        return new Size(Math.Max(_container.Width, _interactive), Math.Max(_container.Height, _interactive));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = double.IsNaN(Width) && HorizontalAlignment != HorizontalAlignment.Stretch && finalSize.Width <= _interactive ? Math.Min(_container.Width, finalSize.Width) : finalSize.Width;
        var height = double.IsNaN(Height) && VerticalAlignment != VerticalAlignment.Stretch && finalSize.Height <= _interactive ? Math.Min(_container.Height, finalSize.Height) : finalSize.Height;
        var scale = LayoutHelper.GetLayoutScale(this);
        var layoutWidth = Math.Max(finalSize.Width, _interactive);
        var layoutHeight = Math.Max(finalSize.Height, _interactive);
        var x = (MathUtils.RoundToInt((layoutWidth - width) * scale / 2) + Math.Truncate((finalSize.Width - layoutWidth) * scale / 2)) / scale;
        var y = (MathUtils.RoundToInt((layoutHeight - height) * scale / 2) + Math.Truncate((finalSize.Height - layoutHeight) * scale / 2)) / scale;
        Child?.Arrange(new Rect(x, y, width, height));
        return finalSize;
    }
}

// Three children are always present, including zero-width spacers. The weighted label has
// fill=false for SelectableChip, while non-selectable Chip uses IntrinsicSize.Max.
internal sealed class ChipContentLayout : Panel
{
    private ChipSpacing _spacing = new();
    public ChipSpacing Spacing { get => _spacing; set { _spacing = value; InvalidateMeasure(); } }
    private double _leading, _trailing, _label;
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 3) return default;
        var scale = LayoutHelper.GetLayoutScale(this);
        Children[0].Measure(availableSize);
        Children[2].Measure(availableSize.WithWidth(Math.Max(0, availableSize.Width - Children[0].DesiredSize.Width
            - MathUtils.DipToPx((Spacing.Leading + Spacing.Trailing) / 2, scale) / scale)));
        _leading = Children[0].DesiredSize.Width; _trailing = Children[2].DesiredSize.Width;
        var rowGap = 2 * MathUtils.DipToPx((Spacing.Leading + Spacing.Trailing) / 2, scale) / scale;
        // RowMeasurePolicy reserves the arrangement's average spacing before measuring weight.
        Children[1].Measure(new Size(Math.Max(0, availableSize.Width - _leading - _trailing - rowGap), availableSize.Height));
        _label = Children[1].DesiredSize.Width;
        return new Size(_leading + _label + _trailing + rowGap,
            Children.Max(child => child.DesiredSize.Height));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 3) return finalSize;
        var scale = LayoutHelper.GetLayoutScale(this);
        var gap = MathUtils.DipToPx(_leading > 0 ? Spacing.Leading : Spacing.Trailing, scale) / scale;
        var positions = new[] { 0d, _leading + gap, finalSize.Width - _trailing };
        var sizes = new[] { _leading, _label, _trailing };
        for (var i = 0; i < 3; i++)
        {
            var child = Children[i];
            // Avalonia's inherited FlowDirection mirrors the visual subtree once.
            var x = positions[i];
            var y = MathUtils.RoundToInt((finalSize.Height - child.DesiredSize.Height) * scale / 2) / scale;
            child.Arrange(new Rect(x, y, sizes[i], child.DesiredSize.Height));
        }
        return finalSize;
    }
}
