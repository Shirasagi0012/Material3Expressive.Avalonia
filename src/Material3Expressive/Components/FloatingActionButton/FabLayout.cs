using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Surface's minimumInteractiveComponentSize outside the FAB's sizeIn/defaultMinSize.
internal sealed class FabLayout : Decorator, IMinimumInteractiveLayout
{
    public static readonly StyledProperty<double> ContainerMinSizeProperty = AvaloniaProperty.Register<FabLayout, double>(nameof(ContainerMinSize), 56);
    public double ContainerMinSize { get => GetValue(ContainerMinSizeProperty); set => SetValue(ContainerMinSizeProperty, value); }
    private Size _container;
    public Point MinimumInteractivePadding
    {
        get
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            return new Point(Math.Max(0, MathUtils.RoundToInt((48 - _container.Width) * scale / 2)) / scale,
                Math.Max(0, MathUtils.RoundToInt((48 - _container.Height) * scale / 2)) / scale);
        }
    }
    static FabLayout() => AffectsMeasure<FabLayout>(ContainerMinSizeProperty);
    protected override Size MeasureOverride(Size available)
    {
        Child?.Measure(available);
        var content = Child?.DesiredSize ?? default;
        var scale = LayoutHelper.GetLayoutScale(this);
        var minimum = MathUtils.PxToDip(MathUtils.DipToPx(ContainerMinSize, scale), scale);
        _container = new Size(
            double.IsNaN(Width) ? Math.Max(content.Width, Math.Min(MinWidth > 0 ? MinWidth : minimum, available.Width)) : available.Width,
            double.IsNaN(Height) ? Math.Max(content.Height, Math.Min(MinHeight > 0 ? MinHeight : minimum, available.Height)) : available.Height);
        return new Size(Math.Max(_container.Width, 48), Math.Max(_container.Height, 48));
    }
    protected override Size ArrangeOverride(Size final)
    {
        var width = double.IsNaN(Width) && HorizontalAlignment != HorizontalAlignment.Stretch && final.Width <= 48 ? Math.Min(_container.Width, final.Width) : final.Width;
        var height = double.IsNaN(Height) && VerticalAlignment != VerticalAlignment.Stretch && final.Height <= 48 ? Math.Min(_container.Height, final.Height) : final.Height;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Center(double outer, double inner) => (MathUtils.RoundToInt((Math.Max(outer, 48) - inner) * scale / 2) + Math.Truncate((outer - Math.Max(outer, 48)) * scale / 2)) / scale;
        Child?.Arrange(new Rect(Center(final.Width, width), Center(final.Height, height), width, height));
        return final;
    }
}
