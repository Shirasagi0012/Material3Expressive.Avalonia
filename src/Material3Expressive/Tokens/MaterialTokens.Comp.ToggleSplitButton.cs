using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class FilledToggleButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.filled-toggle-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.filled-toggle-button.content.color");
            public static readonly MaterialTokenKey<Color> CheckedContainerColor = new("md.comp.filled-toggle-button.checked.container.color");
            public static readonly MaterialTokenKey<Color> CheckedContentColor = new("md.comp.filled-toggle-button.checked.content.color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.filled-toggle-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.filled-toggle-button.disabled.content.color");
        }
        public static class ElevatedToggleButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.elevated-toggle-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.elevated-toggle-button.content.color");
            public static readonly MaterialTokenKey<Color> CheckedContainerColor = new("md.comp.elevated-toggle-button.checked.container.color");
            public static readonly MaterialTokenKey<Color> CheckedContentColor = new("md.comp.elevated-toggle-button.checked.content.color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.elevated-toggle-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.elevated-toggle-button.disabled.content.color");
        }
        public static class TonalToggleButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.tonal-toggle-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.tonal-toggle-button.content.color");
            public static readonly MaterialTokenKey<Color> CheckedContainerColor = new("md.comp.tonal-toggle-button.checked.container.color");
            public static readonly MaterialTokenKey<Color> CheckedContentColor = new("md.comp.tonal-toggle-button.checked.content.color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.tonal-toggle-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.tonal-toggle-button.disabled.content.color");
        }
        public static class OutlinedToggleButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.outlined-toggle-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.outlined-toggle-button.content.color");
            public static readonly MaterialTokenKey<Color> CheckedContainerColor = new("md.comp.outlined-toggle-button.checked.container.color");
            public static readonly MaterialTokenKey<Color> CheckedContentColor = new("md.comp.outlined-toggle-button.checked.content.color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.outlined-toggle-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.outlined-toggle-button.disabled.content.color");
        }
        public static class ToggleButton
        {
            public static readonly MaterialTokenKey<SpringSpec> ShapeAnimation = new("md.comp.toggle-button.shape.animation");
            public static readonly MaterialTokenKey<SpringSpec> BorderWidthAnimation = new("md.comp.toggle-button.border-width.animation");
            public static readonly MaterialTokenKey<SpringSpec> BorderColorAnimation = new("md.comp.toggle-button.border-color.animation");
            public static readonly MaterialTokenKey<CornerRadius> SmallPressedShape = new("md.comp.toggle-button.small.pressed.shape");
            public static readonly MaterialTokenKey<double> ExtraSmallIconSpacing = new("md.comp.toggle-button.extra-small.icon.spacing");
        }
        public static class SplitButton
        {
            public static readonly MaterialTokenKey<double> Spacing = new("md.comp.split-button.spacing");
        }
        public static class SplitButtonXSmall
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.split-button.xsmall.container.height");
            public static readonly MaterialTokenKey<Thickness> LeadingPadding = new("md.comp.split-button.xsmall.leading.content.padding");
            public static readonly MaterialTokenKey<Thickness> TrailingPadding = new("md.comp.split-button.xsmall.trailing.content.padding");
            public static readonly MaterialTokenKey<double> TrailingIconSize = new("md.comp.split-button.xsmall.trailing.icon.size");
            public static readonly MaterialTokenKey<CornerRadius> InnerCornerRadius = new("md.comp.split-button.xsmall.inner.corner.radius");
            public static readonly MaterialTokenKey<CornerRadius> PressedInnerCornerRadius = new("md.comp.split-button.xsmall.pressed.inner.corner.radius");
        }
        public static class SplitButtonSmall
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.split-button.small.container.height");
            public static readonly MaterialTokenKey<Thickness> LeadingPadding = new("md.comp.split-button.small.leading.content.padding");
            public static readonly MaterialTokenKey<Thickness> TrailingPadding = new("md.comp.split-button.small.trailing.content.padding");
            public static readonly MaterialTokenKey<double> TrailingIconSize = new("md.comp.split-button.small.trailing.icon.size");
            public static readonly MaterialTokenKey<CornerRadius> InnerCornerRadius = new("md.comp.split-button.small.inner.corner.radius");
            public static readonly MaterialTokenKey<CornerRadius> PressedInnerCornerRadius = new("md.comp.split-button.small.pressed.inner.corner.radius");
        }
        public static class SplitButtonMedium
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.split-button.medium.container.height");
            public static readonly MaterialTokenKey<Thickness> LeadingPadding = new("md.comp.split-button.medium.leading.content.padding");
            public static readonly MaterialTokenKey<Thickness> TrailingPadding = new("md.comp.split-button.medium.trailing.content.padding");
            public static readonly MaterialTokenKey<double> TrailingIconSize = new("md.comp.split-button.medium.trailing.icon.size");
            public static readonly MaterialTokenKey<CornerRadius> InnerCornerRadius = new("md.comp.split-button.medium.inner.corner.radius");
            public static readonly MaterialTokenKey<CornerRadius> PressedInnerCornerRadius = new("md.comp.split-button.medium.pressed.inner.corner.radius");
        }
        public static class SplitButtonLarge
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.split-button.large.container.height");
            public static readonly MaterialTokenKey<Thickness> LeadingPadding = new("md.comp.split-button.large.leading.content.padding");
            public static readonly MaterialTokenKey<Thickness> TrailingPadding = new("md.comp.split-button.large.trailing.content.padding");
            public static readonly MaterialTokenKey<double> TrailingIconSize = new("md.comp.split-button.large.trailing.icon.size");
            public static readonly MaterialTokenKey<CornerRadius> InnerCornerRadius = new("md.comp.split-button.large.inner.corner.radius");
            public static readonly MaterialTokenKey<CornerRadius> PressedInnerCornerRadius = new("md.comp.split-button.large.pressed.inner.corner.radius");
        }
        public static class SplitButtonXLarge
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.split-button.xlarge.container.height");
            public static readonly MaterialTokenKey<Thickness> LeadingPadding = new("md.comp.split-button.xlarge.leading.content.padding");
            public static readonly MaterialTokenKey<Thickness> TrailingPadding = new("md.comp.split-button.xlarge.trailing.content.padding");
            public static readonly MaterialTokenKey<double> TrailingIconSize = new("md.comp.split-button.xlarge.trailing.icon.size");
            public static readonly MaterialTokenKey<CornerRadius> InnerCornerRadius = new("md.comp.split-button.xlarge.inner.corner.radius");
            public static readonly MaterialTokenKey<CornerRadius> PressedInnerCornerRadius = new("md.comp.split-button.xlarge.pressed.inner.corner.radius");
        }
    }
}
