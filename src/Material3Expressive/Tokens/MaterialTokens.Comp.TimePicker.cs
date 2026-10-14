// Translated from AndroidX TimePickerTokens.kt, fixed baseline f3ed195f0c9e.
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
        public static class TimePicker
        {
            public static readonly MaterialTokenKey<Color> ClockDialColor = new("md.comp.timepicker.clock-dial-color");
            public static readonly MaterialTokenKey<double> ClockDialContainerSize = new("md.comp.timepicker.clock-dial-container-size");
            public static readonly MaterialTokenKey<TextStyle> ClockDialLabelTextFont = new("md.comp.timepicker.clock-dial-label-text-font");
            public static readonly MaterialTokenKey<Color> ClockDialSelectedLabelTextColor = new("md.comp.timepicker.clock-dial-selected-label-text-color");
            public static readonly MaterialTokenKey<Color> ClockDialSelectorCenterContainerColor = new("md.comp.timepicker.clock-dial-selector-center-container-color");
            public static readonly MaterialTokenKey<CornerRadius> ClockDialSelectorCenterContainerShape = new("md.comp.timepicker.clock-dial-selector-center-container-shape");
            public static readonly MaterialTokenKey<double> ClockDialSelectorCenterContainerSize = new("md.comp.timepicker.clock-dial-selector-center-container-size");
            public static readonly MaterialTokenKey<Color> ClockDialSelectorHandleContainerColor = new("md.comp.timepicker.clock-dial-selector-handle-container-color");
            public static readonly MaterialTokenKey<CornerRadius> ClockDialSelectorHandleContainerShape = new("md.comp.timepicker.clock-dial-selector-handle-container-shape");
            public static readonly MaterialTokenKey<double> ClockDialSelectorHandleContainerSize = new("md.comp.timepicker.clock-dial-selector-handle-container-size");
            public static readonly MaterialTokenKey<Color> ClockDialSelectorTrackContainerColor = new("md.comp.timepicker.clock-dial-selector-track-container-color");
            public static readonly MaterialTokenKey<double> ClockDialSelectorTrackContainerWidth = new("md.comp.timepicker.clock-dial-selector-track-container-width");
            public static readonly MaterialTokenKey<CornerRadius> ClockDialShape = new("md.comp.timepicker.clock-dial-shape");
            public static readonly MaterialTokenKey<Color> ClockDialUnselectedLabelTextColor = new("md.comp.timepicker.clock-dial-unselected-label-text-color");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.timepicker.container-color");
            public static readonly MaterialTokenKey<double> ContainerElevation = new("md.comp.timepicker.container-elevation");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.timepicker.container-shape");
            public static readonly MaterialTokenKey<Color> HeadlineColor = new("md.comp.timepicker.headline-color");
            public static readonly MaterialTokenKey<TextStyle> HeadlineFont = new("md.comp.timepicker.headline-font");
            public static readonly MaterialTokenKey<CornerRadius> PeriodSelectorContainerShape = new("md.comp.timepicker.period-selector-container-shape");
            public static readonly MaterialTokenKey<double> PeriodSelectorHorizontalContainerHeight = new("md.comp.timepicker.period-selector-horizontal-container-height");
            public static readonly MaterialTokenKey<double> PeriodSelectorHorizontalContainerWidth = new("md.comp.timepicker.period-selector-horizontal-container-width");
            public static readonly MaterialTokenKey<TextStyle> PeriodSelectorLabelTextFont = new("md.comp.timepicker.period-selector-label-text-font");
            public static readonly MaterialTokenKey<Color> PeriodSelectorOutlineColor = new("md.comp.timepicker.period-selector-outline-color");
            public static readonly MaterialTokenKey<double> PeriodSelectorOutlineWidth = new("md.comp.timepicker.period-selector-outline-width");
            public static readonly MaterialTokenKey<Color> PeriodSelectorSelectedContainerColor = new("md.comp.timepicker.period-selector-selected-container-color");
            public static readonly MaterialTokenKey<Color> PeriodSelectorSelectedFocusLabelTextColor = new("md.comp.timepicker.period-selector-selected-focus-label-text-color");
            public static readonly MaterialTokenKey<Color> PeriodSelectorSelectedHoverLabelTextColor = new("md.comp.timepicker.period-selector-selected-hover-label-text-color");
            public static readonly MaterialTokenKey<Color> PeriodSelectorSelectedLabelTextColor = new("md.comp.timepicker.period-selector-selected-label-text-color");
            public static readonly MaterialTokenKey<Color> PeriodSelectorSelectedPressedLabelTextColor = new("md.comp.timepicker.period-selector-selected-pressed-label-text-color");
            public static readonly MaterialTokenKey<Color> PeriodSelectorUnselectedFocusLabelTextColor = new("md.comp.timepicker.period-selector-unselected-focus-label-text-color");
            public static readonly MaterialTokenKey<Color> PeriodSelectorUnselectedHoverLabelTextColor = new("md.comp.timepicker.period-selector-unselected-hover-label-text-color");
            public static readonly MaterialTokenKey<Color> PeriodSelectorUnselectedLabelTextColor = new("md.comp.timepicker.period-selector-unselected-label-text-color");
            public static readonly MaterialTokenKey<Color> PeriodSelectorUnselectedPressedLabelTextColor = new("md.comp.timepicker.period-selector-unselected-pressed-label-text-color");
            public static readonly MaterialTokenKey<double> PeriodSelectorVerticalContainerHeight = new("md.comp.timepicker.period-selector-vertical-container-height");
            public static readonly MaterialTokenKey<double> PeriodSelectorVerticalContainerWidth = new("md.comp.timepicker.period-selector-vertical-container-width");
            public static readonly MaterialTokenKey<double> TimeSelector24HVerticalContainerWidth = new("md.comp.timepicker.time-selector24-h-vertical-container-width");
            public static readonly MaterialTokenKey<double> TimeSelectorContainerHeight = new("md.comp.timepicker.time-selector-container-height");
            public static readonly MaterialTokenKey<CornerRadius> TimeSelectorContainerShape = new("md.comp.timepicker.time-selector-container-shape");
            public static readonly MaterialTokenKey<double> TimeSelectorContainerWidth = new("md.comp.timepicker.time-selector-container-width");
            public static readonly MaterialTokenKey<TextStyle> TimeSelectorLabelTextFont = new("md.comp.timepicker.time-selector-label-text-font");
            public static readonly MaterialTokenKey<Color> TimeSelectorSelectedContainerColor = new("md.comp.timepicker.time-selector-selected-container-color");
            public static readonly MaterialTokenKey<Color> TimeSelectorSelectedFocusLabelTextColor = new("md.comp.timepicker.time-selector-selected-focus-label-text-color");
            public static readonly MaterialTokenKey<Color> TimeSelectorSelectedHoverLabelTextColor = new("md.comp.timepicker.time-selector-selected-hover-label-text-color");
            public static readonly MaterialTokenKey<Color> TimeSelectorSelectedLabelTextColor = new("md.comp.timepicker.time-selector-selected-label-text-color");
            public static readonly MaterialTokenKey<Color> TimeSelectorSelectedPressedLabelTextColor = new("md.comp.timepicker.time-selector-selected-pressed-label-text-color");
            public static readonly MaterialTokenKey<Color> TimeSelectorSeparatorColor = new("md.comp.timepicker.time-selector-separator-color");
            public static readonly MaterialTokenKey<TextStyle> TimeSelectorSeparatorFont = new("md.comp.timepicker.time-selector-separator-font");
            public static readonly MaterialTokenKey<Color> TimeSelectorUnselectedContainerColor = new("md.comp.timepicker.time-selector-unselected-container-color");
            public static readonly MaterialTokenKey<Color> TimeSelectorUnselectedFocusLabelTextColor = new("md.comp.timepicker.time-selector-unselected-focus-label-text-color");
            public static readonly MaterialTokenKey<Color> TimeSelectorUnselectedHoverLabelTextColor = new("md.comp.timepicker.time-selector-unselected-hover-label-text-color");
            public static readonly MaterialTokenKey<Color> TimeSelectorUnselectedLabelTextColor = new("md.comp.timepicker.time-selector-unselected-label-text-color");
            public static readonly MaterialTokenKey<Color> TimeSelectorUnselectedPressedLabelTextColor = new("md.comp.timepicker.time-selector-unselected-pressed-label-text-color");
        }
    }
}

