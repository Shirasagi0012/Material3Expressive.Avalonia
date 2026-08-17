using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by the linear and circular progress indicators.
/// </summary>
/// <remarks>
/// The indicators deliberately expose separate metric profiles because the two shapes have
/// different consumer slots.  Colour is shared, while determinate progress, indeterminate sweep,
/// wave phase, bounds and animation remain control-owned state.
/// </remarks>
public static class ProgressValueKeys
{
    /// <summary>Gets the immutable brushes shared by both progress indicator shapes.</summary>
    public static MaterialValueKey<ProgressVisualColors> VisualColors { get; } =
        new(
            "Value.Progress.VisualColors",
            new RawRecipePlan<ProgressVisualColors>(
                new ProgressVisualColorsRecipe(
                    ProgressIndicatorTokensKeys.ActiveIndicatorColor,
                    ProgressIndicatorTokensKeys.TrackColor,
                    ProgressIndicatorTokensKeys.StopColor)));

    /// <summary>Gets the metrics consumed by a linear progress indicator.</summary>
    public static MaterialValueKey<LinearProgressMetrics> LinearMetrics { get; } =
        new(
            "Value.Progress.LinearMetrics",
            new RawRecipePlan<LinearProgressMetrics>(
                new LinearProgressMetricsRecipe(
                    LinearProgressIndicatorTokensKeys.ActiveThickness,
                    LinearProgressIndicatorTokensKeys.TrackThickness,
                    LinearProgressIndicatorTokensKeys.TrackActiveSpace,
                    LinearProgressIndicatorTokensKeys.Height,
                    LinearProgressIndicatorTokensKeys.StopSize,
                    LinearProgressIndicatorTokensKeys.ActiveWaveAmplitude,
                    LinearProgressIndicatorTokensKeys.ActiveWaveWavelength,
                    LinearProgressIndicatorTokensKeys.IndeterminateActiveWaveWavelength,
                    LinearProgressIndicatorTokensKeys.WaveHeight)));

    /// <summary>Gets the metrics consumed by a circular progress indicator.</summary>
    public static MaterialValueKey<CircularProgressMetrics> CircularMetrics { get; } =
        new(
            "Value.Progress.CircularMetrics",
            new RawRecipePlan<CircularProgressMetrics>(
                new CircularProgressMetricsRecipe(
                    CircularProgressIndicatorTokensKeys.ActiveThickness,
                    CircularProgressIndicatorTokensKeys.TrackThickness,
                    CircularProgressIndicatorTokensKeys.TrackActiveSpace,
                    CircularProgressIndicatorTokensKeys.Size,
                    CircularProgressIndicatorTokensKeys.ActiveWaveAmplitude,
                    CircularProgressIndicatorTokensKeys.ActiveWaveWavelength,
                    CircularProgressIndicatorTokensKeys.WaveSize)));
}

/// <summary>Immutable brushes consumed by either progress indicator renderer.</summary>
public sealed record ProgressVisualColors(
    IImmutableBrush ActiveIndicatorColor,
    IImmutableBrush TrackColor,
    IImmutableBrush StopColor);

/// <summary>Raw-derived geometry and wave metrics consumed by the linear renderer.</summary>
public sealed record LinearProgressMetrics(
    double ActiveThickness,
    double TrackThickness,
    double TrackActiveSpace,
    double Height,
    double StopSize,
    double WaveAmplitude,
    double WaveWavelength,
    double IndeterminateWaveWavelength,
    double WaveSize);

/// <summary>Raw-derived geometry and wave metrics consumed by the circular renderer.</summary>
public sealed record CircularProgressMetrics(
    double ActiveThickness,
    double TrackThickness,
    double TrackActiveSpace,
    double Size,
    double WaveAmplitude,
    double WaveWavelength,
    double WaveSize);

internal sealed class ProgressVisualColorsRecipe : IRawValueRecipe<ProgressVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _activeIndicatorColor;
    private readonly MaterialToken<Color> _trackColor;
    private readonly MaterialToken<Color> _stopColor;

    internal ProgressVisualColorsRecipe(
        MaterialToken<Color> activeIndicatorColor,
        MaterialToken<Color> trackColor,
        MaterialToken<Color> stopColor)
    {
        _activeIndicatorColor = activeIndicatorColor ??
            throw new ArgumentNullException(nameof(activeIndicatorColor));
        _trackColor = trackColor ?? throw new ArgumentNullException(nameof(trackColor));
        _stopColor = stopColor ?? throw new ArgumentNullException(nameof(stopColor));
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [activeIndicatorColor, trackColor, stopColor]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ProgressVisualColors Evaluate(MaterialValueContext context) => new(
        Brush(context.Get(_activeIndicatorColor)),
        Brush(context.Get(_trackColor)),
        Brush(context.Get(_stopColor)));

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);
}

