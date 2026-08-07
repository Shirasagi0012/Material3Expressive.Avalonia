using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls.SliderRuntime;

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
    public static SliderDefaultsTokens Tokens(ResolvedTheme theme) => theme.SliderDefaultsTokens;

    public static SliderColors Colors(ResolvedTheme theme, bool isDark) =>
        theme.GetSliderColors(isDark);

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
