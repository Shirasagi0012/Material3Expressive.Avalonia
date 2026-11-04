using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class SearchBar
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.search-bar.container.color");
            public static readonly MaterialTokenKey<CornerRadius> DockedContainerShape = new("md.comp.search-view.docked.container.shape");
            public static readonly MaterialTokenKey<CornerRadius> DropdownContainerShape = new("md.comp.search-view.dropdown.container.shape");
            public static readonly MaterialTokenKey<double> DropdownGapSize = new("md.comp.search-view.dropdown.gap-size");
            public static readonly MaterialTokenKey<Color> DropdownScrimColor = new("md.comp.search-view.dropdown.scrim-color");
            public static readonly MaterialTokenKey<double> DropdownMaximumWindowHeightFraction = new("md.comp.search-view.dropdown.maximum-window-height-fraction");
            public static readonly MaterialTokenKey<Color> ContainedContainerColor = new("md.comp.search-view.contained.container.color");
            public static readonly MaterialTokenKey<double> ContainedInputHorizontalPadding = new("md.comp.search-view.contained.input.horizontal-padding");
            public static readonly MaterialTokenKey<double> ContainedInputTopPadding = new("md.comp.search-view.contained.input.top-padding");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ContainedAnimation = new("md.comp.search-view.contained.animation");
            public static readonly MaterialTokenKey<Color> DividerColor = new("md.comp.search-view.divider.color");
            public static readonly MaterialTokenKey<double> TonalElevation = new("md.comp.search-bar.tonal-elevation");
            public static readonly MaterialTokenKey<double> ShadowElevation = new("md.comp.search-bar.shadow-elevation");
            public static readonly MaterialTokenKey<double> DefaultWidth = new("md.comp.search-bar.default-width");
            public static readonly MaterialTokenKey<double> InputHeight = new("md.comp.search-bar.input.height");
            public static readonly MaterialTokenKey<double> FullScreenVerticalPadding = new("md.comp.search-view.full-screen.vertical-padding");
            public static readonly MaterialTokenKey<Thickness> FullScreenWindowInsets = new("md.comp.search-view.full-screen.window-insets");
            public static readonly MaterialTokenKey<double> InputMaximumWidth = new("md.comp.search-bar.input.maximum-width");
            public static readonly MaterialTokenKey<CornerRadius> InputContainerShape = new("md.comp.search-bar.input.container.shape");
            public static readonly MaterialTokenKey<Color> InputTextColor = new("md.comp.search-bar.input-text.color");
            public static readonly MaterialTokenKey<Color> LeadingIconColor = new("md.comp.search-bar.leading-icon.color");
            public static readonly MaterialTokenKey<Color> TrailingIconColor = new("md.comp.search-bar.trailing-icon.color");
            public static readonly MaterialTokenKey<Color> SupportingTextColor = new("md.comp.search-bar.supporting-text.color");
            public static readonly MaterialTokenKey<double> MinimumResultsHeight = new("md.comp.search-bar.results.minimum-height");
            public static readonly MaterialTokenKey<double> MaximumWindowHeightFraction = new("md.comp.search-bar.results.maximum-window-height-fraction");
            public static readonly MaterialTokenKey<TimeSpan> EnterDuration = new("md.comp.search-bar.enter.duration");
            public static readonly MaterialTokenKey<TimeSpan> ExitDuration = new("md.comp.search-bar.exit.duration");
            public static readonly MaterialTokenKey<TimeSpan> AnimationDelay = new("md.comp.search-bar.animation.delay");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ExpandAnimation = new("md.comp.search-bar.expand.animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> CollapseAnimation = new("md.comp.search-bar.collapse.animation");
        }
    }
}
