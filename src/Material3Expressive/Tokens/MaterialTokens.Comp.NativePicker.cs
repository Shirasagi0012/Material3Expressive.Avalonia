using Avalonia;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class NativeDatePicker
        {
            public static readonly MaterialTokenKey<double> MinimumWidth = new("md.comp.nativedatepicker.minimum-width");
        }
        public static class NativeTimePicker
        {
            public static readonly MaterialTokenKey<double> MinimumWidth = new("md.comp.nativetimepicker.minimum-width");
        }
        public static class NativePicker
        {
            public static readonly MaterialTokenKey<double> FieldMinimumHeight = new("md.comp.nativepicker.field-minimum-height");
            public static readonly MaterialTokenKey<Thickness> FieldPadding = new("md.comp.nativepicker.field-padding");
            public static readonly MaterialTokenKey<Thickness> FieldDividerMargin = new("md.comp.nativepicker.field-divider-margin");
            public static readonly MaterialTokenKey<double> PresenterWidth = new("md.comp.nativepicker.presenter-width");
            public static readonly MaterialTokenKey<double> PresenterHeight = new("md.comp.nativepicker.presenter-height");
            public static readonly MaterialTokenKey<double> PresenterMaxHeight = new("md.comp.nativepicker.presenter-max-height");
            public static readonly MaterialTokenKey<Thickness> WheelMargin = new("md.comp.nativepicker.wheel-margin");
            public static readonly MaterialTokenKey<double> WheelColumnSpacing = new("md.comp.nativepicker.wheel-column-spacing");
            public static readonly MaterialTokenKey<double> WheelItemHeight = new("md.comp.nativepicker.wheel-item-height");
            public static readonly MaterialTokenKey<double> RepeatButtonHeight = new("md.comp.nativepicker.repeat-button-height");
            public static readonly MaterialTokenKey<double> RepeatButtonMinimumHeight = new("md.comp.nativepicker.repeat-button-minimum-height");
            public static readonly MaterialTokenKey<double> ActionRowHeight = new("md.comp.nativepicker.action-row-height");
            public static readonly MaterialTokenKey<Thickness> ActionPadding = new("md.comp.nativepicker.action-padding");
        }
        public static class NativeCalendarDatePicker
        {
            public static readonly MaterialTokenKey<double> MinimumWidth = new("md.comp.nativecalendardatepicker.minimum-width");
            public static readonly MaterialTokenKey<double> OpenButtonMinimumWidth = new("md.comp.nativecalendardatepicker.open-button-minimum-width");
        }
        public static class NativeCalendar
        {
            public static readonly MaterialTokenKey<double> ContentWidth = new("md.comp.nativecalendar.content-width");
            public static readonly MaterialTokenKey<Thickness> ContentMargin = new("md.comp.nativecalendar.content-margin");
            public static readonly MaterialTokenKey<double> HeaderHeight = new("md.comp.nativecalendar.header-height");
            public static readonly MaterialTokenKey<double> NavigationColumnWidth = new("md.comp.nativecalendar.navigation-column-width");
            public static readonly MaterialTokenKey<double> WeekdayRowHeight = new("md.comp.nativecalendar.weekday-row-height");
            public static readonly MaterialTokenKey<double> DayRowHeight = new("md.comp.nativecalendar.day-row-height");
            public static readonly MaterialTokenKey<double> DayMinimumWidth = new("md.comp.nativecalendar.day-minimum-width");
            public static readonly MaterialTokenKey<double> DayMinimumHeight = new("md.comp.nativecalendar.day-minimum-height");
            public static readonly MaterialTokenKey<Thickness> DayMargin = new("md.comp.nativecalendar.day-margin");
            public static readonly MaterialTokenKey<double> YearMinimumWidth = new("md.comp.nativecalendar.year-minimum-width");
            public static readonly MaterialTokenKey<double> YearMinimumHeight = new("md.comp.nativecalendar.year-minimum-height");
            public static readonly MaterialTokenKey<Thickness> YearPadding = new("md.comp.nativecalendar.year-padding");
            public static readonly MaterialTokenKey<double> NavigationButtonMinimumWidth = new("md.comp.nativecalendar.navigation-button-minimum-width");
        }
    }
}
