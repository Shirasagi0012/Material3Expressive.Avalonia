using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Components;

public sealed class CircularProgressIndicator : ProgressIndicator
{
    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<CircularProgressIndicator, double>(nameof(StrokeWidth), 4);
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }
    public static readonly StyledProperty<PenLineCap> StrokeCapProperty =
        AvaloniaProperty.Register<CircularProgressIndicator, PenLineCap>(nameof(StrokeCap), PenLineCap.Round);
    public PenLineCap StrokeCap { get => GetValue(StrokeCapProperty); set => SetValue(StrokeCapProperty, value); }
}
