using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Resolved state colours selected by a Material switch renderer.</summary>
internal sealed record SwitchVisualColors(
    IImmutableBrush TrackColor,
    IImmutableBrush? TrackOutlineColor,
    IImmutableBrush HandleColor,
    IImmutableBrush StateLayerColor);

/// <summary>
/// Component-owned adapter for the Switch defaults table.
/// </summary>
/// <remarks>
/// The generated table owns the static geometry and colour roles. This adapter owns the
/// enabled/disabled and checked/unchecked projection that is specific to the control runtime.
/// </remarks>
internal static class SwitchDefaults
{
    public static SwitchDefaultsTokens Tokens(ResolvedTheme theme) => theme.SwitchDefaultsTokens;

    public static SwitchColors Colors(ResolvedTheme theme, bool isDark) =>
        theme.GetSwitchColors(isDark);

    public static SwitchVisualColors Visuals(SwitchColors colors, bool enabled, bool selected)
    {
        var track = !enabled
            ? selected ? colors.DisabledSelectedTrackColor : colors.DisabledUnselectedTrackColor
            : selected ? colors.SelectedTrackColor : colors.UnselectedTrackColor;
        var outline = selected
            ? null
            : !enabled ? colors.DisabledUnselectedTrackOutlineColor : colors.UnselectedTrackOutlineColor;
        var handle = !enabled
            ? selected ? colors.DisabledSelectedHandleColor : colors.DisabledUnselectedHandleColor
            : selected ? colors.SelectedHandleColor : colors.UnselectedHandleColor;
        var stateLayer = selected ? colors.SelectedStateLayerColor : colors.UnselectedStateLayerColor;

        return new SwitchVisualColors(track, outline, handle, stateLayer);
    }
}
