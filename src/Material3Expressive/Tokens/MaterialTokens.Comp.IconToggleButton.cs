using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        /// <summary>Standard vibrant toggle colors; the ordinary theme uses ambient content colors.</summary>
        public static class StandardIconToggleButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.standard-icon-toggle-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.standard-icon-toggle-button.content.color");
            public static readonly MaterialTokenKey<Color> CheckedContainerColor = new("md.comp.standard-icon-toggle-button.checked.container.color");
            public static readonly MaterialTokenKey<Color> CheckedContentColor = new("md.comp.standard-icon-toggle-button.checked.content.color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.standard-icon-toggle-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.standard-icon-toggle-button.disabled.content.color");
        }

        /// <summary>Outlined vibrant toggle colors; ordinary outlined colors retain their ambient defaults.</summary>
        public static class OutlinedIconToggleButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.outlined-icon-toggle-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.outlined-icon-toggle-button.content.color");
            public static readonly MaterialTokenKey<Color> CheckedContainerColor = new("md.comp.outlined-icon-toggle-button.checked.container.color");
            public static readonly MaterialTokenKey<Color> CheckedContentColor = new("md.comp.outlined-icon-toggle-button.checked.content.color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.outlined-icon-toggle-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.outlined-icon-toggle-button.disabled.content.color");
        }

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
