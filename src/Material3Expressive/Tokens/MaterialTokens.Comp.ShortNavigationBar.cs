using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // ShortNavigationBar.kt consumes NavigationBarTokens, NavigationBarVerticalItemTokens and NavigationBarHorizontalItemTokens.
        public static class ShortNavigationBar
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.short-navigation-bar.container.color");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.short-navigation-bar.tall-container.height");
            public static readonly MaterialTokenKey<Foundation.Typography.TextStyle> LabelTextStyle = new("md.comp.short-navigation-bar.label.text-style");
            public static readonly MaterialTokenKey<double> ActiveIndicatorWidth = new("md.comp.short-navigation-bar.active-indicator.width");
            public static readonly MaterialTokenKey<double> ActiveIndicatorHeight = new("md.comp.short-navigation-bar.active-indicator.height");
            public static readonly MaterialTokenKey<CornerRadius> ActiveIndicatorShape = new("md.comp.short-navigation-bar.active-indicator.shape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.short-navigation-bar.icon.size");
            public static readonly MaterialTokenKey<Color> SelectedIconColor = new("md.comp.short-navigation-bar.selected.icon.color");
            public static readonly MaterialTokenKey<Color> SelectedTextColorTopIconPosition = new("md.comp.short-navigation-bar.selected.label.top.color");
            public static readonly MaterialTokenKey<Color> SelectedTextColorStartIconPosition = new("md.comp.short-navigation-bar.selected.label.start.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.short-navigation-bar.content.color");
            public static readonly MaterialTokenKey<double> StartIndicatorHeight = new("md.comp.short-navigation-bar.start-indicator.height");
            public static readonly MaterialTokenKey<double> StartIndicatorHorizontalPadding = new("md.comp.short-navigation-bar.start-indicator.horizontal-padding");
            public static readonly MaterialTokenKey<double> TopItemVerticalPadding = new("md.comp.short-navigation-bar.top-item.vertical-padding");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.short-navigation-bar.icon-label.space");
            public static readonly MaterialTokenKey<Color> IndicatorColor = new("md.comp.short-navigation-bar.indicator.color");
            public static readonly MaterialTokenKey<Color> UnselectedIconColor = new("md.comp.short-navigation-bar.unselected.icon.color");
            public static readonly MaterialTokenKey<Color> UnselectedTextColor = new("md.comp.short-navigation-bar.unselected.label.color");
            // Local derivations of ShortNavigationBarItemDefaults.colors, rather than upstream tokens.
            public static readonly MaterialTokenKey<Color> DisabledIconColor = new("md.comp.short-navigation-bar.disabled.icon.color");
            public static readonly MaterialTokenKey<Color> DisabledTextColor = new("md.comp.short-navigation-bar.disabled.label.color");
        }
    }
}
