using Avalonia;

namespace Material3Expressive.Components;

public abstract class WavyProgressIndicator : ProgressIndicator
{
    public static readonly StyledProperty<ProgressIndicatorStroke?> StrokeProperty =
        AvaloniaProperty.Register<WavyProgressIndicator, ProgressIndicatorStroke?>(nameof(Stroke));
    public ProgressIndicatorStroke? Stroke { get => GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public static readonly StyledProperty<ProgressIndicatorStroke?> TrackStrokeProperty =
        AvaloniaProperty.Register<WavyProgressIndicator, ProgressIndicatorStroke?>(nameof(TrackStroke));
    public ProgressIndicatorStroke? TrackStroke { get => GetValue(TrackStrokeProperty); set => SetValue(TrackStrokeProperty, value); }
    public static readonly StyledProperty<double> ActiveThicknessProperty =
        AvaloniaProperty.Register<WavyProgressIndicator, double>(nameof(ActiveThickness));
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public double ActiveThickness { get => GetValue(ActiveThicknessProperty); set => SetValue(ActiveThicknessProperty, value); }
    public static readonly StyledProperty<double> TrackThicknessProperty =
        AvaloniaProperty.Register<WavyProgressIndicator, double>(nameof(TrackThickness));
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public double TrackThickness { get => GetValue(TrackThicknessProperty); set => SetValue(TrackThicknessProperty, value); }
    internal ProgressIndicatorStroke ActiveStroke => Stroke ?? new(ActiveThickness);
    internal ProgressIndicatorStroke ResolvedTrackStroke => TrackStroke ?? new(TrackThickness);
    /// <summary>Null uses AmplitudeSelector for determinate, or 1 for indeterminate.</summary>
    public static readonly StyledProperty<double?> AmplitudeProperty =
        AvaloniaProperty.Register<WavyProgressIndicator, double?>(nameof(Amplitude));
    public double? Amplitude { get => GetValue(AmplitudeProperty); set => SetValue(AmplitudeProperty, value); }
    public static readonly StyledProperty<Func<double, double>> AmplitudeSelectorProperty =
        AvaloniaProperty.Register<WavyProgressIndicator, Func<double, double>>(nameof(AmplitudeSelector),
            WavyProgressIndicatorDefaults.IndicatorAmplitude);
    public Func<double, double> AmplitudeSelector { get => GetValue(AmplitudeSelectorProperty); set => SetValue(AmplitudeSelectorProperty, value); }
    /// <summary>Null selects the variant's default wavelength.</summary>
    public static readonly StyledProperty<double?> WavelengthProperty =
        AvaloniaProperty.Register<WavyProgressIndicator, double?>(nameof(Wavelength));
    public double? Wavelength { get => GetValue(WavelengthProperty); set => SetValue(WavelengthProperty, value); }
    /// <summary>DIP per second. Null follows Wavelength; non-positive values stop wave motion.</summary>
    public static readonly StyledProperty<double?> WaveSpeedProperty =
        AvaloniaProperty.Register<WavyProgressIndicator, double?>(nameof(WaveSpeed));
    public double? WaveSpeed { get => GetValue(WaveSpeedProperty); set => SetValue(WaveSpeedProperty, value); }
}

public sealed class LinearWavyProgressIndicator : WavyProgressIndicator
{
    public static readonly StyledProperty<double> StopSizeProperty =
        AvaloniaProperty.Register<LinearWavyProgressIndicator, double>(nameof(StopSize));
    public double StopSize { get => GetValue(StopSizeProperty); set => SetValue(StopSizeProperty, value); }
}

public sealed class CircularWavyProgressIndicator : WavyProgressIndicator;
