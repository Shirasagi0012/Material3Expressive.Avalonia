using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class TabRowTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.TabRow.EdgePadding] = 52d,
        [Comp.TabRow.MinTabWidth] = 90d,
        [Comp.TabRow.SelectedContentAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault),
        [Comp.TabRow.UnselectedContentAnimation] = new TokenRef(Sys.Motion.Spring.EffectsFast),
        [Comp.TabRow.IndicatorAnimation] = new TokenRef(Sys.Motion.Spring.SpatialDefault),
        [Comp.TabRow.ScrollAnimation] = new TokenRef(Sys.Motion.Spring.SpatialDefault),
        [Comp.TabRow.ContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.TabRow.PrimaryContentColor] = new TokenRef(Sys.Color.Primary),
        [Comp.TabRow.SecondaryContentColor] = new TokenRef(Sys.Color.OnSurface),
        // SecondaryIndicator also uses PrimaryNavigationTabTokens, not the secondary divider tokens.
        [Comp.TabRow.IndicatorColor] = new TokenRef(Sys.Color.Primary),
        [Comp.TabRow.IndicatorHeight] = 3d,
        [Comp.TabRow.IndicatorShape] = new CornerRadius(3),
        [Comp.TabRow.ContainerHeight] = 48d,
        [Comp.TabRow.LargeContainerHeight] = 72d,
        [Comp.TabRow.LabelTextStyle] = new TokenRef(Sys.TypeScale.TitleSmall.Style),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
