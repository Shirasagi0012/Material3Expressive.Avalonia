using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>A morphing Material loading indicator, with determinate and indeterminate modes.</summary>
public sealed class LoadingIndicator : LoadingIndicatorBase
{
    public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<LoadingIndicator, Color>(nameof(Color));
    public Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    protected override Color ActiveColor => Color;
}
