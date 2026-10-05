using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // Fixed NavigationRail.kt consumes these members from Collapsed, BaselineItem,
        // VerticalItem and ColorTokens; WideNavigationRail tokens are intentionally separate.
        public static class NavigationRail
        {
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> EffectsAnimation = new("md.comp.navigation-rail.effects-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> IndicatorSizeAnimation = new("md.comp.navigation-rail.indicator-size-animation");
            public static readonly MaterialTokenKey<Thickness> WindowInsets = new("md.comp.navigation-rail.window-insets");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.navigation-rail.container.color");
            public static readonly MaterialTokenKey<double> NarrowContainerWidth = new("md.comp.navigation-rail.narrow-container.width");
            public static readonly MaterialTokenKey<double> ActiveIndicatorWidth = new("md.comp.navigation-rail.active-indicator.width");
            public static readonly MaterialTokenKey<double> ActiveIndicatorHeight = new("md.comp.navigation-rail.active-indicator.height");
            public static readonly MaterialTokenKey<CornerRadius> ActiveIndicatorShape = new("md.comp.navigation-rail.active-indicator.shape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.navigation-rail.icon.size");
            public static readonly MaterialTokenKey<Color> SelectedIconColor = new("md.comp.navigation-rail.selected.icon.color");
            public static readonly MaterialTokenKey<Color> SelectedTextColor = new("md.comp.navigation-rail.selected.label.color");
            public static readonly MaterialTokenKey<Color> IndicatorColor = new("md.comp.navigation-rail.indicator.color");
            public static readonly MaterialTokenKey<Color> UnselectedIconColor = new("md.comp.navigation-rail.unselected.icon.color");
            public static readonly MaterialTokenKey<Color> UnselectedTextColor = new("md.comp.navigation-rail.unselected.label.color");
            // Local derivations of NavigationRailItemDefaults.colors, rather than upstream tokens.
            public static readonly MaterialTokenKey<Color> DisabledIconColor = new("md.comp.navigation-rail.disabled.icon.color");
            public static readonly MaterialTokenKey<Color> DisabledTextColor = new("md.comp.navigation-rail.disabled.label.color");
        }
    }
}
