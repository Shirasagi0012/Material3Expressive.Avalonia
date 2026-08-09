using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

internal sealed record BottomSheetMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue ScrimColor,
    ColorTokenValue DragHandleColor,
    double ContainerElevation,
    ShapeRole ContainerShape,
    double DragHandleWidth,
    double DragHandleHeight,
    double DragHandleSpace,
    double MaxWidth,
    double ScrimOpacity,
    bool IsModal);

internal sealed record BottomSheetVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ScrimColor,
    IImmutableBrush DragHandleColor);

/// <summary>Component-owned policy for modal and standard bottom sheets.</summary>
internal static class BottomSheetDefaults
{
    public static BottomSheetMetrics Metrics(BottomSheetVariant variant) => variant switch
    {
        BottomSheetVariant.Modal => new(
            SheetBottomTokens.DockedContainerColor,
            SysColorToken.Scrim,
            SheetBottomTokens.DockedDragHandleColor,
            SheetBottomTokens.DockedModalContainerElevation,
            SheetBottomTokens.DockedContainerShape,
            SheetBottomTokens.DockedDragHandleWidth,
            SheetBottomTokens.DockedDragHandleHeight,
            22,
            640,
            ScrimTokens.ContainerOpacity,
            true),
        BottomSheetVariant.Standard => new(
            SheetBottomTokens.DockedContainerColor,
            SysColorToken.Scrim,
            SheetBottomTokens.DockedDragHandleColor,
            SheetBottomTokens.DockedModalContainerElevation,
            SheetBottomTokens.DockedContainerShape,
            SheetBottomTokens.DockedDragHandleWidth,
            SheetBottomTokens.DockedDragHandleHeight,
            22,
            640,
            0,
            false),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    public static BottomSheetVisualColors Colors(ResolvedTheme theme, BottomSheetVariant variant, bool isDark)
    {
        var metrics = Metrics(variant);
        ArgumentNullException.ThrowIfNull(theme);
        return new(
            metrics.ContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ScrimColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.DragHandleColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}

internal sealed record DialogMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue ScrimColor,
    ColorTokenValue HeadlineColor,
    ColorTokenValue SupportingTextColor,
    ColorTokenValue IconColor,
    double ContainerElevation,
    ShapeRole ContainerShape,
    double MinWidth,
    double MaxWidth,
    double Padding,
    double IconSize,
    double ScrimOpacity,
    TypographyRole HeadlineTextStyle,
    TypographyRole SupportingTextStyle);

internal sealed record DialogVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ScrimColor,
    IImmutableBrush HeadlineColor,
    IImmutableBrush SupportingTextColor,
    IImmutableBrush IconColor);

/// <summary>Component-owned policy for dialog geometry, text roles and colours.</summary>
internal static class DialogDefaults
{
    public static DialogMetrics Metrics() => new(
        DialogTokens.ContainerColor,
        SysColorToken.Scrim,
        DialogTokens.HeadlineColor,
        DialogTokens.SupportingTextColor,
        DialogTokens.IconColor,
        DialogTokens.ContainerElevation,
        DialogTokens.ContainerShape,
        280,
        560,
        24,
        DialogTokens.IconSize,
        ScrimTokens.ContainerOpacity,
        DialogTokens.HeadlineFont,
        DialogTokens.SupportingTextFont);

    public static DialogVisualColors Colors(ResolvedTheme theme, bool isDark)
    {
        var metrics = Metrics();
        ArgumentNullException.ThrowIfNull(theme);
        return new(
            metrics.ContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ScrimColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.HeadlineColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.SupportingTextColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.IconColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}

internal sealed record SnackbarMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue SupportingTextColor,
    ColorTokenValue ActionLabelTextColor,
    ColorTokenValue IconColor,
    double ContainerElevation,
    ShapeRole ContainerShape,
    double SingleLineHeight,
    double TwoLineHeight,
    double MinWidth,
    double MaxWidth,
    double HorizontalSpace,
    double IconSize,
    TypographyRole SupportingTextStyle,
    TypographyRole ActionTextStyle);

internal sealed record SnackbarVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush SupportingTextColor,
    IImmutableBrush ActionLabelTextColor,
    IImmutableBrush IconColor);

/// <summary>Component-owned policy for snackbar chrome and slot typography.</summary>
internal static class SnackbarDefaults
{
    public static SnackbarMetrics Metrics() => new(
        SnackbarTokens.ContainerColor,
        SnackbarTokens.SupportingTextColor,
        SnackbarTokens.ActionLabelTextColor,
        SnackbarTokens.IconColor,
        SnackbarTokens.ContainerElevation,
        SnackbarTokens.ContainerShape,
        SnackbarTokens.SingleLineContainerHeight,
        SnackbarTokens.TwoLinesContainerHeight,
        344,
        600,
        16,
        SnackbarTokens.IconSize,
        SnackbarTokens.SupportingTextFont,
        SnackbarTokens.ActionLabelTextFont);

