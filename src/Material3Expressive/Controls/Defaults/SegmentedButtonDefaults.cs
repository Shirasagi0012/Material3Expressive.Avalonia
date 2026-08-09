using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Geometry, typography and spacing consumed by segmented-button controls.</summary>
internal sealed record SegmentedButtonMetrics(
    double ContainerHeight,
    ShapeRole ContainerShape,
    TypographyRole LabelTextStyle,
    double IconSize,
    double IconSpacing,
    double HorizontalContentPadding,
    double VerticalContentPadding,
    double OutlineWidth);

/// <summary>Theme-resolved state colours shared by a segment and its row.</summary>
internal sealed record SegmentedButtonColors(
    IImmutableBrush ActiveContainerColor,
    IImmutableBrush ActiveContentColor,
    IImmutableBrush BorderColor,
    IImmutableBrush InactiveContainerColor,
    IImmutableBrush InactiveContentColor,
    IImmutableBrush DisabledActiveContainerColor,
    IImmutableBrush DisabledActiveContentColor,
    IImmutableBrush DisabledInactiveContainerColor,
    IImmutableBrush DisabledInactiveContentColor,
    IImmutableBrush DisabledBorderColor,
    double DisabledContentOpacity,
    double DisabledBorderOpacity);

/// <summary>Resolves the raw outlined segmented-button token set for the self-rendering controls.</summary>
internal static class SegmentedButtonDefaults
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Avalonia.Media.Colors.Transparent);

    private static readonly SegmentedButtonMetrics DefaultMetrics = new(
        OutlinedSegmentedButtonTokens.ContainerHeight,
        OutlinedSegmentedButtonTokens.Shape,
        OutlinedSegmentedButtonTokens.LabelTextFont,
        OutlinedSegmentedButtonTokens.IconSize,
        // SegmentedButtonDefaults delegates spacing/padding to ButtonDefaults in Compose.
        IconSpacing: 8,
        HorizontalContentPadding: 12,
        VerticalContentPadding: 8,
        OutlinedSegmentedButtonTokens.OutlineWidth);

    public static SegmentedButtonMetrics Metrics() => DefaultMetrics;

    public static SegmentedButtonColors Colors(ResolvedTheme theme, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);

        var scheme = theme.ColorScheme;
        return new SegmentedButtonColors(
            OutlinedSegmentedButtonTokens.SelectedContainerColor.ResolveBrush(scheme, isDark),
            OutlinedSegmentedButtonTokens.SelectedLabelTextColor.ResolveBrush(scheme, isDark),
            OutlinedSegmentedButtonTokens.OutlineColor.ResolveBrush(scheme, isDark),
            Transparent,
            OutlinedSegmentedButtonTokens.UnselectedLabelTextColor.ResolveBrush(scheme, isDark),
            OutlinedSegmentedButtonTokens.SelectedContainerColor.ResolveBrush(scheme, isDark),
            OutlinedSegmentedButtonTokens.DisabledLabelTextColor.ResolveBrush(
                scheme, isDark, OutlinedSegmentedButtonTokens.DisabledLabelTextOpacity),
            Transparent,
            OutlinedSegmentedButtonTokens.DisabledLabelTextColor.ResolveBrush(
                scheme, isDark, OutlinedSegmentedButtonTokens.DisabledLabelTextOpacity),
            OutlinedSegmentedButtonTokens.DisabledOutlineColor.ResolveBrush(
                scheme, isDark, OutlinedSegmentedButtonTokens.DisabledOutlineOpacity),
            OutlinedSegmentedButtonTokens.DisabledLabelTextOpacity,
            OutlinedSegmentedButtonTokens.DisabledOutlineOpacity);
    }
}
