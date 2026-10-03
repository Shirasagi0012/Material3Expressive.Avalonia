using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class NavigationDrawer
        {
            public static readonly MaterialTokenKey<Color> ModalContainerColor = new("md.comp.navigation-drawer.modal.container.color");
            public static readonly MaterialTokenKey<Color> StandardContainerColor = new("md.comp.navigation-drawer.standard.container.color");
            public static readonly MaterialTokenKey<double> ContainerWidth = new("md.comp.navigation-drawer.container.width");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.navigation-drawer.container.shape");
            public static readonly MaterialTokenKey<Color> SelectedContainerColor = new("md.comp.navigation-drawer.selected.container.color");
            public static readonly MaterialTokenKey<Color> UnselectedContainerColor = new("md.comp.navigation-drawer.unselected.container.color");
            public static readonly MaterialTokenKey<Color> SelectedIconColor = new("md.comp.navigation-drawer.selected.icon.color");
            public static readonly MaterialTokenKey<Color> UnselectedIconColor = new("md.comp.navigation-drawer.unselected.icon.color");
            public static readonly MaterialTokenKey<Color> SelectedTextColor = new("md.comp.navigation-drawer.selected.text.color");
            public static readonly MaterialTokenKey<Color> UnselectedTextColor = new("md.comp.navigation-drawer.unselected.text.color");
            public static readonly MaterialTokenKey<double> ActiveIndicatorHeight = new("md.comp.navigation-drawer.active.indicator.height");
            public static readonly MaterialTokenKey<CornerRadius> ActiveIndicatorShape = new("md.comp.navigation-drawer.active.indicator.shape");
            public static readonly MaterialTokenKey<TextStyle> LabelTextStyle = new("md.comp.navigation-drawer.label.text.style");
            public static readonly MaterialTokenKey<Color> ScrimColor = new("md.comp.navigation-drawer.scrim.color");
        }
    }
}