    public static SnackbarVisualColors Colors(ResolvedTheme theme, bool isDark)
    {
        var metrics = Metrics();
        ArgumentNullException.ThrowIfNull(theme);
        return new(
            metrics.ContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.SupportingTextColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ActionLabelTextColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.IconColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}

internal sealed record TooltipMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue SupportingTextColor,
    ColorTokenValue TitleColor,
    ColorTokenValue ActionLabelTextColor,
    ShapeRole ContainerShape,
    double ContainerElevation,
    double MinHeight,
    double MaxWidth,
    double HorizontalSpace,
    double VerticalSpace,
    TypographyRole SupportingTextStyle,
    TypographyRole TitleTextStyle);

internal sealed record TooltipVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush SupportingTextColor,
    IImmutableBrush TitleColor,
    IImmutableBrush ActionLabelTextColor);

/// <summary>Component-owned policy for plain and rich tooltip surfaces.</summary>
internal static class TooltipDefaults
{
    public static TooltipMetrics Metrics(TooltipVariant variant) => variant switch
    {
        TooltipVariant.Plain => new(
            PlainTooltipTokens.ContainerColor,
            PlainTooltipTokens.SupportingTextColor,
            SysColorToken.InverseOnSurface,
            SysColorToken.InversePrimary,
            PlainTooltipTokens.ContainerShape,
            ElevationTokens.Level0,
            24,
            200,
            8,
            4,
            PlainTooltipTokens.SupportingTextFont,
            TypographyRole.TitleSmall),
        TooltipVariant.Rich => new(
            RichTooltipTokens.ContainerColor,
            RichTooltipTokens.SupportingTextColor,
            RichTooltipTokens.SubheadColor,
            RichTooltipTokens.ActionLabelTextColor,
            RichTooltipTokens.ContainerShape,
            RichTooltipTokens.ContainerElevation,
            40,
            320,
            16,
            12,
            RichTooltipTokens.SupportingTextFont,
            RichTooltipTokens.SubheadFont),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    public static TooltipVisualColors Colors(ResolvedTheme theme, TooltipVariant variant, bool isDark)
    {
        var metrics = Metrics(variant);
        ArgumentNullException.ThrowIfNull(theme);
        return new(
            metrics.ContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.SupportingTextColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.TitleColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ActionLabelTextColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}

internal sealed record FloatingToolbarMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue ContentColor,
    ColorTokenValue FabContainerColor,
    ColorTokenValue FabContentColor,
    double ContainerSize,
    double ContentPadding,
    double ToolbarToFabGap,
    double ScreenOffset,
    double ScrollDistanceThreshold,
    double ExpandedElevation,
    double CollapsedElevation,
    double ExpandedElevationWithFab,
    double CollapsedElevationWithFab);

internal sealed record FloatingToolbarVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ContentColor,
    IImmutableBrush FabContainerColor,
    IImmutableBrush FabContentColor);

/// <summary>Component-owned policy for standard and vibrant floating toolbars.</summary>
internal static class FloatingToolbarDefaults
{
    public static FloatingToolbarMetrics Metrics(FloatingToolbarVariant variant) => variant switch
    {
        FloatingToolbarVariant.Standard => new(
            FloatingToolbarTokens.StandardContainerColor,
            SysColorToken.OnSurface,
            SysColorToken.PrimaryContainer,
            SysColorToken.OnPrimaryContainer,
            FloatingToolbarTokens.ContainerHeight,
            FloatingToolbarTokens.ContainerLeadingSpace,
            8,
            FloatingToolbarTokens.ContainerExternalPadding,
            40,
            0,
            0,
            1,
            0),
        FloatingToolbarVariant.Vibrant => new(
            FloatingToolbarTokens.VibrantContainerColor,
            SysColorToken.OnPrimaryContainer,
            SysColorToken.TertiaryContainer,
            SysColorToken.OnTertiaryContainer,
            FloatingToolbarTokens.ContainerHeight,
            FloatingToolbarTokens.ContainerLeadingSpace,
            8,
            FloatingToolbarTokens.ContainerExternalPadding,
            40,
            0,
            0,
            1,
            0),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    public static FloatingToolbarVisualColors Colors(ResolvedTheme theme, FloatingToolbarVariant variant, bool isDark)
    {
        var metrics = Metrics(variant);
        ArgumentNullException.ThrowIfNull(theme);
        return new(
            metrics.ContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ContentColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.FabContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.FabContentColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}
