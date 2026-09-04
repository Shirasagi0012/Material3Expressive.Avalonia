using Avalonia;
using Avalonia.Media;
using Material3ExpressiveRefactor.Foundation.Tokens;

namespace Material3ExpressiveRefactor.Tokens;

public static partial class MaterialTokens
{
    /// <summary>Component layer — the per-component slots ControlThemes actually consume (§7.1).</summary>
    public static partial class Comp
    {
        // Button family (design doc §7.5), split along upstream's two orthogonal axes:
        // style tables (colors/elevation/outline, per style) and size tables (metrics/shape/
        // typography, shared by every style). All style tables follow the §7.4 state-layer model:
        // a shared StateLayerColor with per-state slots aliasing it, so overriding the shared one
        // restyles every state while a per-state override only touches that state. Content slots
        // merge upstream's label-text and icon tables (icons are not consumed yet); the merge is
        // recorded for future parity tracking rather than mirroring the upstream tables
        // one-to-one. Toggle-only tokens (Selected*/Unselected*) are not transcribed — toggle
        // buttons are their own component slice.

        /// <summary>
        /// <c>md.comp.filled-button.*</c> — filled Button color slots (upstream FilledButtonTokens
        /// v0_11_0). Upstream's elevation tokens (resting 0, hover 1) are recorded but not
        /// transcribed: desktop hover shadows read as noise, and the motion slice can revisit.
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
        }

        /// <summary>
        /// <c>md.comp.elevated-button.*</c> — elevated Button color and elevation slots (upstream
        /// ElevatedButtonTokens v0_11_0). Elevation is this style's defining feature; the per-state
        /// levels are DIPs aliasing <c>md.sys.elevation.level*</c>, consumed by the template part's
        /// <c>MaterialSurface.ShadowElevation</c>.
        /// </summary>
        public static class ElevatedButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.elevated-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.elevated-button.content.color");
            public static readonly MaterialTokenKey<Color> StateLayerColor = new("md.comp.elevated-button.state-layer.color");
            public static readonly MaterialTokenKey<Color> HoverStateLayerColor = new("md.comp.elevated-button.hover.state-layer-color");
            public static readonly MaterialTokenKey<Color> PressedStateLayerColor = new("md.comp.elevated-button.pressed.state-layer-color");
            public static readonly MaterialTokenKey<Color> FocusedStateLayerColor = new("md.comp.elevated-button.focus.state-layer-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.elevated-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.elevated-button.disabled.content.color");

