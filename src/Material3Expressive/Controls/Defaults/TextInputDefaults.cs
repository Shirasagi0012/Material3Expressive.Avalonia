using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

internal sealed record TextFieldMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue DisabledContainerColor,
    double DisabledContainerOpacity,
    ColorTokenValue InputColor,
    ColorTokenValue PlaceholderColor,
    ColorTokenValue LabelColor,
    ColorTokenValue FocusLabelColor,
    ColorTokenValue SupportingColor,
    ColorTokenValue LeadingIconColor,
    ColorTokenValue TrailingIconColor,
    ColorTokenValue OutlineColor,
    ColorTokenValue FocusOutlineColor,
    ColorTokenValue ErrorColor,
    ColorTokenValue ErrorInputColor,
    ColorTokenValue DisabledInputColor,
    ColorTokenValue DisabledLabelColor,
    ColorTokenValue DisabledSupportingColor,
    ColorTokenValue DisabledIconColor,
    ColorTokenValue DisabledOutlineColor,
    double DisabledContentOpacity,
    double DisabledOutlineOpacity,
    ShapeRole ContainerShape,
    double ContainerHeight,
    double OutlineWidth,
    double FocusOutlineWidth,
    double IconSize,
    double HorizontalSpace,
    TypographyRole InputTextStyle,
    TypographyRole FloatingLabelTextStyle,
    TypographyRole SupportingTextStyle,
    bool IsFilled);

internal sealed record TextFieldVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush DisabledContainerColor,
    IImmutableBrush InputColor,
    IImmutableBrush PlaceholderColor,
    IImmutableBrush LabelColor,
    IImmutableBrush FocusLabelColor,
    IImmutableBrush SupportingColor,
    IImmutableBrush LeadingIconColor,
    IImmutableBrush TrailingIconColor,
    IImmutableBrush OutlineColor,
    IImmutableBrush FocusOutlineColor,
    IImmutableBrush ErrorColor,
    IImmutableBrush ErrorInputColor,
    IImmutableBrush DisabledInputColor,
    IImmutableBrush DisabledLabelColor,
    IImmutableBrush DisabledSupportingColor,
    IImmutableBrush DisabledIconColor,
    IImmutableBrush DisabledOutlineColor);

/// <summary>Component-owned policy for filled and outlined text fields.</summary>
internal static class TextFieldDefaults
{
    private static ColorTokenValue Transparent => ColorTokenValue.Literal(Avalonia.Media.Colors.Transparent);

