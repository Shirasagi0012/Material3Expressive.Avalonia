using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class BottomSheet
        {
            public static readonly MaterialTokenKey<Color> ScrimColor = new("md.comp.bottom-sheet.scrim.color");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ScrimAnimation = new("md.comp.bottom-sheet.scrim.animation");
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.bottom-sheet.container.color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.bottom-sheet.container.shape");
            public static readonly MaterialTokenKey<double> TonalElevation = new("md.comp.bottom-sheet.tonal-elevation");
            public static readonly MaterialTokenKey<double> PeekHeight = new("md.comp.bottom-sheet.peek-height");
            public static readonly MaterialTokenKey<double> MaxWidth = new("md.comp.bottom-sheet.max-width");
            public static readonly MaterialTokenKey<Thickness> WindowInsets = new("md.comp.bottom-sheet.window-insets");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> ShowAnimation = new("md.comp.bottom-sheet.show-animation");
            public static readonly MaterialTokenKey<Foundation.Motion.SpringSpec> HideAnimation = new("md.comp.bottom-sheet.hide-animation");
            public static readonly MaterialTokenKey<double> DragHandleWidth = new("md.comp.bottom-sheet.drag-handle.width");
            public static readonly MaterialTokenKey<double> DragHandleHeight = new("md.comp.bottom-sheet.drag-handle.height");
            public static readonly MaterialTokenKey<Color> DragHandleColor = new("md.comp.bottom-sheet.drag-handle.color");
            public static readonly MaterialTokenKey<CornerRadius> DragHandleShape = new("md.comp.bottom-sheet.drag-handle.shape");
        }
    }
}
