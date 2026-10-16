using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Defaults from ButtonGroup.kt and its two Small token files at the fixed baseline.</summary>
public sealed class ButtonGroupTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.ButtonGroup.ExpandedRatio] = 0.15d,
        [Comp.ButtonGroup.Spacing] = 12d,
        [Comp.ButtonGroup.WidthAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.ButtonGroup.ContentPadding] = new Thickness(24, 8),
        [Comp.ButtonGroup.IconContentPadding] = new Thickness(16, 8, 24, 8),
        [Comp.ButtonGroup.CompressionLimit] = 24d,
        [Comp.ButtonGroup.HorizontalArrangement] = ButtonGroupArrangement.SpacedBy,
        [Comp.ConnectedButtonGroup.Spacing] = 2d,
        [Comp.ConnectedButtonGroup.InnerCornerRadius] = new TokenRef(Sys.Shape.Small),
        [Comp.ConnectedButtonGroup.PressedInnerCornerRadius] = new TokenRef(Sys.Shape.ExtraSmall),
        [Comp.ConnectedButtonGroup.OuterCornerRadius] = new TokenRef(Sys.Shape.Full),
        [Comp.ConnectedButtonGroup.CheckedCornerRadius] = new TokenRef(Sys.Shape.Full),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