    public static TextFieldMetrics Metrics(TextFieldVariant variant) => variant switch
    {
        TextFieldVariant.Filled => new(
            FilledTextFieldTokens.ContainerColor,
            FilledTextFieldTokens.ContainerColor,
            1,
            FilledTextFieldTokens.InputColor,
            FilledTextFieldTokens.InputPlaceholderColor,
            FilledTextFieldTokens.LabelColor,
            FilledTextFieldTokens.FocusLabelColor,
            FilledTextFieldTokens.SupportingColor,
            FilledTextFieldTokens.LeadingIconColor,
            FilledTextFieldTokens.TrailingIconColor,
            FilledTextFieldTokens.ActiveIndicatorColor,
            FilledTextFieldTokens.FocusActiveIndicatorColor,
            FilledTextFieldTokens.ErrorLabelColor,
            FilledTextFieldTokens.ErrorInputColor,
            FilledTextFieldTokens.DisabledInputColor,
            FilledTextFieldTokens.DisabledLabelColor,
            FilledTextFieldTokens.DisabledSupportingColor,
            FilledTextFieldTokens.DisabledLeadingIconColor,
            FilledTextFieldTokens.DisabledActiveIndicatorColor,
            FilledTextFieldTokens.DisabledInputOpacity,
            FilledTextFieldTokens.DisabledActiveIndicatorOpacity,
            FilledTextFieldTokens.ContainerShape,
            56,
            FilledTextFieldTokens.ActiveIndicatorHeight,
            FilledTextFieldTokens.FocusActiveIndicatorHeight,
            FilledTextFieldTokens.LeadingIconSize,
            16,
            FilledTextFieldTokens.InputFont,
            TypographyRole.BodySmall,
            FilledTextFieldTokens.SupportingFont,
            true),
        TextFieldVariant.Outlined => new(
            Transparent,
            Transparent,
            1,
            OutlinedTextFieldTokens.InputColor,
            OutlinedTextFieldTokens.InputPlaceholderColor,
            OutlinedTextFieldTokens.LabelColor,
            OutlinedTextFieldTokens.FocusLabelColor,
            OutlinedTextFieldTokens.SupportingColor,
            OutlinedTextFieldTokens.LeadingIconColor,
            OutlinedTextFieldTokens.TrailingIconColor,
            OutlinedTextFieldTokens.OutlineColor,
            OutlinedTextFieldTokens.FocusOutlineColor,
            OutlinedTextFieldTokens.ErrorLabelColor,
            OutlinedTextFieldTokens.ErrorInputColor,
            OutlinedTextFieldTokens.DisabledInputColor,
            OutlinedTextFieldTokens.DisabledLabelColor,
            OutlinedTextFieldTokens.DisabledSupportingColor,
            OutlinedTextFieldTokens.DisabledLeadingIconColor,
            OutlinedTextFieldTokens.DisabledOutlineColor,
            OutlinedTextFieldTokens.DisabledInputOpacity,
            OutlinedTextFieldTokens.DisabledOutlineOpacity,
            OutlinedTextFieldTokens.ContainerShape,
            OutlinedTextFieldTokens.ContainerHeight,
            OutlinedTextFieldTokens.OutlineWidth,
            OutlinedTextFieldTokens.FocusOutlineWidth,
            OutlinedTextFieldTokens.LeadingIconSize,
            16,
            OutlinedTextFieldTokens.InputFont,
            TypographyRole.BodySmall,
            OutlinedTextFieldTokens.SupportingFont,
            false),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    public static TextFieldVisualColors Colors(ResolvedTheme theme, TextFieldVariant variant, bool isDark)
    {
        var metrics = Metrics(variant);
        ArgumentNullException.ThrowIfNull(theme);
        var scheme = theme.ColorScheme;
        return new(
            metrics.ContainerColor.ResolveBrush(scheme, isDark),
            metrics.DisabledContainerColor.ResolveBrush(scheme, isDark, metrics.DisabledContainerOpacity),
            metrics.InputColor.ResolveBrush(scheme, isDark),
            metrics.PlaceholderColor.ResolveBrush(scheme, isDark),
            metrics.LabelColor.ResolveBrush(scheme, isDark),
            metrics.FocusLabelColor.ResolveBrush(scheme, isDark),
            metrics.SupportingColor.ResolveBrush(scheme, isDark),
            metrics.LeadingIconColor.ResolveBrush(scheme, isDark),
            metrics.TrailingIconColor.ResolveBrush(scheme, isDark),
            metrics.OutlineColor.ResolveBrush(scheme, isDark),
            metrics.FocusOutlineColor.ResolveBrush(scheme, isDark),
            metrics.ErrorColor.ResolveBrush(scheme, isDark),
            metrics.ErrorInputColor.ResolveBrush(scheme, isDark),
            metrics.DisabledInputColor.ResolveBrush(scheme, isDark, metrics.DisabledContentOpacity),
            metrics.DisabledLabelColor.ResolveBrush(scheme, isDark, metrics.DisabledContentOpacity),
            metrics.DisabledSupportingColor.ResolveBrush(scheme, isDark, metrics.DisabledContentOpacity),
            metrics.DisabledIconColor.ResolveBrush(scheme, isDark, metrics.DisabledContentOpacity),
            metrics.DisabledOutlineColor.ResolveBrush(scheme, isDark, metrics.DisabledOutlineOpacity));
    }
}

internal sealed record SearchBarMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue InputTextColor,
    ColorTokenValue SupportingTextColor,
    ColorTokenValue LeadingIconColor,
    ColorTokenValue TrailingIconColor,
    ColorTokenValue ResultsContainerColor,
    ColorTokenValue DividerColor,
    double ContainerElevation,
    double ContainerHeight,
    ShapeRole ContainerShape,
    ShapeRole ResultsContainerShape,
    double HorizontalSpace,
    double IconSize,
    double IconLabelSpace,
    double DividerHeight,
    TypographyRole InputTextStyle);

internal sealed record SearchBarVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush InputTextColor,
    IImmutableBrush SupportingTextColor,
    IImmutableBrush LeadingIconColor,
    IImmutableBrush TrailingIconColor,
    IImmutableBrush ResultsContainerColor,
    IImmutableBrush DividerColor);

/// <summary>Component-owned policy for the search bar and its results surface.</summary>
internal static class SearchBarDefaults
{
    public static SearchBarMetrics Metrics() => new(
        SearchBarTokens.ContainerColor,
        SearchBarTokens.InputTextColor,
        SearchBarTokens.SupportingTextColor,
        SearchBarTokens.LeadingIconColor,
        SearchBarTokens.TrailingIconColor,
        SearchViewTokens.ContainerColor,
        SearchViewTokens.DividerColor,
        SearchBarTokens.ContainerElevation,
        SearchBarTokens.ContainerHeight,
        SearchBarTokens.ContainerShape,
        SearchViewTokens.DockedContainerShape,
        16,
        24,
        16,
        1,
        SearchBarTokens.InputTextFont);

    public static SearchBarVisualColors Colors(ResolvedTheme theme, bool isDark)
    {
        var metrics = Metrics();
        ArgumentNullException.ThrowIfNull(theme);
        var scheme = theme.ColorScheme;
        return new(
            metrics.ContainerColor.ResolveBrush(scheme, isDark),
            metrics.InputTextColor.ResolveBrush(scheme, isDark),
            metrics.SupportingTextColor.ResolveBrush(scheme, isDark),
            metrics.LeadingIconColor.ResolveBrush(scheme, isDark),
            metrics.TrailingIconColor.ResolveBrush(scheme, isDark),
            metrics.ResultsContainerColor.ResolveBrush(scheme, isDark),
            metrics.DividerColor.ResolveBrush(scheme, isDark));
    }
}
