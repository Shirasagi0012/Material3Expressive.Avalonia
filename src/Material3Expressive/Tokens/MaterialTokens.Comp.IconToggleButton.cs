using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        public static class FilledIconToggleButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.filled-icon-toggle-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.filled-icon-toggle-button.content.color");
            public static readonly MaterialTokenKey<Color> CheckedContainerColor = new("md.comp.filled-icon-toggle-button.checked.container.color");
            public static readonly MaterialTokenKey<Color> CheckedContentColor = new("md.comp.filled-icon-toggle-button.checked.content.color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.filled-icon-toggle-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.filled-icon-toggle-button.disabled.content.color");
        }

        public static class TonalIconToggleButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.tonal-icon-toggle-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.tonal-icon-toggle-button.content.color");
            public static readonly MaterialTokenKey<Color> CheckedContainerColor = new("md.comp.tonal-icon-toggle-button.checked.container.color");
            public static readonly MaterialTokenKey<Color> CheckedContentColor = new("md.comp.tonal-icon-toggle-button.checked.content.color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.tonal-icon-toggle-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.tonal-icon-toggle-button.disabled.content.color");
        }
    }
}
