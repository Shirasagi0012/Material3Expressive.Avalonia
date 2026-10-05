using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Components;

public sealed class CircularProgressIndicator : ProgressIndicator
{
    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<CircularProgressIndicator, double>(nameof(StrokeWidth));
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }
    public static readonly StyledProperty<PenLineCap> StrokeCapProperty =
        AvaloniaProperty.Register<CircularProgressIndicator, PenLineCap>(nameof(StrokeCap));
    public PenLineCap StrokeCap { get => GetValue(StrokeCapProperty); set => SetValue(StrokeCapProperty, value); }
}
