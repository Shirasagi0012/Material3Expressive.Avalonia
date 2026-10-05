using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // WideNavigationRail.kt and the fixed NavigationRail{Collapsed,Expanded,HorizontalItem,
        // VerticalItem,BaselineItem,Color}Tokens. Default derivations keep live system references.
        public static class WideNavigationRail
        {
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> LayoutAnimation = new("md.comp.wide-navigation-rail.layout-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ModalWidthAnimation = new("md.comp.wide-navigation-rail.modal-width-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> StateAnimation = new("md.comp.wide-navigation-rail.state-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ItemSpatialAnimation = new("md.comp.wide-navigation-rail.item-spatial-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ItemEffectsAnimation = new("md.comp.wide-navigation-rail.item-effects-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ModalEffectsAnimation = new("md.comp.wide-navigation-rail.modal-effects-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ModalOffsetAnimation = new("md.comp.wide-navigation-rail.modal-offset-animation");
            public static readonly MaterialTokenKey<Avalonia.Thickness> WindowInsets = new("md.comp.wide-navigation-rail.window-insets");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.wide-navigation-rail.container.color");
            public static readonly MaterialTokenKey<Color> ModalContainerColor = new("md.comp.wide-navigation-rail.modal-container.color");
            public static readonly MaterialTokenKey<Color> ModalScrimColor = new("md.comp.wide-navigation-rail.modal-scrim.color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.wide-navigation-rail.container.shape");
            public static readonly MaterialTokenKey<CornerRadius> ModalContainerShape = new("md.comp.wide-navigation-rail.modal-container.shape");
            public static readonly MaterialTokenKey<double> CollapsedContainerWidth = new("md.comp.wide-navigation-rail.collapsed-container.width");
            public static readonly MaterialTokenKey<double> ExpandedContainerMinWidth = new("md.comp.wide-navigation-rail.expanded-container.min-width");
            public static readonly MaterialTokenKey<double> ExpandedContainerMaxWidth = new("md.comp.wide-navigation-rail.expanded-container.max-width");
            public static readonly MaterialTokenKey<double> TopSpace = new("md.comp.wide-navigation-rail.top-space");
            public static readonly MaterialTokenKey<double> HeaderSpace = new("md.comp.wide-navigation-rail.header-space");
            public static readonly MaterialTokenKey<double> CollapsedItemMinHeight = new("md.comp.wide-navigation-rail.collapsed-item.min-height");
            public static readonly MaterialTokenKey<double> CollapsedItemVerticalSpace = new("md.comp.wide-navigation-rail.collapsed-item.vertical-space");
            public static readonly MaterialTokenKey<double> ItemHorizontalPadding = new("md.comp.wide-navigation-rail.item.horizontal-padding");
            public static readonly MaterialTokenKey<double> ActiveIndicatorWidth = new("md.comp.wide-navigation-rail.active-indicator.width");
            public static readonly MaterialTokenKey<double> TopIconIndicatorHeight = new("md.comp.wide-navigation-rail.top-icon-indicator.height");
            public static readonly MaterialTokenKey<double> StartIconIndicatorHeight = new("md.comp.wide-navigation-rail.start-icon-indicator.height");
            public static readonly MaterialTokenKey<double> StartIconIndicatorLeadingSpace = new("md.comp.wide-navigation-rail.start-icon-indicator.leading-space");
            public static readonly MaterialTokenKey<CornerRadius> ActiveIndicatorShape = new("md.comp.wide-navigation-rail.active-indicator.shape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.wide-navigation-rail.icon.size");
            public static readonly MaterialTokenKey<double> TopIconLabelSpace = new("md.comp.wide-navigation-rail.top-icon.label-space");
            public static readonly MaterialTokenKey<double> StartIconLabelSpace = new("md.comp.wide-navigation-rail.start-icon.label-space");
            public static readonly MaterialTokenKey<TextStyle> TopIconLabelTextStyle = new("md.comp.wide-navigation-rail.top-icon.label-text-style");
            public static readonly MaterialTokenKey<TextStyle> StartIconLabelTextStyle = new("md.comp.wide-navigation-rail.start-icon.label-text-style");
            public static readonly MaterialTokenKey<Color> SelectedIconColor = new("md.comp.wide-navigation-rail.selected.icon.color");
            public static readonly MaterialTokenKey<Color> SelectedTextColorTopIconPosition = new("md.comp.wide-navigation-rail.selected.top-icon.label.color");
            public static readonly MaterialTokenKey<Color> SelectedTextColorStartIconPosition = new("md.comp.wide-navigation-rail.selected.start-icon.label.color");
            public static readonly MaterialTokenKey<Color> IndicatorColor = new("md.comp.wide-navigation-rail.indicator.color");
            public static readonly MaterialTokenKey<Color> UnselectedIconColor = new("md.comp.wide-navigation-rail.unselected.icon.color");
            public static readonly MaterialTokenKey<Color> UnselectedTextColor = new("md.comp.wide-navigation-rail.unselected.label.color");
            public static readonly MaterialTokenKey<Color> DisabledIconColor = new("md.comp.wide-navigation-rail.disabled.icon.color");
            public static readonly MaterialTokenKey<Color> DisabledTextColor = new("md.comp.wide-navigation-rail.disabled.label.color");
        }
    }
}

