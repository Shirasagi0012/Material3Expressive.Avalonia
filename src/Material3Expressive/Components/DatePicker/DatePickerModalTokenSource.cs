// Translated from AndroidX DatePickerModalTokens.kt, fixed baseline f3ed195f0c9e.
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).

using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class DatePickerModalTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object> {
        [Comp.DatePickerModal.TitlePadding] = new Thickness(24, 16, 12, 0),
        [Comp.DatePickerModal.HeadlinePadding] = new Thickness(24, 0, 12, 12),
        [Comp.DatePickerModal.ModeToggleSize] = 48.0d,
        [Comp.DatePickerModal.MonthNavigationHeight] = 56.0d,
        [Comp.DatePickerModal.CalendarHorizontalPadding] = new Thickness(12, 0),
        [Comp.DatePickerModal.NavigationButtonSize] = 48.0d,
        [Comp.DatePickerModal.MonthButtonMinWidth] = 48.0d,
        [Comp.DatePickerModal.MonthButtonHeight] = 48.0d,
        [Comp.DatePickerModal.MonthButtonHorizontalPadding] = new Thickness(8, 0),
        [Comp.DatePickerModal.WeekdayRowHeight] = 48.0d,
        [Comp.DatePickerModal.DayCellWidth] = 48.0d,
        [Comp.DatePickerModal.DayCellHeight] = 48.0d,
        [Comp.DatePickerModal.RangeIndicatorTopOffset] = 4.0d,
        [Comp.DatePickerModal.YearGridWidth] = 336.0d,
        [Comp.DatePickerModal.YearGridVerticalMargin] = new Thickness(0, 16),
        [Comp.DatePickerModal.YearCellWidth] = 112.0d,
        [Comp.DatePickerModal.YearCellHeight] = 52.0d,
        [Comp.DatePickerModal.YearScrollHeight] = 335.0d,
        [Comp.DatePickerModal.RangeMonthTitleMargin] = new Thickness(24, 20, 0, 8),
        [Comp.DatePickerModal.InputMargin] = new Thickness(24, 24, 24, 16),
        [Comp.DatePickerModal.InputSpacing] = 8.0d,
        [Comp.DatePickerModal.SingleInputWidth] = 312.0d,
        [Comp.DatePickerModal.RangeInputWidth] = 152.0d,
        [Comp.DatePickerModal.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerHigh),
        [Comp.DatePickerModal.ContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.DatePickerModal.ContainerHeight] = 568.0d,
        [Comp.DatePickerModal.ContainerShape] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.DatePickerModal.ContainerWidth] = 360.0d, [Comp.DatePickerModal.DateContainerHeight] = 40.0d,
        [Comp.DatePickerModal.DateContainerShape] = new TokenRef(Sys.Shape.Full),
        [Comp.DatePickerModal.DateContainerWidth] = 40.0d,
        [Comp.DatePickerModal.DateLabelTextFont] = new TokenRef(Sys.TypeScale.BodyLarge.Style),
        [Comp.DatePickerModal.DateSelectedContainerColor] = new TokenRef(Sys.Color.Primary),
        [Comp.DatePickerModal.DateSelectedLabelTextColor] = new TokenRef(Sys.Color.OnPrimary),
        [Comp.DatePickerModal.DateStateLayerHeight] = 40.0d,
        [Comp.DatePickerModal.DateStateLayerShape] = new TokenRef(Sys.Shape.Full),
        [Comp.DatePickerModal.DateStateLayerWidth] = 40.0d,
        [Comp.DatePickerModal.DateTodayContainerOutlineColor] = new TokenRef(Sys.Color.Primary),
        [Comp.DatePickerModal.DateTodayContainerOutlineWidth] = 1.0d,
        [Comp.DatePickerModal.DateTodayLabelTextColor] = new TokenRef(Sys.Color.Primary),
        [Comp.DatePickerModal.DateUnselectedLabelTextColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.DatePickerModal.HeaderContainerHeight] = 120.0d, [Comp.DatePickerModal.HeaderContainerWidth] = 360.0d,
        [Comp.DatePickerModal.HeaderHeadlineColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.DatePickerModal.HeaderHeadlineFont] = new TokenRef(Sys.TypeScale.HeadlineLarge.Style),
        [Comp.DatePickerModal.HeaderSupportingTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.DatePickerModal.HeaderSupportingTextFont] = new TokenRef(Sys.TypeScale.LabelLarge.Style),
        [Comp.DatePickerModal.RangeSelectionActiveIndicatorContainerColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.DatePickerModal.RangeSelectionActiveIndicatorContainerHeight] = 40.0d,
        [Comp.DatePickerModal.RangeSelectionActiveIndicatorContainerShape] = new TokenRef(Sys.Shape.Full),
        [Comp.DatePickerModal.RangeSelectionContainerElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.DatePickerModal.RangeSelectionContainerShape] = new TokenRef(Sys.Shape.None),
        [Comp.DatePickerModal.SelectionDateInRangeLabelTextColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.DatePickerModal.RangeSelectionHeaderContainerHeight] = 128.0d,
        [Comp.DatePickerModal.RangeSelectionHeaderHeadlineFont] = new TokenRef(Sys.TypeScale.TitleLarge.Style),
        [Comp.DatePickerModal.RangeSelectionMonthSubheadColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.DatePickerModal.RangeSelectionMonthSubheadFont] = new TokenRef(Sys.TypeScale.TitleSmall.Style),
        [Comp.DatePickerModal.WeekdaysLabelTextColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.DatePickerModal.WeekdaysLabelTextFont] = new TokenRef(Sys.TypeScale.BodyLarge.Style),
        [Comp.DatePickerModal.SelectionYearContainerHeight] = 36.0d, [Comp.DatePickerModal.SelectionYearContainerWidth] = 72.0d,
        [Comp.DatePickerModal.SelectionYearLabelTextFont] = new TokenRef(Sys.TypeScale.BodyLarge.Style),
        [Comp.DatePickerModal.SelectionYearSelectedContainerColor] = new TokenRef(Sys.Color.Primary),
        [Comp.DatePickerModal.SelectionYearSelectedLabelTextColor] = new TokenRef(Sys.Color.OnPrimary),
        [Comp.DatePickerModal.SelectionYearStateLayerHeight] = 36.0d,
        [Comp.DatePickerModal.SelectionYearStateLayerShape] = new TokenRef(Sys.Shape.Full),
        [Comp.DatePickerModal.SelectionYearStateLayerWidth] = 72.0d,
        [Comp.DatePickerModal.SelectionYearUnselectedLabelTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant)
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);

    public event EventHandler? Changed
    {
        add
        {
        }
        remove
        {
        }
    }
}
