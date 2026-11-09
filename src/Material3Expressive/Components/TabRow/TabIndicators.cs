using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

/// <summary>A standalone primary tab indicator with required drawing dimensions and a configurable shape.</summary>
public sealed class PrimaryTabIndicator : TemplatedControl
{
    public static readonly StyledProperty<double> IndicatorWidthProperty =
        AvaloniaProperty.Register<PrimaryTabIndicator, double>(nameof(IndicatorWidth), double.NaN);
    public static readonly StyledProperty<double> IndicatorHeightProperty =
        AvaloniaProperty.Register<PrimaryTabIndicator, double>(nameof(IndicatorHeight), double.NaN);
    public static readonly StyledProperty<MaterialShape?> ShapeProperty = MaterialSurface.ShapeProperty.AddOwner<PrimaryTabIndicator>();
    /// <summary>The required drawing width. NaN follows the allocated width.</summary>
    public double IndicatorWidth { get => GetValue(IndicatorWidthProperty); set => SetValue(IndicatorWidthProperty, value); }
    /// <summary>The required drawing height. It can extend beyond a smaller outer constraint.</summary>
    public double IndicatorHeight { get => GetValue(IndicatorHeightProperty); set => SetValue(IndicatorHeightProperty, value); }
    /// <summary>An explicit shape; null uses CornerRadius.</summary>
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
}

/// <summary>A standalone rectangular tab indicator that fills the allocated width and respects height constraints.</summary>
public sealed class SecondaryTabIndicator : TemplatedControl
{
    public static readonly StyledProperty<double> IndicatorHeightProperty = PrimaryTabIndicator.IndicatorHeightProperty.AddOwner<SecondaryTabIndicator>();
    public double IndicatorHeight { get => GetValue(IndicatorHeightProperty); set => SetValue(IndicatorHeightProperty, value); }
}

// TabRowDefaults.PrimaryIndicator uses requiredHeight/requiredWidth; SecondaryIndicator
// uses fillMaxWidth/height. Keep the actual primary drawing size separate from its
// coerced reported size, just as Foundation's SizeNode and Placeable do.
internal sealed class TabIndicatorSizeLayout : Decorator
{
    public static readonly StyledProperty<double> IndicatorWidthProperty =
        PrimaryTabIndicator.IndicatorWidthProperty.AddOwner<TabIndicatorSizeLayout>();
    public static readonly StyledProperty<double> IndicatorHeightProperty =
        PrimaryTabIndicator.IndicatorHeightProperty.AddOwner<TabIndicatorSizeLayout>();
    public static readonly StyledProperty<bool> IsPrimaryProperty = AvaloniaProperty.Register<TabIndicatorSizeLayout, bool>(nameof(IsPrimary));
    public double IndicatorWidth { get => GetValue(IndicatorWidthProperty); set => SetValue(IndicatorWidthProperty, value); }
    public double IndicatorHeight { get => GetValue(IndicatorHeightProperty); set => SetValue(IndicatorHeightProperty, value); }
    public bool IsPrimary { get => GetValue(IsPrimaryProperty); set => SetValue(IsPrimaryProperty, value); }
    static TabIndicatorSizeLayout() => AffectsMeasure<TabIndicatorSizeLayout>(IndicatorWidthProperty, IndicatorHeightProperty, IsPrimaryProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        double Px(double value) => double.IsNaN(value) ? 0 : Math.Max(0, MathUtils.DipToPx((float)value, (float)scale)) / scale;
        var width = IsPrimary ? Px(IndicatorWidth) : double.IsFinite(availableSize.Width) ? availableSize.Width : 0;
        var height = IsPrimary ? Px(IndicatorHeight) : Math.Min(Px(IndicatorHeight), availableSize.Height);
        Child?.Measure(new Size(width, height));
        return new Size(width, height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var width = IsPrimary && !double.IsNaN(IndicatorWidth) ? Math.Max(0, MathUtils.DipToPx((float)IndicatorWidth, (float)scale)) : MathUtils.DipToPx(finalSize.Width, scale);
        var height = IsPrimary && !double.IsNaN(IndicatorHeight) ? Math.Max(0, MathUtils.DipToPx((float)IndicatorHeight, (float)scale)) : MathUtils.DipToPx(finalSize.Height, scale);
        var differenceX = MathUtils.DipToPx(finalSize.Width, scale) - width;
        var x = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft ? differenceX - differenceX / 2 : differenceX / 2;
        var y = (MathUtils.DipToPx(finalSize.Height, scale) - height) / 2;
        Child?.Arrange(new Rect(x / scale, y / scale, width / scale, height / scale));
        return finalSize;
    }
}
