using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // RadioButtonDefaults.defaultRadioButtonColors, fixed RadioButtonTokens v0_117.
        public static class RadioButton
        {
            public static readonly MaterialTokenKey<Color> SelectedColor = new("md.comp.radio-button.selected-color");
            public static readonly MaterialTokenKey<Color> UnselectedColor = new("md.comp.radio-button.unselected-color");
            public static readonly MaterialTokenKey<Color> DisabledSelectedColor = new("md.comp.radio-button.disabled-selected-color");
            public static readonly MaterialTokenKey<Color> DisabledUnselectedColor = new("md.comp.radio-button.disabled-unselected-color");
        }
    }
}
