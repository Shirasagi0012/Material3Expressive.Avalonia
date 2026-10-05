using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Values from the fixed Compose PlainTooltipTokens baseline.</summary>
public sealed class PlainTooltipTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.TooltipBox.Spacing] = 4d,
        [Comp.PlainTooltip.ContentPadding] = new Avalonia.Thickness(8, 4),
        [Comp.PlainTooltip.MaxWidth] = 200d,
        [Comp.PlainTooltip.MinHeight] = 24d,
        [Comp.PlainTooltip.MinWidth] = 40d,
        [Comp.TooltipBox.ScaleAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.TooltipBox.OpacityAnimation] = new TokenRef(Sys.Motion.Spring.EffectsFast),
        [Comp.PlainTooltip.ContainerColor] = new TokenRef(Sys.Color.InverseSurface),
        [Comp.PlainTooltip.ContainerShape] = new TokenRef(Sys.Shape.ExtraSmall),
        [Comp.PlainTooltip.SupportingTextColor] = new TokenRef(Sys.Color.InverseOnSurface),
        [Comp.PlainTooltip.SupportingTextFont] = new TokenRef(Sys.TypeScale.BodySmall.Style),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
