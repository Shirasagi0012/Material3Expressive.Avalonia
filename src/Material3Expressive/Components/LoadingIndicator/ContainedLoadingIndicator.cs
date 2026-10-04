using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>A loading indicator whose morphing shape is clipped inside a colored container.</summary>
public sealed class ContainedLoadingIndicator : LoadingIndicatorBase
{
    public static readonly StyledProperty<Color> IndicatorColorProperty = AvaloniaProperty.Register<ContainedLoadingIndicator, Color>(nameof(IndicatorColor));
    public Color IndicatorColor { get => GetValue(IndicatorColorProperty); set => SetValue(IndicatorColorProperty, value); }
    public static readonly StyledProperty<Color> ContainerColorProperty = AvaloniaProperty.Register<ContainedLoadingIndicator, Color>(nameof(ContainerColor));
    public Color ContainerColor { get => GetValue(ContainerColorProperty); set => SetValue(ContainerColorProperty, value); }
    protected override Color ActiveColor => IndicatorColor;
    protected override Color BackgroundColor => ContainerColor;
}
