// Translated from AndroidX DateInputModalTokens.kt, fixed baseline f3ed195f0c9e.
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).

using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class DateInputModalTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object> {
        [Comp.DateInputModal.ContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.DateInputModal.ContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.DateInputModal.ContainerHeight] = 512.0d, [Comp.DateInputModal.ContainerShape] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.DateInputModal.ContainerSurfaceTintLayerColor] = new TokenRef(Sys.Color.SurfaceTint),
        [Comp.DateInputModal.ContainerWidth] = 328.0d, [Comp.DateInputModal.HeaderContainerHeight] = 120.0d,
        [Comp.DateInputModal.HeaderContainerWidth] = 328.0d,
        [Comp.DateInputModal.HeaderHeadlineColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.DateInputModal.HeaderHeadlineFont] = new TokenRef(Sys.TypeScale.HeadlineLarge.Style),
        [Comp.DateInputModal.HeaderSupportingTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.DateInputModal.HeaderSupportingTextFont] = new TokenRef(Sys.TypeScale.LabelLarge.Style)
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
