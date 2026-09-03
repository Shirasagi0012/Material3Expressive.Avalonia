using Avalonia;
using Avalonia.Media;
using Material3ExpressiveRefactor.Foundation.Tokens;

namespace Material3ExpressiveRefactor.Tokens;

public static partial class MaterialTokens
{
    /// <summary>Component layer — the per-component slots ControlThemes actually consume (§7.1).</summary>
    public static class Comp
    {
        /// <summary>
        /// <c>md.comp.filled-button.*</c> — filled Button slots (§7.5). State-layer colors follow
        /// the §7.4 model: a shared <see cref="StateLayerColor"/> with per-state slots aliasing
        /// it, so overriding the shared one restyles every state while a per-state override only
        /// touches that state. The content slots merge upstream's label-text and icon tables
        /// (icons are not consumed yet); the merge is recorded here for future parity tracking
        /// rather than mirroring the upstream tables one-to-one.
        /// </summary>
        public static class FilledButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.filled-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.filled-button.content.color");
            public static readonly MaterialTokenKey<Color> StateLayerColor = new("md.comp.filled-button.state-layer.color");
            public static readonly MaterialTokenKey<Color> HoverStateLayerColor = new("md.comp.filled-button.hover.state-layer-color");
            public static readonly MaterialTokenKey<Color> PressedStateLayerColor = new("md.comp.filled-button.pressed.state-layer-color");
            public static readonly MaterialTokenKey<Color> FocusedStateLayerColor = new("md.comp.filled-button.focus.state-layer-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.filled-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.filled-button.disabled.content.color");

            public static readonly MaterialTokenKey<CornerRadius> ContainerShape = new("md.comp.filled-button.container.shape");
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.filled-button.container.height");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.filled-button.content-padding");

            public static readonly MaterialTokenKey<FontFamily> LabelTextFont = new("md.comp.filled-button.label-text.font");
            public static readonly MaterialTokenKey<double> LabelTextSize = new("md.comp.filled-button.label-text.size");
            public static readonly MaterialTokenKey<FontWeight> LabelTextWeight = new("md.comp.filled-button.label-text.weight");
            public static readonly MaterialTokenKey<double> LabelTextTracking = new("md.comp.filled-button.label-text.tracking");
            public static readonly MaterialTokenKey<double> LabelTextLineHeight = new("md.comp.filled-button.label-text.line-height");
        }
    }
}