internal sealed class LinearProgressMetricsRecipe : IRawValueRecipe<LinearProgressMetrics>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _activeThickness;
    private readonly MaterialToken<double> _trackThickness;
    private readonly MaterialToken<double> _trackActiveSpace;
    private readonly MaterialToken<double> _height;
    private readonly MaterialToken<double> _stopSize;
    private readonly MaterialToken<double> _waveAmplitude;
    private readonly MaterialToken<double> _waveWavelength;
    private readonly MaterialToken<double> _indeterminateWaveWavelength;
    private readonly MaterialToken<double> _waveSize;

    internal LinearProgressMetricsRecipe(
        MaterialToken<double> activeThickness,
        MaterialToken<double> trackThickness,
        MaterialToken<double> trackActiveSpace,
        MaterialToken<double> height,
        MaterialToken<double> stopSize,
        MaterialToken<double> waveAmplitude,
        MaterialToken<double> waveWavelength,
        MaterialToken<double> indeterminateWaveWavelength,
        MaterialToken<double> waveSize)
    {
        _activeThickness = activeThickness ?? throw new ArgumentNullException(nameof(activeThickness));
        _trackThickness = trackThickness ?? throw new ArgumentNullException(nameof(trackThickness));
        _trackActiveSpace = trackActiveSpace ?? throw new ArgumentNullException(nameof(trackActiveSpace));
        _height = height ?? throw new ArgumentNullException(nameof(height));
        _stopSize = stopSize ?? throw new ArgumentNullException(nameof(stopSize));
        _waveAmplitude = waveAmplitude ?? throw new ArgumentNullException(nameof(waveAmplitude));
        _waveWavelength = waveWavelength ?? throw new ArgumentNullException(nameof(waveWavelength));
        _indeterminateWaveWavelength = indeterminateWaveWavelength ??
            throw new ArgumentNullException(nameof(indeterminateWaveWavelength));
        _waveSize = waveSize ?? throw new ArgumentNullException(nameof(waveSize));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            activeThickness,
            trackThickness,
            trackActiveSpace,
            height,
            stopSize,
            waveAmplitude,
            waveWavelength,
            indeterminateWaveWavelength,
            waveSize,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public LinearProgressMetrics Evaluate(MaterialValueContext context) => new(
        context.Get(_activeThickness),
        context.Get(_trackThickness),
        context.Get(_trackActiveSpace),
        context.Get(_height),
        context.Get(_stopSize),
        context.Get(_waveAmplitude),
        context.Get(_waveWavelength),
        context.Get(_indeterminateWaveWavelength),
        context.Get(_waveSize));
}

internal sealed class CircularProgressMetricsRecipe : IRawValueRecipe<CircularProgressMetrics>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _activeThickness;
    private readonly MaterialToken<double> _trackThickness;
    private readonly MaterialToken<double> _trackActiveSpace;
    private readonly MaterialToken<double> _size;
    private readonly MaterialToken<double> _waveAmplitude;
    private readonly MaterialToken<double> _waveWavelength;
    private readonly MaterialToken<double> _waveSize;

    internal CircularProgressMetricsRecipe(
        MaterialToken<double> activeThickness,
        MaterialToken<double> trackThickness,
        MaterialToken<double> trackActiveSpace,
        MaterialToken<double> size,
        MaterialToken<double> waveAmplitude,
        MaterialToken<double> waveWavelength,
        MaterialToken<double> waveSize)
    {
        _activeThickness = activeThickness ?? throw new ArgumentNullException(nameof(activeThickness));
        _trackThickness = trackThickness ?? throw new ArgumentNullException(nameof(trackThickness));
        _trackActiveSpace = trackActiveSpace ?? throw new ArgumentNullException(nameof(trackActiveSpace));
        _size = size ?? throw new ArgumentNullException(nameof(size));
        _waveAmplitude = waveAmplitude ?? throw new ArgumentNullException(nameof(waveAmplitude));
        _waveWavelength = waveWavelength ?? throw new ArgumentNullException(nameof(waveWavelength));
        _waveSize = waveSize ?? throw new ArgumentNullException(nameof(waveSize));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            activeThickness,
            trackThickness,
            trackActiveSpace,
            size,
            waveAmplitude,
            waveWavelength,
            waveSize,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public CircularProgressMetrics Evaluate(MaterialValueContext context) => new(
        context.Get(_activeThickness),
        context.Get(_trackThickness),
        context.Get(_trackActiveSpace),
        context.Get(_size),
        context.Get(_waveAmplitude),
        context.Get(_waveWavelength),
        context.Get(_waveSize));
}
