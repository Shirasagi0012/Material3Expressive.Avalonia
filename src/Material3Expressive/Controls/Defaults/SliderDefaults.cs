using Avalonia.Media;
using System.ComponentModel;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// Geometry values shared by the Slider and RangeSlider renderers.
/// </summary>
/// <remarks>
/// The generated <see cref="SliderTokens"/> class is raw upstream data.  This record is the
/// small semantic contract the two controls actually consume, including the expressive track and
/// handle corrections that used to be hidden in the defaults JSON table.
/// </remarks>
[Obsolete(
    "Legacy slider geometry snapshot. Use SliderValueKeys.GeometryProfile instead.",
    DiagnosticId = "M3E1005")]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed record SliderMetrics(
    double TrackHeight,
    double HandleWidth,
    double PressedHandleWidth,
    double HandleHeight,
    double HandlePadding,
    double StopIndicatorSize,
    double StopIndicatorTrailingSpace);

/// <summary>
/// Theme-resolved colour roles shared by Slider and RangeSlider.
/// </summary>
[Obsolete(
    "Legacy slider colour snapshot. Use SliderValueKeys.VisualColors instead.",
    DiagnosticId = "M3E1005")]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed record SliderColors(
    IImmutableBrush ActiveTrackColor,
    IImmutableBrush InactiveTrackColor,
    IImmutableBrush HandleColor,
    IImmutableBrush StopIndicatorColor,
    IImmutableBrush StateLayerColor,
    IImmutableBrush DisabledActiveTrackColor,
    IImmutableBrush DisabledInactiveTrackColor,
    IImmutableBrush DisabledHandleColor);

/// <summary>Resolved state colours selected by the legacy slider renderer.</summary>
[Obsolete(
    "Legacy slider visual projection. Use SliderValueKeys.VisualColors instead.",
    DiagnosticId = "M3E1005")]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed record LegacySliderVisualColors(
    IImmutableBrush ActiveTrackColor,
    IImmutableBrush InactiveTrackColor,
    IImmutableBrush HandleColor,
    IImmutableBrush StopIndicatorColor,
    IImmutableBrush StateLayerColor);

/// <summary>
/// Component-owned adapter shared by Slider and RangeSlider.
/// </summary>
/// <remarks>
/// The two controls share the same component token domain, while their interaction and endpoint
/// policies remain in their respective controls. Disabled colour projection is common policy and
/// lives here so it cannot drift between the two renderers.
/// </remarks>
[Obsolete(
    "Legacy slider defaults adapter. Use SliderValueKeys and MaterialValueKey<T> resources instead.",
    DiagnosticId = "M3E1005")]
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class SliderDefaults
{
    private static readonly SliderMetrics DefaultMetrics = new(
        // Expressive uses the active track dimensions for both halves.
        SliderTokens.ActiveTrackHeight,
        SliderTokens.HandleWidth,
        SliderTokens.PressedHandleWidth,
        SliderTokens.HandleHeight,
        SliderTokens.ActiveHandlePadding,
        SliderTokens.StopIndicatorSize,
        SliderTokens.StopIndicatorTrailingSpace);

    /// <summary>Maps the raw upstream token set to the shared geometry contract.</summary>
    [Obsolete(
        "Legacy slider geometry snapshot. Use SliderValueKeys.GeometryProfile instead.",
        DiagnosticId = "M3E1005")]
    public static SliderMetrics Metrics() => DefaultMetrics;

    /// <summary>Resolves the raw upstream colour roles against a system theme snapshot.</summary>
    [Obsolete(
        "Legacy slider colour snapshot. Use SliderValueKeys.VisualColors instead.",
        DiagnosticId = "M3E1005")]
    public static SliderColors Colors(ResolvedTheme theme, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var scheme = theme.ColorScheme;
        return new SliderColors(
            SliderTokens.ActiveTrackColor.ResolveBrush(scheme, isDark),
            SliderTokens.InactiveTrackColor.ResolveBrush(scheme, isDark),
            SliderTokens.HandleColor.ResolveBrush(scheme, isDark),
            SliderTokens.StopIndicatorColor.ResolveBrush(scheme, isDark),
            // The defaults table maps the state layer to the handle role.
            SliderTokens.HandleColor.ResolveBrush(scheme, isDark),
            SliderTokens.DisabledActiveTrackColor.ResolveBrush(
                scheme, isDark, SliderTokens.DisabledActiveTrackOpacity),
            SliderTokens.DisabledInactiveTrackColor.ResolveBrush(
                scheme, isDark, SliderTokens.DisabledInactiveTrackOpacity),
            SliderTokens.DisabledHandleColor.ResolveBrush(
                scheme, isDark, SliderTokens.DisabledHandleOpacity));
    }

    /// <summary>Projects the legacy colour snapshot for one enabled state.</summary>
    [Obsolete(
        "Legacy slider visual projection. Use SliderValueKeys.VisualColors instead.",
        DiagnosticId = "M3E1005")]
    public static LegacySliderVisualColors Visuals(SliderColors colors, bool enabled)
    {
        var active = enabled ? colors.ActiveTrackColor : colors.DisabledActiveTrackColor;
        var inactive = enabled ? colors.InactiveTrackColor : colors.DisabledInactiveTrackColor;
        var handle = enabled ? colors.HandleColor : colors.DisabledHandleColor;

        // The expressive SliderDefaults.Track path uses the active track colour for its stop dot;
        // StopIndicatorColor is retained only for the compatibility record.
        return new LegacySliderVisualColors(active, inactive, handle, active, colors.StateLayerColor);
    }

}
