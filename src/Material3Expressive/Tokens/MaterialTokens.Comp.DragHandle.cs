using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class DragHandle
        {
            public static readonly MaterialTokenKey<Size> Size = new("md.comp.drag-handle.size");
            public static readonly MaterialTokenKey<Size> PressedSize = new("md.comp.drag-handle.pressed-size");
            public static readonly MaterialTokenKey<Size> DraggedSize = new("md.comp.drag-handle.dragged-size");
            public static readonly MaterialTokenKey<Color> Color = new("md.comp.drag-handle.color");
            public static readonly MaterialTokenKey<Color> PressedColor = new("md.comp.drag-handle.pressed-color");
            public static readonly MaterialTokenKey<Color> DraggedColor = new("md.comp.drag-handle.dragged-color");
            public static readonly MaterialTokenKey<CornerRadius> Shape = new("md.comp.drag-handle.shape");
            public static readonly MaterialTokenKey<CornerRadius> PressedShape = new("md.comp.drag-handle.pressed-shape");
            public static readonly MaterialTokenKey<CornerRadius> DraggedShape = new("md.comp.drag-handle.dragged-shape");
            public static readonly MaterialTokenKey<double> MinimumInteractiveSize = new("md.comp.drag-handle.minimum-interactive-size");
        }
    }
}
