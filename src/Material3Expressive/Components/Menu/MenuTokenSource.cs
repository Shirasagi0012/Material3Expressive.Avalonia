/* Fixed Menu.kt / MenuDefaults.kt, MenuTokens, StandardMenuTokens, VibrantMenuTokens,
 * SegmentedMenuTokens. The pinned Core precision-pointer state remains false on desktop.
 * Copyright 2022-2025 The Android Open Source Project; Apache-2.0.
 */
using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class MenuTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.Menu.InactiveGroupShape] = new TokenRef(Sys.Shape.Small),
        [Comp.Menu.BaselineContainer] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.Menu.StandardContainer] = new TokenRef(Sys.Color.SurfaceContainerLow),
        [Comp.Menu.VibrantContainer] = new TokenRef(Sys.Color.TertiaryContainer),
        [Comp.Menu.Text] = new TokenRef(Sys.Color.OnSurface),
        [Comp.Menu.LeadingIcon] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.Menu.TrailingIcon] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.Menu.DisabledText] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.Menu.DisabledIcon] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.Menu.StandardSelectedContainer] = new TokenRef(Sys.Color.TertiaryContainer),
        [Comp.Menu.StandardSelectedContent] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.Menu.VibrantText] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.Menu.VibrantDisabledContent] = new WithAlpha(Sys.Color.OnTertiaryContainer, 0.38),
        [Comp.Menu.VibrantSelectedContainer] = new TokenRef(Sys.Color.Tertiary),
        [Comp.Menu.VibrantSelectedContent] = new TokenRef(Sys.Color.OnTertiary),
        [Comp.Menu.BaselineShape] = new TokenRef(Sys.Shape.ExtraSmall),
        [Comp.Menu.VerticalShape] = new TokenRef(Sys.Shape.Large),
        [Comp.Menu.ItemShape] = new TokenRef(Sys.Shape.ExtraSmall),
        [Comp.Menu.SelectedItemShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.Menu.ShadowElevation] = new TokenRef(Sys.Elevation.Level2),
        [Comp.Menu.BaselinePadding] = new Thickness(0, 8),
        [Comp.Menu.VerticalPadding] = new Thickness(0, 2),
        [Comp.Menu.BaselineItemPadding] = new Thickness(12, 0),
        [Comp.Menu.VerticalItemPadding] = new Thickness(12),
        [Comp.Menu.ItemMargin] = new Thickness(4, 0),
        [Comp.Menu.BaselineItemHeight] = 48d,
        [Comp.Menu.VerticalItemHeight] = 44d,
        [Comp.Menu.MinimumInteractiveSize] = 48d,
        [Comp.Menu.MinItemWidth] = 112d,
        [Comp.Menu.MaxItemWidth] = 280d,
        [Comp.Menu.BaselineIconSize] = 24d,
        [Comp.Menu.LeadingIconSize] = 20d,
        [Comp.Menu.TrailingIconSize] = 20d,
        [Comp.Menu.ColorAnimation] = new TokenRef(Sys.Motion.Spring.EffectsFast),
        [Comp.Menu.ShapeAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

