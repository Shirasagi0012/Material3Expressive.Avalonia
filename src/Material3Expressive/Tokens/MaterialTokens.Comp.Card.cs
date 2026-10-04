using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class FilledCard
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.filled-card.container.color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.filled-card.container.shape");
            public static readonly MaterialTokenKey<double> ContainerElevation = new("md.comp.filled-card.container.elevation");
            public static readonly MaterialTokenKey<double> PressedContainerElevation = new("md.comp.filled-card.pressed.container.elevation");
            public static readonly MaterialTokenKey<double> FocusContainerElevation = new("md.comp.filled-card.focus.container.elevation");
            public static readonly MaterialTokenKey<double> HoverContainerElevation = new("md.comp.filled-card.hover.container.elevation");
            public static readonly MaterialTokenKey<double> DraggedContainerElevation = new("md.comp.filled-card.dragged.container.elevation");
            public static readonly MaterialTokenKey<double> DisabledContainerElevation = new("md.comp.filled-card.disabled.container.elevation");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.filled-card.disabled.container.color");
            public static readonly MaterialTokenKey<double> DisabledContainerOpacity = new("md.comp.filled-card.disabled.container.opacity");
        }
        public static class ElevatedCard
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.elevated-card.container.color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.elevated-card.container.shape");
            public static readonly MaterialTokenKey<double> ContainerElevation = new("md.comp.elevated-card.container.elevation");
            public static readonly MaterialTokenKey<double> PressedContainerElevation = new("md.comp.elevated-card.pressed.container.elevation");
            public static readonly MaterialTokenKey<double> FocusContainerElevation = new("md.comp.elevated-card.focus.container.elevation");
            public static readonly MaterialTokenKey<double> HoverContainerElevation = new("md.comp.elevated-card.hover.container.elevation");
            public static readonly MaterialTokenKey<double> DraggedContainerElevation = new("md.comp.elevated-card.dragged.container.elevation");
            public static readonly MaterialTokenKey<double> DisabledContainerElevation = new("md.comp.elevated-card.disabled.container.elevation");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.elevated-card.disabled.container.color");
            public static readonly MaterialTokenKey<double> DisabledContainerOpacity = new("md.comp.elevated-card.disabled.container.opacity");
        }
        public static class OutlinedCard
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.outlined-card.container.color");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.outlined-card.container.shape");
            public static readonly MaterialTokenKey<double> ContainerElevation = new("md.comp.outlined-card.container.elevation");
            public static readonly MaterialTokenKey<double> PressedContainerElevation = new("md.comp.outlined-card.pressed.container.elevation");
            public static readonly MaterialTokenKey<double> FocusContainerElevation = new("md.comp.outlined-card.focus.container.elevation");
            public static readonly MaterialTokenKey<double> HoverContainerElevation = new("md.comp.outlined-card.hover.container.elevation");
            public static readonly MaterialTokenKey<double> DraggedContainerElevation = new("md.comp.outlined-card.dragged.container.elevation");
            public static readonly MaterialTokenKey<double> DisabledContainerElevation = new("md.comp.outlined-card.disabled.container.elevation");
            public static readonly MaterialTokenKey<Color> OutlineColor = new("md.comp.outlined-card.outline.color");
            public static readonly MaterialTokenKey<double> OutlineWidth = new("md.comp.outlined-card.outline.width");
            public static readonly MaterialTokenKey<Color> DisabledOutlineColor = new("md.comp.outlined-card.disabled.outline.color");
            public static readonly MaterialTokenKey<double> DisabledOutlineOpacity = new("md.comp.outlined-card.disabled.outline.opacity");
        }
    }
}
