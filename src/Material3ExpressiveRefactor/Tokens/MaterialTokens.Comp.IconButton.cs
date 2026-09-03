using Avalonia;
using Avalonia.Media;
using Material3ExpressiveRefactor.Foundation.Tokens;

namespace Material3ExpressiveRefactor.Tokens;

public static partial class MaterialTokens
{
    public static partial class Comp
    {
        // IconButton family, split along upstream's two orthogonal axes like the Button family:
        // style tables (colors/outline, per style) and size tables (metrics/shape, shared by every
        // style). The style tables follow the §7.4 state-layer model with a shared StateLayerColor
        // aliased per state. Toggle-only tokens (Selected*/Unselected* colors and
        // SelectedContainerShape*) are not transcribed — the icon toggle button is its own slice.
        // No elevation slots: every icon button style is flat upstream.

        /// <summary>
        /// <c>md.comp.standard-icon-button.*</c> — standard IconButton color slots (upstream
        /// StandardIconButtonTokens 14_1_0). Recorded deviation: the plain upstream composable
        /// defaults the content to the ambient LocalContentColor and only the "vibrant" variant
        /// uses the token table (OnSurfaceVariant). The table value is taken here because the
        /// state-layer and disabled derivations need a concrete color to hang off — the same
        /// reason the vibrant variant exists upstream.
        /// </summary>
        public static class StandardIconButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.standard-icon-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.standard-icon-button.content.color");
            public static readonly MaterialTokenKey<Color> StateLayerColor = new("md.comp.standard-icon-button.state-layer.color");
            public static readonly MaterialTokenKey<Color> HoverStateLayerColor = new("md.comp.standard-icon-button.hover.state-layer-color");
            public static readonly MaterialTokenKey<Color> PressedStateLayerColor = new("md.comp.standard-icon-button.pressed.state-layer-color");
            public static readonly MaterialTokenKey<Color> FocusedStateLayerColor = new("md.comp.standard-icon-button.focus.state-layer-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.standard-icon-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.standard-icon-button.disabled.content.color");
        }

