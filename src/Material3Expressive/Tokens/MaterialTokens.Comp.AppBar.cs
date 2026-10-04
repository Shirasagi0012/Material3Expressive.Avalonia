using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class AppBar
        {
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
