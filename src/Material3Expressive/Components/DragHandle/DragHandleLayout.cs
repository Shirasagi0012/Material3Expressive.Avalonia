using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

internal sealed class DragHandleLayout : Decorator, IMinimumInteractiveLayout
{
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<double> MinimumInteractiveSizeProperty = AvaloniaProperty.Register<DragHandleLayout, double>(nameof(MinimumInteractiveSize));
    public double MinimumInteractiveSize { get => GetValue(MinimumInteractiveSizeProperty); set => SetValue(MinimumInteractiveSizeProperty, value); }
    private Size _handleSize, _layoutSize;
    public Point MinimumInteractivePadding
    {
        get
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            return new Point(Math.Max(0, MathUtils.RoundToInt((_layoutSize.Width - _handleSize.Width) * scale / 2)) / scale,
                Math.Max(0, MathUtils.RoundToInt((_layoutSize.Height - _handleSize.Height) * scale / 2)) / scale);
        }
    }
    static DragHandleLayout() => AffectsMeasure<DragHandleLayout>(MinimumInteractiveSizeProperty, MaterialInteractiveSize.MinimumProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        // DragHandle's layout modifier explicitly ignores incoming constraints.
        Child?.Measure(Size.Infinity);
        _handleSize = Child?.DesiredSize ?? default;
        var scale = LayoutHelper.GetLayoutScale(this);
        var minimum = this.IsAttachedToVisualTree() ? MathUtils.RoundToInt(MaterialInteractiveSize.Resolve(this, MinimumInteractiveSize) * scale) / scale : 0;
        _layoutSize = new Size(Math.Max(minimum, _handleSize.Width), Math.Max(minimum, _handleSize.Height));
        return _layoutSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        double Offset(double actual, double layout, double handle) =>
            (MathUtils.RoundToInt((layout - handle) * scale / 2) + Math.Truncate((actual - layout) * scale / 2)) / scale;
        Child?.Arrange(new Rect(Offset(finalSize.Width, _layoutSize.Width, _handleSize.Width),
            Offset(finalSize.Height, _layoutSize.Height, _handleSize.Height), _handleSize.Width, _handleSize.Height));
        return finalSize;
    }
}