        /// <summary>
        /// <c>md.comp.filled-icon-button.*</c> — filled IconButton color slots (upstream
        /// FilledIconButtonTokens 14_1_0). Note the disabled container is OnSurface at 0.10, not
        /// the <c>md.sys.state.disabled-container-opacity</c> 0.12 the Button family uses.
        /// </summary>
        public static class FilledIconButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.filled-icon-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.filled-icon-button.content.color");
            public static readonly MaterialTokenKey<Color> StateLayerColor = new("md.comp.filled-icon-button.state-layer.color");
            public static readonly MaterialTokenKey<Color> HoverStateLayerColor = new("md.comp.filled-icon-button.hover.state-layer-color");
            public static readonly MaterialTokenKey<Color> PressedStateLayerColor = new("md.comp.filled-icon-button.pressed.state-layer-color");
            public static readonly MaterialTokenKey<Color> FocusedStateLayerColor = new("md.comp.filled-icon-button.focus.state-layer-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.filled-icon-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.filled-icon-button.disabled.content.color");
        }

        /// <summary>
        /// <c>md.comp.tonal-icon-button.*</c> — filled tonal IconButton color slots (upstream
        /// FilledTonalIconButtonTokens 14_1_0; the facade shortens the name like
        /// <see cref="TonalButton"/>).
        /// </summary>
        public static class TonalIconButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.tonal-icon-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.tonal-icon-button.content.color");
            public static readonly MaterialTokenKey<Color> StateLayerColor = new("md.comp.tonal-icon-button.state-layer.color");
            public static readonly MaterialTokenKey<Color> HoverStateLayerColor = new("md.comp.tonal-icon-button.hover.state-layer-color");
            public static readonly MaterialTokenKey<Color> PressedStateLayerColor = new("md.comp.tonal-icon-button.pressed.state-layer-color");
            public static readonly MaterialTokenKey<Color> FocusedStateLayerColor = new("md.comp.tonal-icon-button.focus.state-layer-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.tonal-icon-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.tonal-icon-button.disabled.content.color");
        }

        /// <summary>
        /// <c>md.comp.outlined-icon-button.*</c> — outlined IconButton color and outline slots
        /// (upstream OutlinedIconButtonTokens 14_1_0). Same LocalContentColor deviation as the
        /// standard style. The disabled outline is the outline color at 0.38: the raw token table
        /// lists plain OutlineVariant, but both upstream border paths (plain and vibrant) derive
        /// the disabled border as the outline color at the disabled opacity — the composable is
        /// the source of truth here, as with the Button family.
        /// </summary>
        public static class OutlinedIconButton
        {
            public static readonly MaterialTokenKey<Color> ContainerColor = new("md.comp.outlined-icon-button.container.color");
            public static readonly MaterialTokenKey<Color> ContentColor = new("md.comp.outlined-icon-button.content.color");
            public static readonly MaterialTokenKey<Color> StateLayerColor = new("md.comp.outlined-icon-button.state-layer.color");
            public static readonly MaterialTokenKey<Color> HoverStateLayerColor = new("md.comp.outlined-icon-button.hover.state-layer-color");
            public static readonly MaterialTokenKey<Color> PressedStateLayerColor = new("md.comp.outlined-icon-button.pressed.state-layer-color");
            public static readonly MaterialTokenKey<Color> FocusedStateLayerColor = new("md.comp.outlined-icon-button.focus.state-layer-color");
            public static readonly MaterialTokenKey<Color> DisabledContainerColor = new("md.comp.outlined-icon-button.disabled.container.color");
            public static readonly MaterialTokenKey<Color> DisabledContentColor = new("md.comp.outlined-icon-button.disabled.content.color");

            public static readonly MaterialTokenKey<Color> OutlineColor = new("md.comp.outlined-icon-button.outline.color");
            public static readonly MaterialTokenKey<Color> DisabledOutlineColor = new("md.comp.outlined-icon-button.disabled.outline.color");
        }

        // Size tables (upstream XSmall/Small/Medium/Large/XLargeIconButtonTokens 14_1_0, except the
        // Large table which still ships as v0_11_0). ContainerShapeRound is the resting shape;
        // ContainerShapeSquare is the opt-in square variant and PressedContainerShape the press
        // morph target — transcribed now, consumed later (square opt-in and the motion slice).
        // The three width options are recorded as their leading/trailing space pairs (container
        // width = space + icon size + space); the default option is square, so it is consumed as
        // MinWidth = ContainerHeight, and the narrow/wide XAML expression is deferred with the
        // width-option slice. The older Large revision names the default pair Uniform* — the
        // facade keeps the 14_1_0 Default* naming across all five tables.

        /// <summary><c>md.comp.icon-button-xsmall.*</c> — extra small icon button metrics (32dp, 20dp icon).</summary>
        public static class IconButtonXSmall
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.icon-button-xsmall.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeRound = new("md.comp.icon-button-xsmall.container.shape-round");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeSquare = new("md.comp.icon-button-xsmall.container.shape-square");
            public static readonly MaterialTokenKey<CornerRadius> PressedContainerShape = new("md.comp.icon-button-xsmall.pressed.container.shape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.icon-button-xsmall.icon.size");
            public static readonly MaterialTokenKey<double> DefaultLeadingSpace = new("md.comp.icon-button-xsmall.default-leading-space");
            public static readonly MaterialTokenKey<double> DefaultTrailingSpace = new("md.comp.icon-button-xsmall.default-trailing-space");
            public static readonly MaterialTokenKey<double> NarrowLeadingSpace = new("md.comp.icon-button-xsmall.narrow-leading-space");
            public static readonly MaterialTokenKey<double> NarrowTrailingSpace = new("md.comp.icon-button-xsmall.narrow-trailing-space");
            public static readonly MaterialTokenKey<double> WideLeadingSpace = new("md.comp.icon-button-xsmall.wide-leading-space");
            public static readonly MaterialTokenKey<double> WideTrailingSpace = new("md.comp.icon-button-xsmall.wide-trailing-space");
            public static readonly MaterialTokenKey<Thickness> OutlinedOutlineWidth = new("md.comp.icon-button-xsmall.outlined-outline-width");
        }

        /// <summary><c>md.comp.icon-button-small.*</c> — small icon button metrics (40dp, 24dp icon), the default size.</summary>
        public static class IconButtonSmall
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.icon-button-small.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeRound = new("md.comp.icon-button-small.container.shape-round");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeSquare = new("md.comp.icon-button-small.container.shape-square");
            public static readonly MaterialTokenKey<CornerRadius> PressedContainerShape = new("md.comp.icon-button-small.pressed.container.shape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.icon-button-small.icon.size");
            public static readonly MaterialTokenKey<double> DefaultLeadingSpace = new("md.comp.icon-button-small.default-leading-space");
            public static readonly MaterialTokenKey<double> DefaultTrailingSpace = new("md.comp.icon-button-small.default-trailing-space");
            public static readonly MaterialTokenKey<double> NarrowLeadingSpace = new("md.comp.icon-button-small.narrow-leading-space");
            public static readonly MaterialTokenKey<double> NarrowTrailingSpace = new("md.comp.icon-button-small.narrow-trailing-space");
            public static readonly MaterialTokenKey<double> WideLeadingSpace = new("md.comp.icon-button-small.wide-leading-space");
            public static readonly MaterialTokenKey<double> WideTrailingSpace = new("md.comp.icon-button-small.wide-trailing-space");
            public static readonly MaterialTokenKey<Thickness> OutlinedOutlineWidth = new("md.comp.icon-button-small.outlined-outline-width");
        }

        /// <summary><c>md.comp.icon-button-medium.*</c> — medium icon button metrics (56dp, 24dp icon).</summary>
        public static class IconButtonMedium
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.icon-button-medium.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeRound = new("md.comp.icon-button-medium.container.shape-round");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeSquare = new("md.comp.icon-button-medium.container.shape-square");
            public static readonly MaterialTokenKey<CornerRadius> PressedContainerShape = new("md.comp.icon-button-medium.pressed.container.shape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.icon-button-medium.icon.size");
            public static readonly MaterialTokenKey<double> DefaultLeadingSpace = new("md.comp.icon-button-medium.default-leading-space");
            public static readonly MaterialTokenKey<double> DefaultTrailingSpace = new("md.comp.icon-button-medium.default-trailing-space");
            public static readonly MaterialTokenKey<double> NarrowLeadingSpace = new("md.comp.icon-button-medium.narrow-leading-space");
            public static readonly MaterialTokenKey<double> NarrowTrailingSpace = new("md.comp.icon-button-medium.narrow-trailing-space");
            public static readonly MaterialTokenKey<double> WideLeadingSpace = new("md.comp.icon-button-medium.wide-leading-space");
            public static readonly MaterialTokenKey<double> WideTrailingSpace = new("md.comp.icon-button-medium.wide-trailing-space");
            public static readonly MaterialTokenKey<Thickness> OutlinedOutlineWidth = new("md.comp.icon-button-medium.outlined-outline-width");
        }

        /// <summary><c>md.comp.icon-button-large.*</c> — large icon button metrics (96dp, 32dp icon).</summary>
        public static class IconButtonLarge
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.icon-button-large.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeRound = new("md.comp.icon-button-large.container.shape-round");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeSquare = new("md.comp.icon-button-large.container.shape-square");
            public static readonly MaterialTokenKey<CornerRadius> PressedContainerShape = new("md.comp.icon-button-large.pressed.container.shape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.icon-button-large.icon.size");
            public static readonly MaterialTokenKey<double> DefaultLeadingSpace = new("md.comp.icon-button-large.default-leading-space");
            public static readonly MaterialTokenKey<double> DefaultTrailingSpace = new("md.comp.icon-button-large.default-trailing-space");
            public static readonly MaterialTokenKey<double> NarrowLeadingSpace = new("md.comp.icon-button-large.narrow-leading-space");
            public static readonly MaterialTokenKey<double> NarrowTrailingSpace = new("md.comp.icon-button-large.narrow-trailing-space");
            public static readonly MaterialTokenKey<double> WideLeadingSpace = new("md.comp.icon-button-large.wide-leading-space");
            public static readonly MaterialTokenKey<double> WideTrailingSpace = new("md.comp.icon-button-large.wide-trailing-space");
            public static readonly MaterialTokenKey<Thickness> OutlinedOutlineWidth = new("md.comp.icon-button-large.outlined-outline-width");
        }

        /// <summary><c>md.comp.icon-button-xlarge.*</c> — extra large icon button metrics (136dp, 40dp icon).</summary>
        public static class IconButtonXLarge
        {
            public static readonly MaterialTokenKey<double> ContainerHeight = new("md.comp.icon-button-xlarge.container.height");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeRound = new("md.comp.icon-button-xlarge.container.shape-round");
            public static readonly MaterialTokenKey<CornerRadius> ContainerShapeSquare = new("md.comp.icon-button-xlarge.container.shape-square");
            public static readonly MaterialTokenKey<CornerRadius> PressedContainerShape = new("md.comp.icon-button-xlarge.pressed.container.shape");
            public static readonly MaterialTokenKey<double> IconSize = new("md.comp.icon-button-xlarge.icon.size");
            public static readonly MaterialTokenKey<double> DefaultLeadingSpace = new("md.comp.icon-button-xlarge.default-leading-space");
            public static readonly MaterialTokenKey<double> DefaultTrailingSpace = new("md.comp.icon-button-xlarge.default-trailing-space");
            public static readonly MaterialTokenKey<double> NarrowLeadingSpace = new("md.comp.icon-button-xlarge.narrow-leading-space");
            public static readonly MaterialTokenKey<double> NarrowTrailingSpace = new("md.comp.icon-button-xlarge.narrow-trailing-space");
            public static readonly MaterialTokenKey<double> WideLeadingSpace = new("md.comp.icon-button-xlarge.wide-leading-space");
            public static readonly MaterialTokenKey<double> WideTrailingSpace = new("md.comp.icon-button-xlarge.wide-trailing-space");
            public static readonly MaterialTokenKey<Thickness> OutlinedOutlineWidth = new("md.comp.icon-button-xlarge.outlined-outline-width");
        }
    }
}
