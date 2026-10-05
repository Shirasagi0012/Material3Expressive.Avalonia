using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class AppBar
        {
            public static readonly MaterialTokenKey<HorizontalAlignment> TitleAlignment = new("md.comp.app-bar.title.alignment");
            public static readonly MaterialTokenKey<HorizontalAlignment> CenterTitleAlignment = new("md.comp.app-bar.center.title.alignment");
            public static readonly MaterialTokenKey<double> SmallExpandedHeight = new("md.comp.app-bar.small.expanded-height");
            public static readonly MaterialTokenKey<double> CollapsedHeight = new("md.comp.app-bar.collapsed-height");
            public static readonly MaterialTokenKey<double> MediumExpandedHeight = new("md.comp.app-bar.medium.expanded-height");
            public static readonly MaterialTokenKey<double> LargeExpandedHeight = new("md.comp.app-bar.large.expanded-height");
            public static readonly MaterialTokenKey<double> MediumFlexibleExpandedHeight = new("md.comp.app-bar.medium-flexible.expanded-height");
            public static readonly MaterialTokenKey<double> MediumFlexibleWithSubtitleExpandedHeight = new("md.comp.app-bar.medium-flexible.with-subtitle.expanded-height");
            public static readonly MaterialTokenKey<double> LargeFlexibleExpandedHeight = new("md.comp.app-bar.large-flexible.expanded-height");
            public static readonly MaterialTokenKey<double> LargeFlexibleWithSubtitleExpandedHeight = new("md.comp.app-bar.large-flexible.with-subtitle.expanded-height");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.app-bar.content-padding");
            public static readonly MaterialTokenKey<Thickness> WindowInsets = new("md.comp.app-bar.window-insets");
            public static readonly MaterialTokenKey<double> BottomExpandedHeight = new("md.comp.bottom-app-bar.expanded-height");
            public static readonly MaterialTokenKey<double> FlexibleBottomExpandedHeight = new("md.comp.bottom-app-bar.flexible.expanded-height");
            public static readonly MaterialTokenKey<Thickness> BottomContentPadding = new("md.comp.bottom-app-bar.content-padding");
            public static readonly MaterialTokenKey<Thickness> FlexibleBottomContentPadding = new("md.comp.bottom-app-bar.flexible.content-padding");
            public static readonly MaterialTokenKey<Thickness> BottomWindowInsets = new("md.comp.bottom-app-bar.window-insets");
            public static readonly MaterialTokenKey<double> BottomTonalElevation = new("md.comp.bottom-app-bar.tonal-elevation");
            public static readonly MaterialTokenKey<double> FlexibleBottomTonalElevation = new("md.comp.bottom-app-bar.flexible.tonal-elevation");
            public static readonly MaterialTokenKey<Components.AppBarArrangement> BottomArrangement = new("md.comp.bottom-app-bar.arrangement");
            public static readonly MaterialTokenKey<Components.AppBarArrangement> FlexibleBottomArrangement = new("md.comp.bottom-app-bar.flexible.arrangement");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ContainerColorAnimation = new("md.comp.app-bar.container-color.animation");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.app-bar.container.color");
            public static readonly MaterialTokenKey<Color> ScrolledContainerColor = new("md.comp.app-bar.scrolled-container.color");
            public static readonly MaterialTokenKey<Color> NavigationIconColor = new("md.comp.app-bar.navigation-icon.color");
            public static readonly MaterialTokenKey<Color> TitleColor = new("md.comp.app-bar.title.color");
            public static readonly MaterialTokenKey<Color> SubtitleColor = new("md.comp.app-bar.subtitle.color");
            public static readonly MaterialTokenKey<Color> ActionIconColor = new("md.comp.app-bar.action-icon.color");
            public static readonly MaterialTokenKey<Color> BottomContainerColor = new("md.comp.bottom-app-bar.container.color");
            public static readonly MaterialTokenKey<TextStyle> SmallTitleStyle = new("md.comp.app-bar.small.title.style");
            public static readonly MaterialTokenKey<TextStyle> SmallSubtitleStyle = new("md.comp.app-bar.small.subtitle.style");
            public static readonly MaterialTokenKey<TextStyle> MediumTitleStyle = new("md.comp.app-bar.medium.title.style");
            public static readonly MaterialTokenKey<TextStyle> LargeTitleStyle = new("md.comp.app-bar.large.title.style");
            public static readonly MaterialTokenKey<TextStyle> MediumFlexibleTitleStyle = new("md.comp.app-bar.medium-flexible.title.style");
            public static readonly MaterialTokenKey<TextStyle> MediumFlexibleSubtitleStyle = new("md.comp.app-bar.medium-flexible.subtitle.style");
            public static readonly MaterialTokenKey<TextStyle> LargeFlexibleTitleStyle = new("md.comp.app-bar.large-flexible.title.style");
            public static readonly MaterialTokenKey<TextStyle> LargeFlexibleSubtitleStyle = new("md.comp.app-bar.large-flexible.subtitle.style");
        }
    }
}
