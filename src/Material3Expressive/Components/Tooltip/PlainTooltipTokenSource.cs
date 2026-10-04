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
        [Comp.PlainTooltip.ContainerColor] = new TokenRef(Sys.Color.InverseSurface),
        [Comp.PlainTooltip.ContainerShape] = new TokenRef(Sys.Shape.ExtraSmall),
        [Comp.PlainTooltip.SupportingTextColor] = new TokenRef(Sys.Color.InverseOnSurface),
        [Comp.PlainTooltip.SupportingTextFont] = new TokenRef(Sys.TypeScale.BodySmall.Style),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
