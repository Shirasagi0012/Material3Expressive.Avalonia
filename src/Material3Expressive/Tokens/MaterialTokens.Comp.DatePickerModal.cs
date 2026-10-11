using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class DatePickerModal
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.date-picker-modal.container-color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.date-picker-modal.container-shape");
            public static readonly MaterialTokenKey<double> ContainerWidth = new("md.comp.date-picker-modal.container-width");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.date-picker-modal.container-height");
        }
    }
}
