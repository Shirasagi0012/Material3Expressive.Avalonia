using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // NavigationBar.kt consumes NavigationBarTokens and NavigationBarVerticalItemTokens.
        public static class NavigationBar
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.navigation-bar.container.color");
            public static readonly MaterialTokenKey<double> TallContainerHeight = new("md.comp.navigation-bar.tall-container.height");
            // NavigationBarDefaults.Elevation is Level0, unlike the unused generated ContainerElevation.
            public static readonly MaterialTokenKey<double> DefaultTonalElevation = new("md.comp.navigation-bar.default-tonal-elevation");
            public static readonly MaterialTokenKey<Foundation.Typography.TextStyle> LabelTextStyle = new("md.comp.navigation-bar.label.text-style");
            public static readonly MaterialTokenKey<double> ActiveIndicatorWidth = new("md.comp.navigation-bar.active-indicator.width");
            public static readonly MaterialTokenKey<double> ActiveIndicatorHeight = new("md.comp.navigation-bar.active-indicator.height");
            public static readonly MaterialTokenKey<CornerRadius> ActiveIndicatorShape = new("md.comp.navigation-bar.active-indicator.shape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.navigation-bar.icon.size");
            public static readonly MaterialTokenKey<Color> SelectedIconColor = new("md.comp.navigation-bar.selected.icon.color");
            public static readonly MaterialTokenKey<Color> SelectedTextColor = new("md.comp.navigation-bar.selected.label.color");
            public static readonly MaterialTokenKey<Color> IndicatorColor = new("md.comp.navigation-bar.indicator.color");
            public static readonly MaterialTokenKey<Color> UnselectedIconColor = new("md.comp.navigation-bar.unselected.icon.color");
            public static readonly MaterialTokenKey<Color> UnselectedTextColor = new("md.comp.navigation-bar.unselected.label.color");
            // Local derivations of NavigationBarItemDefaults.colors, rather than upstream tokens.
            public static readonly MaterialTokenKey<Color> DisabledIconColor = new("md.comp.navigation-bar.disabled.icon.color");
            public static readonly MaterialTokenKey<Color> DisabledTextColor = new("md.comp.navigation-bar.disabled.label.color");
        }
    }
}
