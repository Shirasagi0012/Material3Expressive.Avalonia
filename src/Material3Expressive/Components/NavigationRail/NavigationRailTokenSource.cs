using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class NavigationRailTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.NavigationRail.ContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.NavigationRail.NarrowContainerWidth] = 80d,
        [Comp.NavigationRail.ActiveIndicatorWidth] = 56d,
        [Comp.NavigationRail.ActiveIndicatorHeight] = 32d,
        [Comp.NavigationRail.ActiveIndicatorShape] = new TokenRef(Sys.Shape.Full),
        [Comp.NavigationRail.IconSize] = 24d,
        [Comp.NavigationRail.SelectedIconColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.NavigationRail.SelectedTextColor] = new TokenRef(Sys.Color.Secondary),
        [Comp.NavigationRail.IndicatorColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.NavigationRail.UnselectedIconColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.NavigationRail.UnselectedTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.NavigationRail.DisabledIconColor] = new WithAlpha(Comp.NavigationRail.UnselectedIconColor, 0.38),
        [Comp.NavigationRail.DisabledTextColor] = new WithAlpha(Comp.NavigationRail.UnselectedTextColor, 0.38),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
