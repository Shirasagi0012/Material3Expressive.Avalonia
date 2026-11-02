// Translated from AndroidX DatePickerModalTokens.kt, fixed baseline f3ed195f0c9e.
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class DatePickerModal
        {
            public static readonly MaterialTokenKey<Thickness> TitlePadding = new("md.comp.datepickermodal.title-padding");
            public static readonly MaterialTokenKey<Thickness> HeadlinePadding = new("md.comp.datepickermodal.headline-padding");
            public static readonly MaterialTokenKey<double> ModeToggleSize = new("md.comp.datepickermodal.mode-toggle-size");
            public static readonly MaterialTokenKey<double> MonthNavigationHeight = new("md.comp.datepickermodal.month-navigation-height");
            public static readonly MaterialTokenKey<Thickness> CalendarHorizontalPadding = new("md.comp.datepickermodal.calendar-horizontal-padding");
            public static readonly MaterialTokenKey<double> NavigationButtonSize = new("md.comp.datepickermodal.navigation-button-size");
            public static readonly MaterialTokenKey<double> MonthButtonMinWidth = new("md.comp.datepickermodal.month-button-min-width");
            public static readonly MaterialTokenKey<double> MonthButtonHeight = new("md.comp.datepickermodal.month-button-height");
            public static readonly MaterialTokenKey<Thickness> MonthButtonHorizontalPadding = new("md.comp.datepickermodal.month-button-horizontal-padding");
            public static readonly MaterialTokenKey<double> WeekdayRowHeight = new("md.comp.datepickermodal.weekday-row-height");
            public static readonly MaterialTokenKey<double> DayCellWidth = new("md.comp.datepickermodal.day-cell-width");
            public static readonly MaterialTokenKey<double> DayCellHeight = new("md.comp.datepickermodal.day-cell-height");
            public static readonly MaterialTokenKey<double> RangeIndicatorTopOffset = new("md.comp.datepickermodal.range-indicator-top-offset");
            public static readonly MaterialTokenKey<double> YearGridWidth = new("md.comp.datepickermodal.year-grid-width");
            public static readonly MaterialTokenKey<Thickness> YearGridVerticalMargin = new("md.comp.datepickermodal.year-grid-vertical-margin");
            public static readonly MaterialTokenKey<double> YearCellWidth = new("md.comp.datepickermodal.year-cell-width");
            public static readonly MaterialTokenKey<double> YearCellHeight = new("md.comp.datepickermodal.year-cell-height");
            public static readonly MaterialTokenKey<double> YearScrollHeight = new("md.comp.datepickermodal.year-scroll-height");
            public static readonly MaterialTokenKey<double> RangeMonthListHeight = new("md.comp.datepickermodal.range-month-list-height");
            public static readonly MaterialTokenKey<Thickness> RangeMonthTitleMargin = new("md.comp.datepickermodal.range-month-title-margin");
            public static readonly MaterialTokenKey<Thickness> InputMargin = new("md.comp.datepickermodal.input-margin");
            public static readonly MaterialTokenKey<double> InputSpacing = new("md.comp.datepickermodal.input-spacing");
            public static readonly MaterialTokenKey<double> SingleInputWidth = new("md.comp.datepickermodal.single-input-width");
            public static readonly MaterialTokenKey<double> RangeInputWidth = new("md.comp.datepickermodal.range-input-width");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.datepickermodal.container-color");
            public static readonly MaterialTokenKey<double> ContainerElevation = new("md.comp.datepickermodal.container-elevation");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.datepickermodal.container-height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.datepickermodal.container-shape");
            public static readonly MaterialTokenKey<double> ContainerWidth = new("md.comp.datepickermodal.container-width");
            public static readonly MaterialTokenKey<double> DateContainerHeight = new("md.comp.datepickermodal.date-container-height");
            public static readonly MaterialTokenKey<CornerRadius> DateContainerShape = new("md.comp.datepickermodal.date-container-shape");
            public static readonly MaterialTokenKey<double> DateContainerWidth = new("md.comp.datepickermodal.date-container-width");
            public static readonly MaterialTokenKey<TextStyle> DateLabelTextFont = new("md.comp.datepickermodal.date-label-text-font");
            public static readonly MaterialTokenKey<Color> DateSelectedContainerColor = new("md.comp.datepickermodal.date-selected-container-color");
            public static readonly MaterialTokenKey<Color> DateSelectedLabelTextColor = new("md.comp.datepickermodal.date-selected-label-text-color");
            public static readonly MaterialTokenKey<double> DateStateLayerHeight = new("md.comp.datepickermodal.date-state-layer-height");
            public static readonly MaterialTokenKey<CornerRadius> DateStateLayerShape = new("md.comp.datepickermodal.date-state-layer-shape");
            public static readonly MaterialTokenKey<double> DateStateLayerWidth = new("md.comp.datepickermodal.date-state-layer-width");
            public static readonly MaterialTokenKey<Color> DateTodayContainerOutlineColor = new("md.comp.datepickermodal.date-today-container-outline-color");
            public static readonly MaterialTokenKey<double> DateTodayContainerOutlineWidth = new("md.comp.datepickermodal.date-today-container-outline-width");
            public static readonly MaterialTokenKey<Color> DateTodayLabelTextColor = new("md.comp.datepickermodal.date-today-label-text-color");
            public static readonly MaterialTokenKey<Color> DateUnselectedLabelTextColor = new("md.comp.datepickermodal.date-unselected-label-text-color");
            public static readonly MaterialTokenKey<double> HeaderContainerHeight = new("md.comp.datepickermodal.header-container-height");
            public static readonly MaterialTokenKey<double> HeaderContainerWidth = new("md.comp.datepickermodal.header-container-width");
            public static readonly MaterialTokenKey<Color> HeaderHeadlineColor = new("md.comp.datepickermodal.header-headline-color");
            public static readonly MaterialTokenKey<TextStyle> HeaderHeadlineFont = new("md.comp.datepickermodal.header-headline-font");
            public static readonly MaterialTokenKey<Color> HeaderSupportingTextColor = new("md.comp.datepickermodal.header-supporting-text-color");
            public static readonly MaterialTokenKey<TextStyle> HeaderSupportingTextFont = new("md.comp.datepickermodal.header-supporting-text-font");
            public static readonly MaterialTokenKey<Color> RangeSelectionActiveIndicatorContainerColor = new("md.comp.datepickermodal.range-selection-active-indicator-container-color");
            public static readonly MaterialTokenKey<double> RangeSelectionActiveIndicatorContainerHeight = new("md.comp.datepickermodal.range-selection-active-indicator-container-height");
            public static readonly MaterialTokenKey<CornerRadius> RangeSelectionActiveIndicatorContainerShape = new("md.comp.datepickermodal.range-selection-active-indicator-container-shape");
            public static readonly MaterialTokenKey<double> RangeSelectionContainerElevation = new("md.comp.datepickermodal.range-selection-container-elevation");
            public static readonly MaterialTokenKey<CornerRadius> RangeSelectionContainerShape = new("md.comp.datepickermodal.range-selection-container-shape");
            public static readonly MaterialTokenKey<Color> SelectionDateInRangeLabelTextColor = new("md.comp.datepickermodal.selection-date-in-range-label-text-color");
            public static readonly MaterialTokenKey<double> RangeSelectionHeaderContainerHeight = new("md.comp.datepickermodal.range-selection-header-container-height");
            public static readonly MaterialTokenKey<TextStyle> RangeSelectionHeaderHeadlineFont = new("md.comp.datepickermodal.range-selection-header-headline-font");
            public static readonly MaterialTokenKey<Color> RangeSelectionMonthSubheadColor = new("md.comp.datepickermodal.range-selection-month-subhead-color");
            public static readonly MaterialTokenKey<TextStyle> RangeSelectionMonthSubheadFont = new("md.comp.datepickermodal.range-selection-month-subhead-font");
            public static readonly MaterialTokenKey<Color> WeekdaysLabelTextColor = new("md.comp.datepickermodal.weekdays-label-text-color");
            public static readonly MaterialTokenKey<TextStyle> WeekdaysLabelTextFont = new("md.comp.datepickermodal.weekdays-label-text-font");
            public static readonly MaterialTokenKey<double> SelectionYearContainerHeight = new("md.comp.datepickermodal.selection-year-container-height");
            public static readonly MaterialTokenKey<double> SelectionYearContainerWidth = new("md.comp.datepickermodal.selection-year-container-width");
            public static readonly MaterialTokenKey<TextStyle> SelectionYearLabelTextFont = new("md.comp.datepickermodal.selection-year-label-text-font");
            public static readonly MaterialTokenKey<Color> SelectionYearSelectedContainerColor = new("md.comp.datepickermodal.selection-year-selected-container-color");
            public static readonly MaterialTokenKey<Color> SelectionYearSelectedLabelTextColor = new("md.comp.datepickermodal.selection-year-selected-label-text-color");
            public static readonly MaterialTokenKey<double> SelectionYearStateLayerHeight = new("md.comp.datepickermodal.selection-year-state-layer-height");
            public static readonly MaterialTokenKey<CornerRadius> SelectionYearStateLayerShape = new("md.comp.datepickermodal.selection-year-state-layer-shape");
            public static readonly MaterialTokenKey<double> SelectionYearStateLayerWidth = new("md.comp.datepickermodal.selection-year-state-layer-width");
            public static readonly MaterialTokenKey<Color> SelectionYearUnselectedLabelTextColor = new("md.comp.datepickermodal.selection-year-unselected-label-text-color");
        }
    }
}

