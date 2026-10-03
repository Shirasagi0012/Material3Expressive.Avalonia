using System.Collections.Frozen;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Defaults from the fixed Compose baseline. References remain dynamically resolved.</summary>
public sealed class SplitButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.SplitButton.Spacing] = 2d,
        [Comp.SplitButtonXSmall.ContainerHeight] = 32d,
        [Comp.SplitButtonXSmall.LeadingPadding] = new Thickness(12, 0, 10, 0),
        [Comp.SplitButtonXSmall.TrailingPadding] = new Thickness(13, 0),
        [Comp.SplitButtonXSmall.TrailingIconSize] = 22d,
        [Comp.SplitButtonXSmall.InnerCornerRadius] = new TokenRef(Sys.Shape.ExtraSmall),
        [Comp.SplitButtonXSmall.PressedInnerCornerRadius] = new TokenRef(Sys.Shape.Small),
        [Comp.SplitButtonSmall.ContainerHeight] = 40d,
        [Comp.SplitButtonSmall.LeadingPadding] = new Thickness(16, 0, 12, 0),
        [Comp.SplitButtonSmall.TrailingPadding] = new Thickness(13, 0),
        [Comp.SplitButtonSmall.TrailingIconSize] = 22d,
        [Comp.SplitButtonSmall.InnerCornerRadius] = new TokenRef(Sys.Shape.ExtraSmall),
        [Comp.SplitButtonSmall.PressedInnerCornerRadius] = new TokenRef(Sys.Shape.Medium),
        [Comp.SplitButtonMedium.ContainerHeight] = 56d,
        [Comp.SplitButtonMedium.LeadingPadding] = new Thickness(24, 0, 24, 0),
        [Comp.SplitButtonMedium.TrailingPadding] = new Thickness(15, 0),
        [Comp.SplitButtonMedium.TrailingIconSize] = 26d,
        [Comp.SplitButtonMedium.InnerCornerRadius] = new TokenRef(Sys.Shape.ExtraSmall),
        [Comp.SplitButtonMedium.PressedInnerCornerRadius] = new TokenRef(Sys.Shape.Medium),
        [Comp.SplitButtonLarge.ContainerHeight] = 96d,
        [Comp.SplitButtonLarge.LeadingPadding] = new Thickness(48, 0, 48, 0),
        [Comp.SplitButtonLarge.TrailingPadding] = new Thickness(29, 0),
        [Comp.SplitButtonLarge.TrailingIconSize] = 38d,
        [Comp.SplitButtonLarge.InnerCornerRadius] = new TokenRef(Sys.Shape.Small),
        [Comp.SplitButtonLarge.PressedInnerCornerRadius] = new TokenRef(Sys.Shape.LargeIncreased),
        [Comp.SplitButtonXLarge.ContainerHeight] = 136d,
        [Comp.SplitButtonXLarge.LeadingPadding] = new Thickness(64, 0, 64, 0),
        [Comp.SplitButtonXLarge.TrailingPadding] = new Thickness(43, 0),
        [Comp.SplitButtonXLarge.TrailingIconSize] = 50d,
        [Comp.SplitButtonXLarge.InnerCornerRadius] = new TokenRef(Sys.Shape.Medium),
        [Comp.SplitButtonXLarge.PressedInnerCornerRadius] = new TokenRef(Sys.Shape.LargeIncreased),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
