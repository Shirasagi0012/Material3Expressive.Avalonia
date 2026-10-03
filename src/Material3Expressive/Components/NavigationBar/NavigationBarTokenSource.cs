using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class NavigationBarTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.NavigationBar.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.NavigationBar.TallContainerHeight] = 80d,
        [Comp.NavigationBar.DefaultTonalElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.NavigationBar.LabelTextStyle] = new TokenRef(Sys.TypeScale.LabelMedium.Style),
        [Comp.NavigationBar.ActiveIndicatorWidth] = 56d,
        [Comp.NavigationBar.ActiveIndicatorHeight] = 32d,
        [Comp.NavigationBar.ActiveIndicatorShape] = new TokenRef(Sys.Shape.Full),
        [Comp.NavigationBar.IconSize] = 24d,
        [Comp.NavigationBar.SelectedIconColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.NavigationBar.SelectedTextColor] = new TokenRef(Sys.Color.Secondary),
        [Comp.NavigationBar.IndicatorColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.NavigationBar.UnselectedIconColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.NavigationBar.UnselectedTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.NavigationBar.DisabledIconColor] = new WithAlpha(Comp.NavigationBar.UnselectedIconColor, 0.38),
        [Comp.NavigationBar.DisabledTextColor] = new WithAlpha(Comp.NavigationBar.UnselectedTextColor, 0.38),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
