using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class NavigationDrawerTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.NavigationDrawer.ModalContainerColor] = new TokenRef(Sys.Color.SurfaceContainerLow),
        [Comp.NavigationDrawer.StandardContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.NavigationDrawer.ContainerWidth] = 360d,
        [Comp.NavigationDrawer.ContainerShape] = new TokenRef(Sys.Shape.LargeEnd),
        [Comp.NavigationDrawer.SelectedContainerColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.NavigationDrawer.UnselectedContainerColor] = Avalonia.Media.Colors.Transparent,
        [Comp.NavigationDrawer.SelectedIconColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.NavigationDrawer.UnselectedIconColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.NavigationDrawer.SelectedTextColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.NavigationDrawer.UnselectedTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.NavigationDrawer.ActiveIndicatorHeight] = 56d,
        [Comp.NavigationDrawer.ActiveIndicatorShape] = new TokenRef(Sys.Shape.Full),
        [Comp.NavigationDrawer.LabelTextStyle] = new TokenRef(Sys.TypeScale.LabelLarge.Style),
        [Comp.NavigationDrawer.ScrimColor] = new WithAlpha(Sys.Color.Scrim, .32),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
