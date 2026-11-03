using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Fixed RichTooltipTokens and TooltipDefaults values, retaining system references.</summary>
public sealed class RichTooltipTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.RichTooltip.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.RichTooltip.ContainerShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.RichTooltip.ContainerElevation] = new TokenRef(Sys.Elevation.Level2),
        [Comp.RichTooltip.SubheadColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.RichTooltip.SubheadFont] = new TokenRef(Sys.TypeScale.TitleSmall.Style),
        [Comp.RichTooltip.SupportingTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.RichTooltip.SupportingTextFont] = new TokenRef(Sys.TypeScale.BodyMedium.Style),
        [Comp.RichTooltip.ActionLabelTextColor] = new TokenRef(Sys.Color.Primary),
        [Comp.RichTooltip.ActionLabelTextFont] = new TokenRef(Sys.TypeScale.LabelLarge.Style),
        [Comp.RichTooltip.ContentPadding] = new Thickness(16, 0),
        [Comp.RichTooltip.MaxWidth] = 320d,
        [Comp.RichTooltip.SubheadFirstBaselineOffset] = 28d,
        [Comp.RichTooltip.TextFirstBaselineOffset] = 24d,
        [Comp.RichTooltip.TextBottomPadding] = 16d,
        [Comp.RichTooltip.TextVerticalPadding] = 4d,
        [Comp.RichTooltip.ActionMinHeight] = 36d,
        [Comp.RichTooltip.ActionBottomPadding] = 8d,
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
