// Translated from AndroidX TimeInputTokens.kt, fixed baseline f3ed195f0c9e.
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class TimeInputTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.TimeInput.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerHigh),
        [Comp.TimeInput.ContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.TimeInput.ContainerShape] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.TimeInput.FocusIndicatorColor] = new TokenRef(Sys.Color.Secondary),
        [Comp.TimeInput.HeadlineColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimeInput.HeadlineFont] = new TokenRef(Sys.TypeScale.LabelMedium.Style),
        [Comp.TimeInput.PeriodSelectorContainerHeight] = 72.0d,
        [Comp.TimeInput.PeriodSelectorContainerShape] = new TokenRef(Sys.Shape.Small),
        [Comp.TimeInput.PeriodSelectorContainerWidth] = 52.0d,
        [Comp.TimeInput.PeriodSelectorLabelTextFont] = new TokenRef(Sys.TypeScale.TitleMedium.Style),
        [Comp.TimeInput.PeriodSelectorOutlineColor] = new TokenRef(Sys.Color.Outline),
        [Comp.TimeInput.PeriodSelectorOutlineWidth] = 1.0d,
        [Comp.TimeInput.PeriodSelectorSelectedContainerColor] = new TokenRef(Sys.Color.TertiaryContainer),
        [Comp.TimeInput.PeriodSelectorSelectedFocusLabelTextColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.TimeInput.PeriodSelectorSelectedHoverLabelTextColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.TimeInput.PeriodSelectorSelectedLabelTextColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.TimeInput.PeriodSelectorSelectedPressedLabelTextColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.TimeInput.PeriodSelectorUnselectedFocusLabelTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimeInput.PeriodSelectorUnselectedHoverLabelTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimeInput.PeriodSelectorUnselectedLabelTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimeInput.PeriodSelectorUnselectedPressedLabelTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimeInput.TimeFieldContainerColor] = new TokenRef(Sys.Color.SurfaceContainerHighest),
        [Comp.TimeInput.TimeFieldContainerHeight] = 72.0d,
        [Comp.TimeInput.TimeFieldContainerShape] = new TokenRef(Sys.Shape.Small),
        [Comp.TimeInput.TimeFieldContainerWidth] = 96.0d,
        [Comp.TimeInput.TimeFieldFocusContainerColor] = new TokenRef(Sys.Color.PrimaryContainer),
        [Comp.TimeInput.TimeFieldFocusLabelTextColor] = new TokenRef(Sys.Color.OnPrimaryContainer),
        [Comp.TimeInput.TimeFieldFocusOutlineColor] = new TokenRef(Sys.Color.Primary),
        [Comp.TimeInput.TimeFieldFocusOutlineWidth] = 2.0d,
        [Comp.TimeInput.TimeFieldHoverLabelTextColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.TimeInput.TimeFieldLabelTextColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.TimeInput.TimeFieldLabelTextFont] = new TokenRef(Sys.TypeScale.DisplayMedium.Style),
        [Comp.TimeInput.TimeFieldSeparatorColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.TimeInput.TimeFieldSeparatorFont] = new TokenRef(Sys.TypeScale.DisplayLarge.Style),
        [Comp.TimeInput.TimeFieldSupportingTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.TimeInput.TimeFieldSupportingTextFont] = new TokenRef(Sys.TypeScale.BodySmall.Style),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

