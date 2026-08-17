using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

[Obsolete(
    "Legacy progress metrics snapshot. Use ProgressValueKeys endpoints instead.",
    DiagnosticId = "M3E1005")]
internal sealed record ProgressMetrics(
    ColorTokenValue ActiveIndicatorColor,
    ColorTokenValue TrackColor,
    ColorTokenValue StopColor,
    double ActiveThickness,
    double TrackThickness,
    double TrackActiveSpace,
    double Size,
    double StopSize,
    double WaveAmplitude,
    double WaveWavelength,
    double IndeterminateWaveWavelength,
    double WaveSize);

/// <summary>Component-owned policy shared by linear and circular progress indicators.</summary>
[Obsolete(
    "Legacy progress defaults transport. Use ProgressValueKeys endpoints instead.",
    DiagnosticId = "M3E1005")]
internal static class ProgressDefaults
{
    [Obsolete(
        "Legacy progress metrics snapshot. Use ProgressValueKeys endpoints instead.",
        DiagnosticId = "M3E1005")]
    public static ProgressMetrics Metrics(ProgressIndicatorShape shape) => shape switch
    {
        ProgressIndicatorShape.Linear => new(
            ProgressIndicatorTokens.ActiveIndicatorColor,
            ProgressIndicatorTokens.TrackColor,
            ProgressIndicatorTokens.StopColor,
            LinearProgressIndicatorTokens.ActiveThickness,
            LinearProgressIndicatorTokens.TrackThickness,
            LinearProgressIndicatorTokens.TrackActiveSpace,
            LinearProgressIndicatorTokens.Height,
            LinearProgressIndicatorTokens.StopSize,
            LinearProgressIndicatorTokens.ActiveWaveAmplitude,
            LinearProgressIndicatorTokens.ActiveWaveWavelength,
            LinearProgressIndicatorTokens.IndeterminateActiveWaveWavelength,
            LinearProgressIndicatorTokens.WaveHeight),
        ProgressIndicatorShape.Circular => new(
            ProgressIndicatorTokens.ActiveIndicatorColor,
            ProgressIndicatorTokens.TrackColor,
            ProgressIndicatorTokens.StopColor,
            CircularProgressIndicatorTokens.ActiveThickness,
            CircularProgressIndicatorTokens.TrackThickness,
            CircularProgressIndicatorTokens.TrackActiveSpace,
            CircularProgressIndicatorTokens.Size,
            0,
            CircularProgressIndicatorTokens.ActiveWaveAmplitude,
            CircularProgressIndicatorTokens.ActiveWaveWavelength,
            CircularProgressIndicatorTokens.ActiveWaveWavelength,
            CircularProgressIndicatorTokens.WaveSize),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
    };

    [Obsolete(
        "Legacy progress colour projection. Use ProgressValueKeys.VisualColors instead.",
        DiagnosticId = "M3E1005")]
    public static ProgressVisualColors Colors(ResolvedTheme theme, ProgressIndicatorShape shape, bool isDark)
    {
        var metrics = Metrics(shape);
        ArgumentNullException.ThrowIfNull(theme);
        return new(
            metrics.ActiveIndicatorColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.TrackColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.StopColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}
