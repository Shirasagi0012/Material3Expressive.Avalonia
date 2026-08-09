using Avalonia.Media;
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
internal sealed record SliderColors(
    IImmutableBrush ActiveTrackColor,
    IImmutableBrush InactiveTrackColor,
    IImmutableBrush HandleColor,
    IImmutableBrush StopIndicatorColor,
    IImmutableBrush StateLayerColor,
    IImmutableBrush DisabledActiveTrackColor,
    IImmutableBrush DisabledInactiveTrackColor,
    IImmutableBrush DisabledHandleColor);

/// <summary>Render colours selected for one enabled state of a slider or range slider.</summary>
internal sealed record SliderVisualColors(
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
    public static SliderMetrics Metrics() => DefaultMetrics;

    /// <summary>Resolves the raw upstream colour roles against a system theme snapshot.</summary>
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

    public static SliderVisualColors Visuals(SliderColors colors, bool enabled)
    {
        var active = enabled ? colors.ActiveTrackColor : colors.DisabledActiveTrackColor;
        var inactive = enabled ? colors.InactiveTrackColor : colors.DisabledInactiveTrackColor;
        var handle = enabled ? colors.HandleColor : colors.DisabledHandleColor;

        // The expressive SliderDefaults.Track path uses the active track role for its stop dot;
        // StopIndicatorColor is retained in the generated record for the legacy overload.
        return new SliderVisualColors(active, inactive, handle, active, colors.StateLayerColor);
    }
}
