using Material3Expressive.Foundation.Motion;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // CheckboxDefaults.defaultCheckboxColors, fixed CheckboxTokens 14_1_0.
        public static class CheckBox
        {
            public static readonly MaterialTokenKey<Avalonia.Thickness> ContentPadding = new("md.comp.checkbox.content-padding");
            public static readonly MaterialTokenKey<SpringSpec> ShapeAnimation = new("md.comp.checkbox.shape-animation");
            public static readonly MaterialTokenKey<SpringSpec> ColorAnimation = new("md.comp.checkbox.color-animation");
            public static readonly MaterialTokenKey<SpringSpec> UncheckColorAnimation = new("md.comp.checkbox.uncheck-color-animation");

            public static readonly MaterialTokenKey<Color> CheckedCheckmarkColor = new("md.comp.checkbox.checked-checkmark-color");
            public static readonly MaterialTokenKey<Color> UncheckedCheckmarkColor = new("md.comp.checkbox.unchecked-checkmark-color");
            public static readonly MaterialTokenKey<Color> CheckedBoxColor = new("md.comp.checkbox.checked-box-color");
            public static readonly MaterialTokenKey<Color> UncheckedBoxColor = new("md.comp.checkbox.unchecked-box-color");
            public static readonly MaterialTokenKey<Color> DisabledCheckedBoxColor = new("md.comp.checkbox.disabled-checked-box-color");
            public static readonly MaterialTokenKey<Color> DisabledUncheckedBoxColor = new("md.comp.checkbox.disabled-unchecked-box-color");
            public static readonly MaterialTokenKey<Color> DisabledIndeterminateBoxColor = new("md.comp.checkbox.disabled-indeterminate-box-color");
            public static readonly MaterialTokenKey<Color> CheckedBorderColor = new("md.comp.checkbox.checked-border-color");
            public static readonly MaterialTokenKey<Color> UncheckedBorderColor = new("md.comp.checkbox.unchecked-border-color");
            public static readonly MaterialTokenKey<Color> DisabledBorderColor = new("md.comp.checkbox.disabled-border-color");
            public static readonly MaterialTokenKey<Color> DisabledUncheckedBorderColor = new("md.comp.checkbox.disabled-unchecked-border-color");
            public static readonly MaterialTokenKey<Color> DisabledIndeterminateBorderColor = new("md.comp.checkbox.disabled-indeterminate-border-color");
            public static readonly MaterialTokenKey<Color> DisabledCheckmarkColor = new("md.comp.checkbox.disabled-checkmark-color");
        }
    }
}
