using Avalonia.Media;
using System.ComponentModel;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// The geometry values a Material switch consumes at render time.
/// </summary>
/// <remarks>
/// This is deliberately a component-owned semantic record rather than a generated defaults
/// snapshot.  The generated <see cref="SwitchTokens"/> class is a verbatim transcription of the
/// upstream token set; the mapping (including the fact that the expressive control uses the width
/// tokens for its handle size) belongs here with the control policy.
/// </remarks>
[Obsolete(
    "Legacy switch geometry snapshot. Use SwitchValueKeys.GeometryProfile instead.",
    DiagnosticId = "M3E1005")]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed record SwitchMetrics(
    double TrackWidth,
    double TrackHeight,
    double TrackOutlineWidth,
    double UnselectedHandleSize,
    double SelectedHandleSize,
    double PressedHandleSize,
    double IconHandleSize,
    double IconSize,
    double StateLayerSize);

/// <summary>
/// Theme-resolved colour roles used by a Material switch.
/// </summary>
/// <remarks>
/// The raw roles are resolved against the current system colour scheme when a control's theme
/// snapshot changes.  Disabled entries carry the opacity specified by <see cref="SwitchTokens"/>
/// so render code never has to know how the upstream role/alpha pair is assembled.
/// </remarks>
[Obsolete(
    "Legacy switch colour snapshot. Use SwitchValueKeys.VisualColors instead.",
    DiagnosticId = "M3E1005")]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed record SwitchColors(
    IImmutableBrush SelectedTrackColor,
    IImmutableBrush SelectedHandleColor,
    IImmutableBrush SelectedIconColor,
    IImmutableBrush UnselectedTrackColor,
    IImmutableBrush UnselectedTrackOutlineColor,
    IImmutableBrush UnselectedHandleColor,
    IImmutableBrush UnselectedIconColor,
    IImmutableBrush SelectedStateLayerColor,
    IImmutableBrush UnselectedStateLayerColor,
    IImmutableBrush DisabledSelectedTrackColor,
    IImmutableBrush DisabledSelectedHandleColor,
    IImmutableBrush DisabledUnselectedTrackColor,
    IImmutableBrush DisabledUnselectedTrackOutlineColor,
    IImmutableBrush DisabledUnselectedHandleColor);

/// <summary>Resolved state colours selected by the legacy switch renderer.</summary>
[Obsolete(
    "Legacy switch visual projection. Use SwitchValueKeys.VisualColors instead.",
    DiagnosticId = "M3E1005")]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed record LegacySwitchVisualColors(
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
[Obsolete(
    "Legacy switch defaults adapter. Use SwitchValueKeys and MaterialValueKey<T> resources instead.",
    DiagnosticId = "M3E1005")]
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class SwitchDefaults
{
    private static readonly SwitchMetrics DefaultMetrics = new(
        SwitchTokens.TrackWidth,
        SwitchTokens.TrackHeight,
        SwitchTokens.TrackOutlineWidth,
        SwitchTokens.UnselectedHandleWidth,
        SwitchTokens.SelectedHandleWidth,
        SwitchTokens.PressedHandleWidth,
        SwitchTokens.IconHandleWidth,
        SwitchTokens.SelectedIconSize,
        SwitchTokens.StateLayerSize);

    /// <summary>Maps the raw upstream token set to the geometry consumed by the control.</summary>
    [Obsolete(
        "Legacy switch geometry snapshot. Use SwitchValueKeys.GeometryProfile instead.",
        DiagnosticId = "M3E1005")]
    public static SwitchMetrics Metrics() => DefaultMetrics;

    /// <summary>Resolves the raw upstream colour roles against a system theme snapshot.</summary>
    [Obsolete(
        "Legacy switch colour snapshot. Use SwitchValueKeys.VisualColors instead.",
        DiagnosticId = "M3E1005")]
    public static SwitchColors Colors(ResolvedTheme theme, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var scheme = theme.ColorScheme;
        return new SwitchColors(
            SwitchTokens.SelectedTrackColor.ResolveBrush(scheme, isDark),
            SwitchTokens.SelectedHandleColor.ResolveBrush(scheme, isDark),
            SwitchTokens.SelectedIconColor.ResolveBrush(scheme, isDark),
            SwitchTokens.UnselectedTrackColor.ResolveBrush(scheme, isDark),
            SwitchTokens.UnselectedTrackOutlineColor.ResolveBrush(scheme, isDark),
            SwitchTokens.UnselectedHandleColor.ResolveBrush(scheme, isDark),
            SwitchTokens.UnselectedIconColor.ResolveBrush(scheme, isDark),
            // The upstream defaults use the track role as the state-layer role.
            SwitchTokens.SelectedTrackColor.ResolveBrush(scheme, isDark),
            SwitchTokens.UnselectedTrackColor.ResolveBrush(scheme, isDark),
            SwitchTokens.DisabledSelectedTrackColor.ResolveBrush(
                scheme, isDark, SwitchTokens.DisabledTrackOpacity),
            SwitchTokens.DisabledSelectedHandleColor.ResolveBrush(scheme, isDark),
            SwitchTokens.DisabledUnselectedTrackColor.ResolveBrush(
                scheme, isDark, SwitchTokens.DisabledTrackOpacity),
            SwitchTokens.DisabledUnselectedTrackOutlineColor.ResolveBrush(
                scheme, isDark, SwitchTokens.DisabledTrackOpacity),
            SwitchTokens.DisabledUnselectedHandleColor.ResolveBrush(
                scheme, isDark, SwitchTokens.DisabledUnselectedHandleOpacity));
    }

    [Obsolete(
        "Legacy switch visual projection. Use SwitchValueKeys.VisualColors instead.",
        DiagnosticId = "M3E1005")]
    public static LegacySwitchVisualColors Visuals(SwitchColors colors, bool enabled, bool selected)
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

        return new LegacySwitchVisualColors(track, outline, handle, stateLayer);
    }
}
