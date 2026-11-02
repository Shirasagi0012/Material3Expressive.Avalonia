using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class CommandBarTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.CommandBar.ContainerHeight] = 48d,
        [Comp.CommandBar.ItemMinHeight] = 48d,
        [Comp.CommandBar.ItemMinWidth] = 48d,
        [Comp.CommandBar.ItemContentPadding] = new Thickness(8, 4),
        [Comp.CommandBar.InlineItemOuterPadding] = 8d,
        [Comp.CommandBar.IndicatorHorizontalInset] = 8d,
        [Comp.CommandBar.IconLabelSpace] = 4d,
        [Comp.CommandBar.OverflowIconLabelSpace] = 8d,
        [Comp.CommandBar.ContainerPadding] = new Thickness(4, 0),
        [Comp.CommandBar.ItemWidthBottom] = 76d,
        [Comp.CommandBar.ItemWidthRight] = 104d,
        [Comp.CommandBar.ItemWidthCollapsed] = 48d,
        [Comp.CommandBar.SeparatorHeight] = 24d,
        [Comp.CommandBar.SeparatorMargin] = new Thickness(8),
        [Comp.CommandBar.OverflowSeparatorMargin] = new Thickness(8, 4),
        [Comp.CommandBar.EffectsAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault),
        [Comp.CommandBar.IndicatorSizeAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.CommandBar.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.CommandBar.StackedContainerHeight] = 60d,
        [Comp.CommandBar.LabelTextStyle] = new TokenRef(Sys.TypeScale.LabelMedium.Style),
        [Comp.CommandBar.ActiveIndicatorWidth] = 56d,
        [Comp.CommandBar.ActiveIndicatorHeight] = 32d,
        [Comp.CommandBar.InlineContentPadding] = new Thickness(12, 6),
        [Comp.CommandBar.IconSize] = 24d,
        [Comp.CommandBar.SelectedIconColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.CommandBar.SelectedTextColor] = new TokenRef(Sys.Color.Secondary),
        [Comp.CommandBar.IndicatorColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.CommandBar.UnselectedIconColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.CommandBar.UnselectedTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.CommandBar.DisabledIconColor] = new WithAlpha(Comp.CommandBar.UnselectedIconColor, 0.38),
        [Comp.CommandBar.DisabledTextColor] = new WithAlpha(Comp.CommandBar.UnselectedTextColor, 0.38),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
