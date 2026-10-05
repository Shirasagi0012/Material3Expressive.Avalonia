using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Components;

public sealed class LinearProgressIndicator : ProgressIndicator
{
    public static readonly StyledProperty<PenLineCap> StrokeCapProperty =
        AvaloniaProperty.Register<LinearProgressIndicator, PenLineCap>(nameof(StrokeCap));
    public PenLineCap StrokeCap { get => GetValue(StrokeCapProperty); set => SetValue(StrokeCapProperty, value); }
    public static readonly StyledProperty<double> StopSizeProperty =
        AvaloniaProperty.Register<LinearProgressIndicator, double>(nameof(StopSize));
    public double StopSize { get => GetValue(StopSizeProperty); set => SetValue(StopSizeProperty, value); }
    /// <summary>Called after track and indicator. Null uses the default stop; a no-op suppresses it.</summary>
    public static readonly StyledProperty<Action<DrawingContext, Size>?> DrawStopIndicatorProperty =
        AvaloniaProperty.Register<LinearProgressIndicator, Action<DrawingContext, Size>?>(nameof(DrawStopIndicator));
    public Action<DrawingContext, Size>? DrawStopIndicator
    { get => GetValue(DrawStopIndicatorProperty); set => SetValue(DrawStopIndicatorProperty, value); }
}
