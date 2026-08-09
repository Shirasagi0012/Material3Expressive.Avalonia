using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

internal sealed record PickerMetrics(
    ColorTokenValue ContainerColor,
    ColorTokenValue HeadlineColor,
    ColorTokenValue SupportingTextColor,
    ColorTokenValue WeekdayColor,
    ColorTokenValue DayColor,
    ColorTokenValue SelectedDayColor,
    ColorTokenValue SelectedDayContainerColor,
    ColorTokenValue TodayOutlineColor,
    ColorTokenValue DividerColor,
    double ContainerElevation,
    ShapeRole ContainerShape,
    double DaySize,
    ShapeRole DayShape,
    double TodayOutlineWidth,
    double ContainerWidth,
    TypographyRole HeadlineTextStyle,
    TypographyRole SupportingTextStyle,
    TypographyRole DayTextStyle,
    bool IsTime);

internal sealed record PickerVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush HeadlineColor,
    IImmutableBrush SupportingTextColor,
    IImmutableBrush WeekdayColor,
    IImmutableBrush DayColor,
    IImmutableBrush SelectedDayColor,
    IImmutableBrush SelectedDayContainerColor,
    IImmutableBrush TodayOutlineColor,
    IImmutableBrush DividerColor);

/// <summary>Component-owned policy shared by date and time pickers.</summary>
internal static class PickerDefaults
{
    public static PickerMetrics Metrics(PickerVariant variant) => variant switch
    {
        PickerVariant.Date => new(
            DatePickerModalTokens.ContainerColor,
            DatePickerModalTokens.HeaderHeadlineColor,
            DatePickerModalTokens.HeaderSupportingTextColor,
            DatePickerModalTokens.WeekdaysLabelTextColor,
            DatePickerModalTokens.DateUnselectedLabelTextColor,
            DatePickerModalTokens.DateSelectedLabelTextColor,
            DatePickerModalTokens.DateSelectedContainerColor,
            DatePickerModalTokens.DateTodayContainerOutlineColor,
            SysColorToken.OutlineVariant,
            DatePickerModalTokens.ContainerElevation,
            DatePickerModalTokens.ContainerShape,
            DatePickerModalTokens.DateContainerWidth,
            DatePickerModalTokens.DateContainerShape,
            DatePickerModalTokens.DateTodayContainerOutlineWidth,
            DatePickerModalTokens.ContainerWidth,
            DatePickerModalTokens.HeaderHeadlineFont,
            DatePickerModalTokens.HeaderSupportingTextFont,
            DatePickerModalTokens.DateLabelTextFont,
            false),
        PickerVariant.Time => new(
            TimePickerTokens.ContainerColor,
            TimePickerTokens.HeadlineColor,
            TimePickerTokens.HeadlineColor,
            TimePickerTokens.ClockDialUnselectedLabelTextColor,
            TimePickerTokens.ClockDialUnselectedLabelTextColor,
            TimePickerTokens.ClockDialSelectedLabelTextColor,
            TimePickerTokens.ClockDialSelectorCenterContainerColor,
            TimePickerTokens.ClockDialSelectorTrackContainerColor,
            SysColorToken.OutlineVariant,
            TimePickerTokens.ContainerElevation,
            TimePickerTokens.ContainerShape,
            48,
            DatePickerModalTokens.DateContainerShape,
            TimePickerTokens.ClockDialSelectorTrackContainerWidth,
            328,
            TimePickerTokens.TimeSelectorLabelTextFont,
            TimePickerTokens.HeadlineFont,
            TimePickerTokens.ClockDialLabelTextFont,
            true),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };

    public static PickerVisualColors Colors(ResolvedTheme theme, PickerVariant variant, bool isDark)
    {
        var metrics = Metrics(variant);
        ArgumentNullException.ThrowIfNull(theme);
        return new(
            metrics.ContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.HeadlineColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.SupportingTextColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.WeekdayColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.DayColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.SelectedDayColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.SelectedDayContainerColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.TodayOutlineColor.ResolveBrush(theme.ColorScheme, isDark),
            metrics.DividerColor.ResolveBrush(theme.ColorScheme, isDark));
    }
}
