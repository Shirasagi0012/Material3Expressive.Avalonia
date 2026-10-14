// Translated from AndroidX TimePickerTokens.kt, fixed baseline f3ed195f0c9e.
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class TimePickerTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.TimePicker.ClockDialColor] = new TokenRef(Sys.Color.SurfaceContainerHighest),
        [Comp.TimePicker.ClockDialContainerSize] = 256.0d,
        [Comp.TimePicker.ClockDialLabelTextFont] = new TokenRef(Sys.TypeScale.BodyLarge.Style),
        [Comp.TimePicker.ClockDialSelectedLabelTextColor] = new TokenRef(Sys.Color.OnPrimary),
        [Comp.TimePicker.ClockDialSelectorCenterContainerColor] = new TokenRef(Sys.Color.Primary),
        [Comp.TimePicker.ClockDialSelectorCenterContainerShape] = new TokenRef(Sys.Shape.Full),
        [Comp.TimePicker.ClockDialSelectorCenterContainerSize] = 8.0d,
        [Comp.TimePicker.ClockDialSelectorHandleContainerColor] = new TokenRef(Sys.Color.Primary),
        [Comp.TimePicker.ClockDialSelectorHandleContainerShape] = new TokenRef(Sys.Shape.Full),
        [Comp.TimePicker.ClockDialSelectorHandleContainerSize] = 48.0d,
        [Comp.TimePicker.ClockDialSelectorTrackContainerColor] = new TokenRef(Sys.Color.Primary),
        [Comp.TimePicker.ClockDialSelectorTrackContainerWidth] = 2.0d,
        [Comp.TimePicker.ClockDialShape] = new TokenRef(Sys.Shape.Full),
        [Comp.TimePicker.ClockDialUnselectedLabelTextColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.TimePicker.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerHigh),
        [Comp.TimePicker.ContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.TimePicker.ContainerShape] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.TimePicker.HeadlineColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimePicker.HeadlineFont] = new TokenRef(Sys.TypeScale.LabelMedium.Style),
        [Comp.TimePicker.PeriodSelectorContainerShape] = new TokenRef(Sys.Shape.Small),
        [Comp.TimePicker.PeriodSelectorHorizontalContainerHeight] = 38.0d,
        [Comp.TimePicker.PeriodSelectorHorizontalContainerWidth] = 216.0d,
        [Comp.TimePicker.PeriodSelectorLabelTextFont] = new TokenRef(Sys.TypeScale.TitleMedium.Style),
        [Comp.TimePicker.PeriodSelectorOutlineColor] = new TokenRef(Sys.Color.Outline),
        [Comp.TimePicker.PeriodSelectorOutlineWidth] = 1.0d,
        [Comp.TimePicker.PeriodSelectorSelectedContainerColor] = new TokenRef(Sys.Color.TertiaryContainer),
        [Comp.TimePicker.PeriodSelectorSelectedFocusLabelTextColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.TimePicker.PeriodSelectorSelectedHoverLabelTextColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.TimePicker.PeriodSelectorSelectedLabelTextColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.TimePicker.PeriodSelectorSelectedPressedLabelTextColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.TimePicker.PeriodSelectorUnselectedFocusLabelTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimePicker.PeriodSelectorUnselectedHoverLabelTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimePicker.PeriodSelectorUnselectedLabelTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimePicker.PeriodSelectorUnselectedPressedLabelTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimePicker.PeriodSelectorVerticalContainerHeight] = 80.0d,
        [Comp.TimePicker.PeriodSelectorVerticalContainerWidth] = 52.0d,
        [Comp.TimePicker.TimeSelector24HVerticalContainerWidth] = 114.0d,
        [Comp.TimePicker.TimeSelectorContainerHeight] = 80.0d,
        [Comp.TimePicker.TimeSelectorContainerShape] = new TokenRef(Sys.Shape.Small),
        [Comp.TimePicker.TimeSelectorContainerWidth] = 96.0d,
        [Comp.TimePicker.TimeSelectorLabelTextFont] = new TokenRef(Sys.TypeScale.DisplayLarge.Style),
        [Comp.TimePicker.TimeSelectorSelectedContainerColor] = new TokenRef(Sys.Color.PrimaryContainer),
        [Comp.TimePicker.TimeSelectorSelectedFocusLabelTextColor] = new TokenRef(Sys.Color.OnPrimaryContainer),
        [Comp.TimePicker.TimeSelectorSelectedHoverLabelTextColor] = new TokenRef(Sys.Color.OnPrimaryContainer),
        [Comp.TimePicker.TimeSelectorSelectedLabelTextColor] = new TokenRef(Sys.Color.OnPrimaryContainer),
        [Comp.TimePicker.TimeSelectorSelectedPressedLabelTextColor] = new TokenRef(Sys.Color.OnPrimaryContainer),
        [Comp.TimePicker.TimeSelectorSeparatorColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.TimePicker.TimeSelectorSeparatorFont] = new TokenRef(Sys.TypeScale.DisplayLarge.Style),
        [Comp.TimePicker.TimeSelectorUnselectedContainerColor] = new TokenRef(Sys.Color.SurfaceContainerHighest),
        [Comp.TimePicker.TimeSelectorUnselectedFocusLabelTextColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.TimePicker.TimeSelectorUnselectedHoverLabelTextColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.TimePicker.TimeSelectorUnselectedLabelTextColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.TimePicker.TimeSelectorUnselectedPressedLabelTextColor] = new TokenRef(Sys.Color.OnSurface),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

