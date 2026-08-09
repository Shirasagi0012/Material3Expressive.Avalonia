using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Geometry consumed by the self-rendering Material radio button.</summary>
internal sealed record RadioButtonMetrics(
    double IconSize,
    double StateLayerSize,
    double RingWidth,
    double DotSize);

/// <summary>Theme-resolved colour roles used by a Material radio button.</summary>
internal sealed record RadioButtonColors(
    IImmutableBrush SelectedColor,
    IImmutableBrush UnselectedColor,
    IImmutableBrush DisabledSelectedColor,
    IImmutableBrush DisabledUnselectedColor);

/// <summary>Maps the raw radio-button token set to renderer semantics.</summary>
internal static class RadioButtonDefaults
{
    private static readonly RadioButtonMetrics DefaultMetrics = new(
        RadioButtonTokens.IconSize,
        RadioButtonTokens.StateLayerSize,
        // RadioButton.kt draws these values directly; they are not token-table entries.
        RingWidth: 2,
        DotSize: 10);

    public static RadioButtonMetrics Metrics() => DefaultMetrics;

    public static RadioButtonColors Colors(ResolvedTheme theme, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var scheme = theme.ColorScheme;
        return new RadioButtonColors(
            RadioButtonTokens.SelectedIconColor.ResolveBrush(scheme, isDark),
            RadioButtonTokens.UnselectedIconColor.ResolveBrush(scheme, isDark),
            RadioButtonTokens.DisabledSelectedIconColor.ResolveBrush(
                scheme, isDark, RadioButtonTokens.DisabledSelectedIconOpacity),
            RadioButtonTokens.DisabledUnselectedIconColor.ResolveBrush(
                scheme, isDark, RadioButtonTokens.DisabledUnselectedIconOpacity));
    }
}
