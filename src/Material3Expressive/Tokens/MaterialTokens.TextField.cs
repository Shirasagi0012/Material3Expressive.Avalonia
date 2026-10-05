using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Motion;
namespace Material3Expressive.Tokens;
public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // Shared decoration defaults for Filled, Outlined and their secure themes.
        public static class TextField
        {
            public static readonly MaterialTokenKey<double> MinimumWidth = new("md.comp.text-field.minimum-width");
            public static readonly MaterialTokenKey<double> MinimumHeight = new("md.comp.text-field.minimum-height");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.text-field.content-padding");
            public static readonly MaterialTokenKey<Thickness> WithLabelContentPadding = new("md.comp.text-field.with-label-content-padding");
            public static readonly MaterialTokenKey<SpringSpec> SpatialAnimation = new("md.comp.text-field.spatial-animation");
            public static readonly MaterialTokenKey<SpringSpec> FastEffectsAnimation = new("md.comp.text-field.fast-effects-animation");
            public static readonly MaterialTokenKey<SpringSpec> SlowEffectsAnimation = new("md.comp.text-field.slow-effects-animation");
        }

        public static class FilledTextField
        {
            public static readonly MaterialTokenKey<Color> UnfocusedInputColor = new("md.comp.filled-text-field.unfocused-input-color");
            public static readonly MaterialTokenKey<Color> UnfocusedContainerColor = new("md.comp.filled-text-field.unfocused-container-color");
            public static readonly MaterialTokenKey<Color> UnfocusedIndicatorColor = new("md.comp.filled-text-field.unfocused-indicator-color");
            public static readonly MaterialTokenKey<Color> UnfocusedLabelColor = new("md.comp.filled-text-field.unfocused-label-color");
            public static readonly MaterialTokenKey<Color> UnfocusedPlaceholderColor = new("md.comp.filled-text-field.unfocused-placeholder-color");
            public static readonly MaterialTokenKey<Color> UnfocusedLeadingIconColor = new("md.comp.filled-text-field.unfocused-leading-icon-color");
            public static readonly MaterialTokenKey<Color> UnfocusedTrailingIconColor = new("md.comp.filled-text-field.unfocused-trailing-icon-color");
            public static readonly MaterialTokenKey<Color> UnfocusedSupportingColor = new("md.comp.filled-text-field.unfocused-supporting-color");
            public static readonly MaterialTokenKey<Color> UnfocusedPrefixColor = new("md.comp.filled-text-field.unfocused-prefix-color");
            public static readonly MaterialTokenKey<Color> UnfocusedSuffixColor = new("md.comp.filled-text-field.unfocused-suffix-color");
            public static readonly MaterialTokenKey<Color> FocusedInputColor = new("md.comp.filled-text-field.focused-input-color");
            public static readonly MaterialTokenKey<Color> FocusedContainerColor = new("md.comp.filled-text-field.focused-container-color");
            public static readonly MaterialTokenKey<Color> FocusedIndicatorColor = new("md.comp.filled-text-field.focused-indicator-color");
            public static readonly MaterialTokenKey<Color> FocusedLabelColor = new("md.comp.filled-text-field.focused-label-color");
            public static readonly MaterialTokenKey<Color> FocusedPlaceholderColor = new("md.comp.filled-text-field.focused-placeholder-color");
            public static readonly MaterialTokenKey<Color> FocusedLeadingIconColor = new("md.comp.filled-text-field.focused-leading-icon-color");
            public static readonly MaterialTokenKey<Color> FocusedTrailingIconColor = new("md.comp.filled-text-field.focused-trailing-icon-color");
            public static readonly MaterialTokenKey<Color> FocusedSupportingColor = new("md.comp.filled-text-field.focused-supporting-color");
            public static readonly MaterialTokenKey<Color> FocusedPrefixColor = new("md.comp.filled-text-field.focused-prefix-color");
            public static readonly MaterialTokenKey<Color> FocusedSuffixColor = new("md.comp.filled-text-field.focused-suffix-color");
            public static readonly MaterialTokenKey<Color> DisabledInputColor = new("md.comp.filled-text-field.disabled-input-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.filled-text-field.disabled-container-color");
            public static readonly MaterialTokenKey<Color> DisabledIndicatorColor = new("md.comp.filled-text-field.disabled-indicator-color");
            public static readonly MaterialTokenKey<Color> DisabledLabelColor = new("md.comp.filled-text-field.disabled-label-color");
            public static readonly MaterialTokenKey<Color> DisabledPlaceholderColor = new("md.comp.filled-text-field.disabled-placeholder-color");
            public static readonly MaterialTokenKey<Color> DisabledLeadingIconColor = new("md.comp.filled-text-field.disabled-leading-icon-color");
            public static readonly MaterialTokenKey<Color> DisabledTrailingIconColor = new("md.comp.filled-text-field.disabled-trailing-icon-color");
            public static readonly MaterialTokenKey<Color> DisabledSupportingColor = new("md.comp.filled-text-field.disabled-supporting-color");
            public static readonly MaterialTokenKey<Color> DisabledPrefixColor = new("md.comp.filled-text-field.disabled-prefix-color");
            public static readonly MaterialTokenKey<Color> DisabledSuffixColor = new("md.comp.filled-text-field.disabled-suffix-color");
            public static readonly MaterialTokenKey<Color> ErrorInputColor = new("md.comp.filled-text-field.error-input-color");
            public static readonly MaterialTokenKey<Color> ErrorContainerColor = new("md.comp.filled-text-field.error-container-color");
            public static readonly MaterialTokenKey<Color> ErrorIndicatorColor = new("md.comp.filled-text-field.error-indicator-color");
            public static readonly MaterialTokenKey<Color> ErrorLabelColor = new("md.comp.filled-text-field.error-label-color");
            public static readonly MaterialTokenKey<Color> ErrorPlaceholderColor = new("md.comp.filled-text-field.error-placeholder-color");
            public static readonly MaterialTokenKey<Color> ErrorLeadingIconColor = new("md.comp.filled-text-field.error-leading-icon-color");
            public static readonly MaterialTokenKey<Color> ErrorTrailingIconColor = new("md.comp.filled-text-field.error-trailing-icon-color");
            public static readonly MaterialTokenKey<Color> ErrorSupportingColor = new("md.comp.filled-text-field.error-supporting-color");
            public static readonly MaterialTokenKey<Color> ErrorPrefixColor = new("md.comp.filled-text-field.error-prefix-color");
            public static readonly MaterialTokenKey<Color> ErrorSuffixColor = new("md.comp.filled-text-field.error-suffix-color");
            public static readonly MaterialTokenKey<Color> CursorColor = new("md.comp.filled-text-field.cursor-color");
            public static readonly MaterialTokenKey<Color> ErrorCursorColor = new("md.comp.filled-text-field.error-cursor-color");
            public static readonly MaterialTokenKey<Color> SelectionColor = new("md.comp.filled-text-field.selection-color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.filled-text-field.container-shape");
        }
        public static class OutlinedTextField
        {
            public static readonly MaterialTokenKey<Color> UnfocusedInputColor = new("md.comp.outlined-text-field.unfocused-input-color");
            public static readonly MaterialTokenKey<Color> UnfocusedContainerColor = new("md.comp.outlined-text-field.unfocused-container-color");
            public static readonly MaterialTokenKey<Color> UnfocusedIndicatorColor = new("md.comp.outlined-text-field.unfocused-indicator-color");
            public static readonly MaterialTokenKey<Color> UnfocusedLabelColor = new("md.comp.outlined-text-field.unfocused-label-color");
            public static readonly MaterialTokenKey<Color> UnfocusedPlaceholderColor = new("md.comp.outlined-text-field.unfocused-placeholder-color");
            public static readonly MaterialTokenKey<Color> UnfocusedLeadingIconColor = new("md.comp.outlined-text-field.unfocused-leading-icon-color");
            public static readonly MaterialTokenKey<Color> UnfocusedTrailingIconColor = new("md.comp.outlined-text-field.unfocused-trailing-icon-color");
            public static readonly MaterialTokenKey<Color> UnfocusedSupportingColor = new("md.comp.outlined-text-field.unfocused-supporting-color");
            public static readonly MaterialTokenKey<Color> UnfocusedPrefixColor = new("md.comp.outlined-text-field.unfocused-prefix-color");
            public static readonly MaterialTokenKey<Color> UnfocusedSuffixColor = new("md.comp.outlined-text-field.unfocused-suffix-color");
            public static readonly MaterialTokenKey<Color> FocusedInputColor = new("md.comp.outlined-text-field.focused-input-color");
            public static readonly MaterialTokenKey<Color> FocusedContainerColor = new("md.comp.outlined-text-field.focused-container-color");
            public static readonly MaterialTokenKey<Color> FocusedIndicatorColor = new("md.comp.outlined-text-field.focused-indicator-color");
            public static readonly MaterialTokenKey<Color> FocusedLabelColor = new("md.comp.outlined-text-field.focused-label-color");
            public static readonly MaterialTokenKey<Color> FocusedPlaceholderColor = new("md.comp.outlined-text-field.focused-placeholder-color");
            public static readonly MaterialTokenKey<Color> FocusedLeadingIconColor = new("md.comp.outlined-text-field.focused-leading-icon-color");
            public static readonly MaterialTokenKey<Color> FocusedTrailingIconColor = new("md.comp.outlined-text-field.focused-trailing-icon-color");
            public static readonly MaterialTokenKey<Color> FocusedSupportingColor = new("md.comp.outlined-text-field.focused-supporting-color");
            public static readonly MaterialTokenKey<Color> FocusedPrefixColor = new("md.comp.outlined-text-field.focused-prefix-color");
            public static readonly MaterialTokenKey<Color> FocusedSuffixColor = new("md.comp.outlined-text-field.focused-suffix-color");
            public static readonly MaterialTokenKey<Color> DisabledInputColor = new("md.comp.outlined-text-field.disabled-input-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.outlined-text-field.disabled-container-color");
            public static readonly MaterialTokenKey<Color> DisabledIndicatorColor = new("md.comp.outlined-text-field.disabled-indicator-color");
            public static readonly MaterialTokenKey<Color> DisabledLabelColor = new("md.comp.outlined-text-field.disabled-label-color");
            public static readonly MaterialTokenKey<Color> DisabledPlaceholderColor = new("md.comp.outlined-text-field.disabled-placeholder-color");
            public static readonly MaterialTokenKey<Color> DisabledLeadingIconColor = new("md.comp.outlined-text-field.disabled-leading-icon-color");
            public static readonly MaterialTokenKey<Color> DisabledTrailingIconColor = new("md.comp.outlined-text-field.disabled-trailing-icon-color");
            public static readonly MaterialTokenKey<Color> DisabledSupportingColor = new("md.comp.outlined-text-field.disabled-supporting-color");
            public static readonly MaterialTokenKey<Color> DisabledPrefixColor = new("md.comp.outlined-text-field.disabled-prefix-color");
            public static readonly MaterialTokenKey<Color> DisabledSuffixColor = new("md.comp.outlined-text-field.disabled-suffix-color");
            public static readonly MaterialTokenKey<Color> ErrorInputColor = new("md.comp.outlined-text-field.error-input-color");
            public static readonly MaterialTokenKey<Color> ErrorContainerColor = new("md.comp.outlined-text-field.error-container-color");
            public static readonly MaterialTokenKey<Color> ErrorIndicatorColor = new("md.comp.outlined-text-field.error-indicator-color");
            public static readonly MaterialTokenKey<Color> ErrorLabelColor = new("md.comp.outlined-text-field.error-label-color");
            public static readonly MaterialTokenKey<Color> ErrorPlaceholderColor = new("md.comp.outlined-text-field.error-placeholder-color");
            public static readonly MaterialTokenKey<Color> ErrorLeadingIconColor = new("md.comp.outlined-text-field.error-leading-icon-color");
            public static readonly MaterialTokenKey<Color> ErrorTrailingIconColor = new("md.comp.outlined-text-field.error-trailing-icon-color");
            public static readonly MaterialTokenKey<Color> ErrorSupportingColor = new("md.comp.outlined-text-field.error-supporting-color");
            public static readonly MaterialTokenKey<Color> ErrorPrefixColor = new("md.comp.outlined-text-field.error-prefix-color");
            public static readonly MaterialTokenKey<Color> ErrorSuffixColor = new("md.comp.outlined-text-field.error-suffix-color");
            public static readonly MaterialTokenKey<Color> CursorColor = new("md.comp.outlined-text-field.cursor-color");
            public static readonly MaterialTokenKey<Color> ErrorCursorColor = new("md.comp.outlined-text-field.error-cursor-color");
            public static readonly MaterialTokenKey<Color> SelectionColor = new("md.comp.outlined-text-field.selection-color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.outlined-text-field.container-shape");
        }
    }
}
