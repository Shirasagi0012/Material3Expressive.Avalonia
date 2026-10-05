using System.Collections.Frozen;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>SegmentedButton.kt and OutlinedSegmentedButtonTokens v0_162.</summary>
public sealed class SegmentedButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.SegmentedButton.ContentPadding] = new Thickness(12, 8),
        [Comp.SegmentedButton.IconSpacing] = 8d,
        [Comp.SegmentedButton.IconEffectsAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault),
        [Comp.SegmentedButton.IconSpatialAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.SegmentedButton.LabelOffsetAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.SegmentedButton.InactiveContainerColor] = Colors.Transparent,
        [Comp.SegmentedButton.DisabledActiveContainerColor] = new TokenRef(Comp.SegmentedButton.ActiveContainerColor),
        [Comp.SegmentedButton.DisabledInactiveContainerColor] = new TokenRef(Comp.SegmentedButton.InactiveContainerColor),
        [Comp.SegmentedButton.DisabledActiveContentColor] = new TokenRef(Comp.SegmentedButton.DisabledContentColor),
        [Comp.SegmentedButton.DisabledInactiveContentColor] = new TokenRef(Comp.SegmentedButton.DisabledContentColor),
        [Comp.SegmentedButton.ActiveBorderColor] = new TokenRef(Comp.SegmentedButton.OutlineColor),
        [Comp.SegmentedButton.InactiveBorderColor] = new TokenRef(Comp.SegmentedButton.OutlineColor),
        [Comp.SegmentedButton.DisabledActiveBorderColor] = new TokenRef(Comp.SegmentedButton.DisabledOutlineColor),
        [Comp.SegmentedButton.DisabledInactiveBorderColor] = new TokenRef(Comp.SegmentedButton.DisabledOutlineColor),
        [Comp.SegmentedButton.ContainerHeight] = 40d,
        [Comp.SegmentedButton.Shape] = new TokenRef(Sys.Shape.Full),
        [Comp.SegmentedButton.OutlineWidth] = 1d,
        [Comp.SegmentedButton.IconSize] = 18d,
        [Comp.SegmentedButton.LabelTextStyle] = new TokenRef(Sys.TypeScale.LabelLarge.Style),
        [Comp.SegmentedButton.ActiveContainerColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.SegmentedButton.ActiveContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.SegmentedButton.InactiveContentColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.SegmentedButton.OutlineColor] = new TokenRef(Sys.Color.Outline),
        [Comp.SegmentedButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        // The factory uses OutlineColor, not DisabledOutlineColor's OnSurface token.
        [Comp.SegmentedButton.DisabledOutlineColor] = new WithAlpha(Sys.Color.Outline, 0.12),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
