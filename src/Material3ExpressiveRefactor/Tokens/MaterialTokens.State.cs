using Material3ExpressiveRefactor.Foundation.Tokens;

namespace Material3ExpressiveRefactor.Tokens;

public static partial class MaterialTokens
{
    public static partial class Sys
    {
        /// <summary>
        /// <c>md.sys.state.*</c> — shared state-layer opacities (§7.4). These are system-level
        /// tokens, deliberately NOT per-component slots: interactive templates set
        /// <c>PART_StateLayer.Opacity</c> from these in their pseudo-class styles. Values are the
        /// Expressive-era spec (pressed/focus are 0.10, not the legacy 0.12).
        /// </summary>
        public static class State
        {
            public static readonly MaterialTokenKey<double> HoverStateLayerOpacity = new("md.sys.state.hover-state-layer-opacity");
            public static readonly MaterialTokenKey<double> FocusStateLayerOpacity = new("md.sys.state.focus-state-layer-opacity");
            public static readonly MaterialTokenKey<double> PressedStateLayerOpacity = new("md.sys.state.pressed-state-layer-opacity");
            public static readonly MaterialTokenKey<double> DraggedStateLayerOpacity = new("md.sys.state.dragged-state-layer-opacity");

            /// <summary>Opacity applied to the container color of a disabled component (over on-surface).</summary>
            public static readonly MaterialTokenKey<double> DisabledContainerOpacity = new("md.sys.state.disabled-container-opacity");

            /// <summary>Opacity applied to the content color of a disabled component (over on-surface).</summary>
            public static readonly MaterialTokenKey<double> DisabledContentOpacity = new("md.sys.state.disabled-content-opacity");
        }
    }
}
