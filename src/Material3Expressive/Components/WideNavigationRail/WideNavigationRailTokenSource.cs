using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class WideNavigationRailTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.WideNavigationRail.ContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.WideNavigationRail.ModalContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.WideNavigationRail.ModalScrimColor] = new WithAlpha(Sys.Color.Scrim, 0.32),
        [Comp.WideNavigationRail.ContainerShape] = new TokenRef(Sys.Shape.None),
        [Comp.WideNavigationRail.ModalContainerShape] = new TokenRef(Sys.Shape.Large),
        [Comp.WideNavigationRail.CollapsedContainerWidth] = 96d,
        [Comp.WideNavigationRail.ExpandedContainerMinWidth] = 220d,
        [Comp.WideNavigationRail.ExpandedContainerMaxWidth] = 360d,
        [Comp.WideNavigationRail.TopSpace] = 44d,
        [Comp.WideNavigationRail.HeaderSpace] = 40d,
        [Comp.WideNavigationRail.CollapsedItemMinHeight] = 64d,
        [Comp.WideNavigationRail.CollapsedItemVerticalSpace] = 4d,
        [Comp.WideNavigationRail.ItemHorizontalPadding] = 20d,
        [Comp.WideNavigationRail.ActiveIndicatorWidth] = 56d,
        [Comp.WideNavigationRail.TopIconIndicatorHeight] = 32d,
        [Comp.WideNavigationRail.StartIconIndicatorHeight] = 56d,
        [Comp.WideNavigationRail.StartIconIndicatorLeadingSpace] = 16d,
        [Comp.WideNavigationRail.ActiveIndicatorShape] = new TokenRef(Sys.Shape.Full),
        [Comp.WideNavigationRail.IconSize] = 24d,
        [Comp.WideNavigationRail.TopIconLabelSpace] = 4d,
        [Comp.WideNavigationRail.StartIconLabelSpace] = 8d,
        [Comp.WideNavigationRail.TopIconLabelTextStyle] = new TokenRef(Sys.TypeScale.LabelMedium.Style),
        [Comp.WideNavigationRail.StartIconLabelTextStyle] = new TokenRef(Sys.TypeScale.LabelLarge.Style),
        [Comp.WideNavigationRail.SelectedIconColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.WideNavigationRail.SelectedTextColorTopIconPosition] = new TokenRef(Sys.Color.Secondary),
        [Comp.WideNavigationRail.SelectedTextColorStartIconPosition] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.WideNavigationRail.IndicatorColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.WideNavigationRail.UnselectedIconColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.WideNavigationRail.UnselectedTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.WideNavigationRail.DisabledIconColor] = new WithAlpha(Comp.WideNavigationRail.UnselectedIconColor, 0.38),
        [Comp.WideNavigationRail.DisabledTextColor] = new WithAlpha(Comp.WideNavigationRail.UnselectedTextColor, 0.38),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

