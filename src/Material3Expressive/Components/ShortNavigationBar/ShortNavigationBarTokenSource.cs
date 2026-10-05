using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class ShortNavigationBarTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.ShortNavigationBar.IndicatorAnimation] = new TokenRef(Sys.Motion.Spring.SpatialDefault),
        [Comp.ShortNavigationBar.WindowInsets] = default(Avalonia.Thickness),
        [Comp.ShortNavigationBar.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.ShortNavigationBar.ContainerHeight] = 64d,
        [Comp.ShortNavigationBar.ContentColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.ShortNavigationBar.StartIndicatorHeight] = 40d,
        [Comp.ShortNavigationBar.StartIndicatorHorizontalPadding] = 16d,
        [Comp.ShortNavigationBar.TopItemVerticalPadding] = 6d,
        [Comp.ShortNavigationBar.IconLabelSpace] = 4d,
        [Comp.ShortNavigationBar.SelectedTextColorStartIconPosition] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.ShortNavigationBar.LabelTextStyle] = new TokenRef(Sys.TypeScale.LabelMedium.Style),
        [Comp.ShortNavigationBar.ActiveIndicatorWidth] = 56d,
        [Comp.ShortNavigationBar.ActiveIndicatorHeight] = 32d,
        [Comp.ShortNavigationBar.ActiveIndicatorShape] = new TokenRef(Sys.Shape.Full),
        [Comp.ShortNavigationBar.IconSize] = 24d,
        [Comp.ShortNavigationBar.SelectedIconColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.ShortNavigationBar.SelectedTextColorTopIconPosition] = new TokenRef(Sys.Color.Secondary),
        [Comp.ShortNavigationBar.IndicatorColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.ShortNavigationBar.UnselectedIconColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.ShortNavigationBar.UnselectedTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.ShortNavigationBar.DisabledIconColor] = new WithAlpha(Comp.ShortNavigationBar.UnselectedIconColor, 0.38),
        [Comp.ShortNavigationBar.DisabledTextColor] = new WithAlpha(Comp.ShortNavigationBar.UnselectedTextColor, 0.38),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