            public static readonly MaterialTokenKey<double> ContainerElevation = new("md.comp.elevated-button.container.elevation");
            public static readonly MaterialTokenKey<double> HoveredContainerElevation = new("md.comp.elevated-button.hover.container.elevation");
            public static readonly MaterialTokenKey<double> FocusedContainerElevation = new("md.comp.elevated-button.focus.container.elevation");
            public static readonly MaterialTokenKey<double> PressedContainerElevation = new("md.comp.elevated-button.pressed.container.elevation");
            public static readonly MaterialTokenKey<double> DisabledContainerElevation = new("md.comp.elevated-button.disabled.container.elevation");
        }

        /// <summary>
        /// <c>md.comp.tonal-button.*</c> — filled tonal Button color slots (upstream
        /// TonalButtonTokens v0_11_0). Elevation tokens omitted like the filled style's.
        /// </summary>
        public static class TonalButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.tonal-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.tonal-button.content.color");
            public static readonly MaterialTokenKey<Color> StateLayerColor = new("md.comp.tonal-button.state-layer.color");
            public static readonly MaterialTokenKey<Color> HoverStateLayerColor = new("md.comp.tonal-button.hover.state-layer-color");
            public static readonly MaterialTokenKey<Color> PressedStateLayerColor = new("md.comp.tonal-button.pressed.state-layer-color");
            public static readonly MaterialTokenKey<Color> FocusedStateLayerColor = new("md.comp.tonal-button.focus.state-layer-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.tonal-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.tonal-button.disabled.content.color");
        }

        /// <summary>
        /// <c>md.comp.outlined-button.*</c> — outlined Button color and outline slots (upstream
        /// OutlinedButtonTokens v0_11_0). The container is transparent until disabled, and the
        /// outline width lives in the size tables (<c>*.outlined-outline-width</c>), matching
        /// upstream. Note v0_11_0 moved the content color from Primary to OnSurfaceVariant and the
        /// outline from Outline to OutlineVariant — transcribed as upstream has them.
        /// </summary>
        public static class OutlinedButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.outlined-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.outlined-button.content.color");
            public static readonly MaterialTokenKey<Color> StateLayerColor = new("md.comp.outlined-button.state-layer.color");
            public static readonly MaterialTokenKey<Color> HoverStateLayerColor = new("md.comp.outlined-button.hover.state-layer-color");
            public static readonly MaterialTokenKey<Color> PressedStateLayerColor = new("md.comp.outlined-button.pressed.state-layer-color");
            public static readonly MaterialTokenKey<Color> FocusedStateLayerColor = new("md.comp.outlined-button.focus.state-layer-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.outlined-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.outlined-button.disabled.content.color");

            public static readonly MaterialTokenKey<Color> OutlineColor = new("md.comp.outlined-button.outline.color");
            public static readonly MaterialTokenKey<Color> DisabledOutlineColor = new("md.comp.outlined-button.disabled.outline.color");
        }

        /// <summary>
        /// <c>md.comp.text-button.*</c> — text Button color slots (upstream TextButtonTokens
        /// v0_11_0). Upstream's LabelColor token reads OnSurfaceVariant but the composable
        /// overrides it to Primary with a "TODO replace once the token is corrected" — the
        /// composable is the source of truth here, so ContentColor maps to Primary.
        /// </summary>
        public static class TextButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.text-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.text-button.content.color");
            public static readonly MaterialTokenKey<Color> StateLayerColor = new("md.comp.text-button.state-layer.color");
            public static readonly MaterialTokenKey<Color> HoverStateLayerColor = new("md.comp.text-button.hover.state-layer-color");
            public static readonly MaterialTokenKey<Color> PressedStateLayerColor = new("md.comp.text-button.pressed.state-layer-color");
            public static readonly MaterialTokenKey<Color> FocusedStateLayerColor = new("md.comp.text-button.focus.state-layer-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.text-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.text-button.disabled.content.color");
        }

        // Size tables (upstream ButtonXSmall/Small/Medium/Large/XLargeTokens v0_11_0), shared by
        // all five styles. ContainerShapeRound is the resting shape; ContainerShapeSquare is the
        // opt-in square variant and PressedContainerShape the press morph target — both are
        // transcribed now but consumed later (square opt-in and the motion slice respectively).
        // Typography follows the composable's textStyleFor mapping (LabelLarge below medium,
        // TitleMedium at medium, HeadlineSmall at large, HeadlineLarge at extra large), which is
        // code rather than a token table upstream.

        /// <summary><c>md.comp.button-xsmall.*</c> — extra small button metrics (32dp).</summary>
        public static class ButtonXSmall
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.button-xsmall.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeRound = new("md.comp.button-xsmall.container.shape-round");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeSquare = new("md.comp.button-xsmall.container.shape-square");
            public static readonly MaterialTokenKey<CornerRadius> PressedContainerShape = new("md.comp.button-xsmall.pressed.container.shape");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.button-xsmall.content-padding");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.button-xsmall.icon.size");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.button-xsmall.icon-label-space");
            public static readonly MaterialTokenKey<Thickness> OutlinedOutlineWidth = new("md.comp.button-xsmall.outlined-outline-width");

            public static readonly MaterialTokenKey<FontFamily> LabelTextFont = new("md.comp.button-xsmall.label-text.font");
            public static readonly MaterialTokenKey<double> LabelTextSize = new("md.comp.button-xsmall.label-text.size");
            public static readonly MaterialTokenKey<FontWeight> LabelTextWeight = new("md.comp.button-xsmall.label-text.weight");
            public static readonly MaterialTokenKey<double> LabelTextTracking = new("md.comp.button-xsmall.label-text.tracking");
            public static readonly MaterialTokenKey<double> LabelTextLineHeight = new("md.comp.button-xsmall.label-text.line-height");
        }

        /// <summary><c>md.comp.button-small.*</c> — small button metrics (40dp), the default size.</summary>
        public static class ButtonSmall
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.button-small.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeRound = new("md.comp.button-small.container.shape-round");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeSquare = new("md.comp.button-small.container.shape-square");
            public static readonly MaterialTokenKey<CornerRadius> PressedContainerShape = new("md.comp.button-small.pressed.container.shape");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.button-small.content-padding");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.button-small.icon.size");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.button-small.icon-label-space");
            public static readonly MaterialTokenKey<Thickness> OutlinedOutlineWidth = new("md.comp.button-small.outlined-outline-width");

            public static readonly MaterialTokenKey<FontFamily> LabelTextFont = new("md.comp.button-small.label-text.font");
            public static readonly MaterialTokenKey<double> LabelTextSize = new("md.comp.button-small.label-text.size");
            public static readonly MaterialTokenKey<FontWeight> LabelTextWeight = new("md.comp.button-small.label-text.weight");
            public static readonly MaterialTokenKey<double> LabelTextTracking = new("md.comp.button-small.label-text.tracking");
            public static readonly MaterialTokenKey<double> LabelTextLineHeight = new("md.comp.button-small.label-text.line-height");
        }

        /// <summary><c>md.comp.button-medium.*</c> — medium button metrics (56dp).</summary>
        public static class ButtonMedium
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.button-medium.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeRound = new("md.comp.button-medium.container.shape-round");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeSquare = new("md.comp.button-medium.container.shape-square");
            public static readonly MaterialTokenKey<CornerRadius> PressedContainerShape = new("md.comp.button-medium.pressed.container.shape");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.button-medium.content-padding");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.button-medium.icon.size");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.button-medium.icon-label-space");
            public static readonly MaterialTokenKey<Thickness> OutlinedOutlineWidth = new("md.comp.button-medium.outlined-outline-width");

            public static readonly MaterialTokenKey<FontFamily> LabelTextFont = new("md.comp.button-medium.label-text.font");
            public static readonly MaterialTokenKey<double> LabelTextSize = new("md.comp.button-medium.label-text.size");
            public static readonly MaterialTokenKey<FontWeight> LabelTextWeight = new("md.comp.button-medium.label-text.weight");
            public static readonly MaterialTokenKey<double> LabelTextTracking = new("md.comp.button-medium.label-text.tracking");
            public static readonly MaterialTokenKey<double> LabelTextLineHeight = new("md.comp.button-medium.label-text.line-height");
        }

        /// <summary><c>md.comp.button-large.*</c> — large button metrics (96dp).</summary>
        public static class ButtonLarge
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.button-large.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeRound = new("md.comp.button-large.container.shape-round");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeSquare = new("md.comp.button-large.container.shape-square");
            public static readonly MaterialTokenKey<CornerRadius> PressedContainerShape = new("md.comp.button-large.pressed.container.shape");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.button-large.content-padding");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.button-large.icon.size");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.button-large.icon-label-space");
            public static readonly MaterialTokenKey<Thickness> OutlinedOutlineWidth = new("md.comp.button-large.outlined-outline-width");

            public static readonly MaterialTokenKey<FontFamily> LabelTextFont = new("md.comp.button-large.label-text.font");
            public static readonly MaterialTokenKey<double> LabelTextSize = new("md.comp.button-large.label-text.size");
            public static readonly MaterialTokenKey<FontWeight> LabelTextWeight = new("md.comp.button-large.label-text.weight");
            public static readonly MaterialTokenKey<double> LabelTextTracking = new("md.comp.button-large.label-text.tracking");
            public static readonly MaterialTokenKey<double> LabelTextLineHeight = new("md.comp.button-large.label-text.line-height");
        }

        /// <summary><c>md.comp.button-xlarge.*</c> — extra large button metrics (136dp).</summary>
        public static class ButtonXLarge
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.button-xlarge.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeRound = new("md.comp.button-xlarge.container.shape-round");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeSquare = new("md.comp.button-xlarge.container.shape-square");
            public static readonly MaterialTokenKey<CornerRadius> PressedContainerShape = new("md.comp.button-xlarge.pressed.container.shape");
            public static readonly MaterialTokenKey<Thickness> ContentPadding = new("md.comp.button-xlarge.content-padding");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.button-xlarge.icon.size");
            public static readonly MaterialTokenKey<double> IconLabelSpace = new("md.comp.button-xlarge.icon-label-space");
            public static readonly MaterialTokenKey<Thickness> OutlinedOutlineWidth = new("md.comp.button-xlarge.outlined-outline-width");

            public static readonly MaterialTokenKey<FontFamily> LabelTextFont = new("md.comp.button-xlarge.label-text.font");
            public static readonly MaterialTokenKey<double> LabelTextSize = new("md.comp.button-xlarge.label-text.size");
            public static readonly MaterialTokenKey<FontWeight> LabelTextWeight = new("md.comp.button-xlarge.label-text.weight");
            public static readonly MaterialTokenKey<double> LabelTextTracking = new("md.comp.button-xlarge.label-text.tracking");
            public static readonly MaterialTokenKey<double> LabelTextLineHeight = new("md.comp.button-xlarge.label-text.line-height");
        }
    }
}
