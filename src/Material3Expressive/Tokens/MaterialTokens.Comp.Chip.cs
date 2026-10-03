using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class Chip
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.chip.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.chip.container.shape");
            public static readonly MaterialTokenKey<CornerRadius> UnselectedShape = new("md.comp.chip.unselected.shape");
            public static readonly MaterialTokenKey<CornerRadius> SelectedShape = new("md.comp.chip.selected.shape");
            public static readonly MaterialTokenKey<CornerRadius> PressedShape = new("md.comp.chip.pressed.shape");
            public static readonly MaterialTokenKey<CornerRadius> AvatarShape = new("md.comp.chip.avatar.shape");
            public static readonly MaterialTokenKey<double> AvatarOpacity = new("md.comp.chip.disabled.avatar.opacity");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.chip.icon.size");
            public static readonly MaterialTokenKey<double> AvatarSize = new("md.comp.chip.avatar.size");
            public static readonly MaterialTokenKey<TextStyle> LabelTextStyle = new("md.comp.chip.label.text-style");
            public static readonly MaterialTokenKey<Color> AssistLabelColor = new("md.comp.assist-chip.label.color");
            public static readonly MaterialTokenKey<Color> LabelColor = new("md.comp.chip.label.color");
            public static readonly MaterialTokenKey<Color> PrimaryIconColor = new("md.comp.chip.primary-icon.color");
            public static readonly MaterialTokenKey<Color> TonalIconColor = new("md.comp.chip.tonal-icon.color");
            public static readonly MaterialTokenKey<Color> SelectedContainerColor = new("md.comp.chip.selected.container.color");
            public static readonly MaterialTokenKey<Color> SelectedContentColor = new("md.comp.chip.selected.content.color");
            public static readonly MaterialTokenKey<Color> ElevatedContainerColor = new("md.comp.chip.elevated.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.chip.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.chip.disabled.content.color");
            public static readonly MaterialTokenKey<Color> OutlineColor = new("md.comp.chip.outline.color");
            public static readonly MaterialTokenKey<Color> DisabledOutlineColor = new("md.comp.chip.disabled.outline.color");
            public static readonly MaterialTokenKey<double> OutlineWidth = new("md.comp.chip.outline.width");
            public static readonly MaterialTokenKey<double> FlatElevation = new("md.comp.chip.flat.elevation");
            public static readonly MaterialTokenKey<double> FlatFilterHoverElevation = new("md.comp.filter-chip.flat.hover.elevation");
            public static readonly MaterialTokenKey<double> ElevatedElevation = new("md.comp.chip.elevated.elevation");
            public static readonly MaterialTokenKey<double> ElevatedHoverElevation = new("md.comp.chip.elevated.hover.elevation");
            public static readonly MaterialTokenKey<double> DisabledElevation = new("md.comp.chip.disabled.elevation");
            public static readonly MaterialTokenKey<double> DraggedElevation = new("md.comp.chip.dragged.elevation");
        }
    }
}
