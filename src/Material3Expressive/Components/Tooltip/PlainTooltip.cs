using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Components;

/// <summary>A Material plain tooltip surface. Content can be text or an arbitrary control.</summary>
public class PlainTooltip : ContentControl
{
    /// <summary>The content's maximum width, before any explicit Width or minimum constraints.</summary>
    public static readonly StyledProperty<double> TooltipMaxWidthProperty =
        AvaloniaProperty.Register<PlainTooltip, double>(nameof(TooltipMaxWidth));
    public static readonly StyledProperty<double> TonalElevationProperty =
        MaterialSurface.TonalElevationProperty.AddOwner<PlainTooltip>();
    public static readonly StyledProperty<double> ShadowElevationProperty =
        MaterialSurface.ShadowElevationProperty.AddOwner<PlainTooltip>();

    public double TooltipMaxWidth { get => GetValue(TooltipMaxWidthProperty); set => SetValue(TooltipMaxWidthProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    public double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }
}

// The upstream sizeIn(40 x 24, maxWidth) is inside the caller's modifier. Explicit
// dimensions and tighter incoming constraints therefore take precedence over it.
internal sealed class PlainTooltipLayout : Decorator
{
    public static readonly StyledProperty<double> DefaultMinWidthProperty = AvaloniaProperty.Register<PlainTooltipLayout, double>(nameof(DefaultMinWidth));
    public double DefaultMinWidth { get => GetValue(DefaultMinWidthProperty); set => SetValue(DefaultMinWidthProperty, value); }
    public static readonly StyledProperty<double> DefaultMinHeightProperty = AvaloniaProperty.Register<PlainTooltipLayout, double>(nameof(DefaultMinHeight));
    public double DefaultMinHeight { get => GetValue(DefaultMinHeightProperty); set => SetValue(DefaultMinHeightProperty, value); }

    public static readonly StyledProperty<double> TooltipMaxWidthProperty =
        PlainTooltip.TooltipMaxWidthProperty.AddOwner<PlainTooltipLayout>();
    public double TooltipMaxWidth { get => GetValue(TooltipMaxWidthProperty); set => SetValue(TooltipMaxWidthProperty, value); }

    static PlainTooltipLayout() => AffectsMeasure<PlainTooltipLayout>(TooltipMaxWidthProperty, DefaultMinWidthProperty, DefaultMinHeightProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var maximum = double.IsNaN(Width) ? Math.Min(availableSize.Width, Math.Max(MinWidth, TooltipMaxWidth)) : availableSize.Width;
        Child?.Measure(new Size(maximum, availableSize.Height));
        var desired = Child?.DesiredSize ?? default;
        return new Size(double.IsNaN(Width) ? Math.Max(desired.Width, Math.Min(DefaultMinWidth, maximum)) : availableSize.Width,
            double.IsNaN(Height) ? Math.Max(desired.Height, Math.Min(DefaultMinHeight, availableSize.Height)) : availableSize.Height);
    }
}
