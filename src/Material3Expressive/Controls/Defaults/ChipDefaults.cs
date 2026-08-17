using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

[Obsolete(
    "Legacy chip defaults snapshot. Use ChipValueKeys and the MaterialChip ControlTheme instead.",
    DiagnosticId = "M3E1005")]
internal sealed record ChipMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue LabelTextColor,
    ColorTokenValue LeadingIconColor,
    ColorTokenValue TrailingIconColor,
    ColorTokenValue OutlineColor,
    ColorTokenValue StateLayerColor,
    ColorTokenValue SelectedContainerColor,
    ColorTokenValue SelectedLabelTextColor,
    ColorTokenValue SelectedIconColor,
    ColorTokenValue DisabledContainerColor,
    ColorTokenValue DisabledLabelTextColor,
    ColorTokenValue DisabledIconColor,
    ColorTokenValue DisabledOutlineColor,
    double DisabledContainerOpacity,
    double DisabledLabelTextOpacity,
    double DisabledOutlineOpacity,
    double ContainerHeight,
    ShapeRole ContainerShape,
    ShapeRole SelectedContainerShape,
    double OutlineWidth,
    double IconSize,
    double HorizontalSpace,
    double IconLabelSpace,
    TypographyRole LabelTextStyle,
    bool IsSelectable);

[Obsolete(
    "Legacy chip colour snapshot. Use ChipValueKeys instead.",
    DiagnosticId = "M3E1005")]
internal sealed record LegacyChipVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush LabelTextColor,
    IImmutableBrush LeadingIconColor,
    IImmutableBrush TrailingIconColor,
    IImmutableBrush OutlineColor,
    IImmutableBrush StateLayerColor,
    IImmutableBrush SelectedContainerColor,
    IImmutableBrush SelectedLabelTextColor,
    IImmutableBrush SelectedIconColor,
    IImmutableBrush DisabledContainerColor,
    IImmutableBrush DisabledLabelTextColor,
    IImmutableBrush DisabledIconColor,
    IImmutableBrush DisabledOutlineColor);

/// <summary>Component-owned policy for assist, filter, input and suggestion chips.</summary>
[Obsolete(
    "Legacy chip defaults adapter. Use ChipValueKeys and the MaterialChip ControlTheme instead.",
    DiagnosticId = "M3E1005")]
internal static class ChipDefaults
{
    private static ColorTokenValue Transparent => ColorTokenValue.Literal(Avalonia.Media.Colors.Transparent);

