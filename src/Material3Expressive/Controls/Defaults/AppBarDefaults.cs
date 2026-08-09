using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

internal sealed record AppBarMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue ScrolledContainerColor,
    ColorTokenValue TitleColor,
    ColorTokenValue SubtitleColor,
    ColorTokenValue IconColor,
    double ContainerHeight,
    double FlexibleContainerHeight,
    double FlexibleContainerHeightWithSubtitle,
    double HorizontalSpace,
    TypographyRole TitleTextStyle,
    TypographyRole SubtitleTextStyle,
    TypographyRole FlexibleTitleTextStyle,
    TypographyRole FlexibleSubtitleTextStyle,
    bool IsStacked);

internal sealed record AppBarVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush ScrolledContainerColor,
    IImmutableBrush TitleColor,
    IImmutableBrush SubtitleColor,
    IImmutableBrush IconColor);

/// <summary>Component-owned policy for top app-bar size and color profiles.</summary>
internal static class AppBarDefaults
{
    public static AppBarMetrics Metrics(AppBarSize size) => size switch
    {
        AppBarSize.Small => new(
            AppBarTokens.ContainerColor,
            AppBarTokens.OnScrollContainerColor,
            AppBarTokens.TitleColor,
            AppBarTokens.SubtitleColor,
            AppBarTokens.TrailingIconColor,
            AppBarSmallTokens.ContainerHeight,
            AppBarSmallTokens.ContainerHeight,
            AppBarSmallTokens.ContainerHeight,
            16,
            AppBarSmallTokens.TitleFont,
            AppBarSmallTokens.SubtitleFont,
            AppBarSmallTokens.TitleFont,
            AppBarSmallTokens.SubtitleFont,
            false),
        AppBarSize.Medium => new(
            AppBarTokens.ContainerColor,
            AppBarTokens.OnScrollContainerColor,
            AppBarTokens.TitleColor,
            AppBarTokens.SubtitleColor,
            AppBarTokens.TrailingIconColor,
            AppBarMediumTokens.ContainerHeight,
            AppBarMediumFlexibleTokens.ContainerHeight,
            AppBarMediumFlexibleTokens.LargeContainerHeight,
            16,
            AppBarMediumTokens.TitleFont,
            AppBarSmallTokens.SubtitleFont,
            AppBarMediumFlexibleTokens.TitleFont,
            AppBarMediumFlexibleTokens.SubtitleFont,
            true),
        AppBarSize.Large => new(
            AppBarTokens.ContainerColor,
            AppBarTokens.OnScrollContainerColor,
            AppBarTokens.TitleColor,
            AppBarTokens.SubtitleColor,
            AppBarTokens.TrailingIconColor,
            AppBarLargeTokens.ContainerHeight,
            AppBarLargeFlexibleTokens.ContainerHeight,
            AppBarLargeFlexibleTokens.LargeContainerHeight,
            16,
            AppBarLargeTokens.TitleFont,
            AppBarSmallTokens.SubtitleFont,
            AppBarLargeFlexibleTokens.TitleFont,
            AppBarLargeFlexibleTokens.SubtitleFont,
            true),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
    };

    public static AppBarVisualColors Colors(ResolvedTheme theme, AppBarSize size, bool isDark)
    {
        var metrics = Metrics(size);
        ArgumentNullException.ThrowIfNull(theme);
        return new(
            metrics.ContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.ScrolledContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.TitleColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.SubtitleColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.IconColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}