    [Obsolete(
        "Legacy chip metrics snapshot. Use ChipValueKeys.GeometryProfile instead.",
        DiagnosticId = "M3E1005")]
    public static ChipMetrics Metrics(ChipVariant variant) => variant switch
    {
        ChipVariant.Assist => new(
            Transparent,
            AssistChipTokens.LabelTextColor,
            AssistChipTokens.IconColor,
            AssistChipTokens.IconColor,
            AssistChipTokens.FlatOutlineColor,
            AssistChipTokens.LabelTextColor,
            ChipsTokens.SelectedContainerColor,
            ChipsTokens.SelectedLabelTextColor,
            ChipsTokens.SelectedLeadingIconColor,
            Transparent,
            AssistChipTokens.DisabledLabelTextColor,
            AssistChipTokens.DisabledIconColor,
            AssistChipTokens.FlatDisabledOutlineColor,
            1,
            AssistChipTokens.DisabledLabelTextOpacity,
            AssistChipTokens.FlatDisabledOutlineOpacity,
            AssistChipTokens.ContainerHeight,
            AssistChipTokens.ContainerShape,
            ChipsTokens.SelectedShape,
            AssistChipTokens.FlatOutlineWidth,
            AssistChipTokens.IconSize,
            16,
            8,
            AssistChipTokens.LabelTextFont,
            false),
        ChipVariant.Filter => new(
            Transparent,
            FilterChipTokens.UnselectedLabelTextColor,
            FilterChipTokens.UnselectedLeadingIconColor,
            FilterChipTokens.UnselectedTrailingIconColor,
            AssistChipTokens.FlatOutlineColor,
            FilterChipTokens.UnselectedLabelTextColor,
            FilterChipTokens.FlatSelectedContainerColor,
            FilterChipTokens.SelectedLabelTextColor,
            FilterChipTokens.SelectedLeadingIconColor,
            Transparent,
            AssistChipTokens.DisabledLabelTextColor,
            AssistChipTokens.DisabledIconColor,
            AssistChipTokens.FlatDisabledOutlineColor,
            1,
            AssistChipTokens.DisabledLabelTextOpacity,
            AssistChipTokens.FlatDisabledOutlineOpacity,
            AssistChipTokens.ContainerHeight,
            AssistChipTokens.ContainerShape,
            ChipsTokens.SelectedShape,
            AssistChipTokens.FlatOutlineWidth,
            AssistChipTokens.IconSize,
            16,
            8,
            AssistChipTokens.LabelTextFont,
            true),
        ChipVariant.Input => new(
            Transparent,
            InputChipTokens.UnselectedLabelTextColor,
            InputChipTokens.UnselectedLeadingIconColor,
            InputChipTokens.UnselectedTrailingIconColor,
            AssistChipTokens.FlatOutlineColor,
            InputChipTokens.UnselectedLabelTextColor,
            InputChipTokens.SelectedContainerColor,
            InputChipTokens.SelectedLabelTextColor,
            InputChipTokens.SelectedLeadingIconColor,
            Transparent,
            AssistChipTokens.DisabledLabelTextColor,
            AssistChipTokens.DisabledIconColor,
            AssistChipTokens.FlatDisabledOutlineColor,
            1,
            AssistChipTokens.DisabledLabelTextOpacity,
            AssistChipTokens.FlatDisabledOutlineOpacity,
            AssistChipTokens.ContainerHeight,
            AssistChipTokens.ContainerShape,
            ChipsTokens.SelectedShape,
            AssistChipTokens.FlatOutlineWidth,
            AssistChipTokens.IconSize,
            16,
            8,
            AssistChipTokens.LabelTextFont,
            true),
        ChipVariant.Suggestion => new(
            Transparent,
            SuggestionChipTokens.LabelTextColor,
            SuggestionChipTokens.LeadingIconColor,
            SuggestionChipTokens.LeadingIconColor,
            AssistChipTokens.FlatOutlineColor,
            SuggestionChipTokens.LabelTextColor,
            ChipsTokens.SelectedContainerColor,
            ChipsTokens.SelectedLabelTextColor,
            ChipsTokens.SelectedLeadingIconColor,
            Transparent,
            AssistChipTokens.DisabledLabelTextColor,
            AssistChipTokens.DisabledIconColor,
            AssistChipTokens.FlatDisabledOutlineColor,
            1,
            AssistChipTokens.DisabledLabelTextOpacity,
            AssistChipTokens.FlatDisabledOutlineOpacity,
            AssistChipTokens.ContainerHeight,
            AssistChipTokens.ContainerShape,
            ChipsTokens.SelectedShape,
            AssistChipTokens.FlatOutlineWidth,
            AssistChipTokens.IconSize,
            16,
            8,
            AssistChipTokens.LabelTextFont,
            false),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    [Obsolete(
        "Legacy chip colour projection. Use ChipValueKeys visual endpoints instead.",
        DiagnosticId = "M3E1005")]
    public static LegacyChipVisualColors Colors(ResolvedTheme theme, ChipVariant variant, bool isDark)
    {
        var metrics = Metrics(variant);
        ArgumentNullException.ThrowIfNull(theme);
        var scheme = theme.ColorScheme;
        return new(
            metrics.ContainerColor.ResolveBrush(scheme, isDark),
            metrics.LabelTextColor.ResolveBrush(scheme, isDark),
            metrics.LeadingIconColor.ResolveBrush(scheme, isDark),
            metrics.TrailingIconColor.ResolveBrush(scheme, isDark),
            metrics.OutlineColor.ResolveBrush(scheme, isDark),
            metrics.StateLayerColor.ResolveBrush(scheme, isDark),
            metrics.SelectedContainerColor.ResolveBrush(scheme, isDark),
            metrics.SelectedLabelTextColor.ResolveBrush(scheme, isDark),
            metrics.SelectedIconColor.ResolveBrush(scheme, isDark),
            metrics.DisabledContainerColor.ResolveBrush(scheme, isDark, metrics.DisabledContainerOpacity),
            metrics.DisabledLabelTextColor.ResolveBrush(scheme, isDark, metrics.DisabledLabelTextOpacity),
            metrics.DisabledIconColor.ResolveBrush(scheme, isDark, metrics.DisabledLabelTextOpacity),
            metrics.DisabledOutlineColor.ResolveBrush(scheme, isDark, metrics.DisabledOutlineOpacity));
    }
}
