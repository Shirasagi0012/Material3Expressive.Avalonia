using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>SnackbarTokens v0_103 at the fixed Compose baseline.</summary>
public sealed class SnackbarTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.Snackbar.ScaleAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.Snackbar.OpacityAnimation] = new TokenRef(Sys.Motion.Spring.EffectsFast),
        [Comp.Snackbar.ActionFocusLabelTextColor] = new TokenRef(Sys.Color.InversePrimary),
        [Comp.Snackbar.ActionHoverLabelTextColor] = new TokenRef(Sys.Color.InversePrimary),
        [Comp.Snackbar.ActionLabelTextColor] = new TokenRef(Sys.Color.InversePrimary),
        [Comp.Snackbar.ActionPressedLabelTextColor] = new TokenRef(Sys.Color.InversePrimary),
        [Comp.Snackbar.ActionLabelTextFont] = new TokenRef(Sys.TypeScale.LabelLarge.Style),
        [Comp.Snackbar.ContainerColor] = new TokenRef(Sys.Color.InverseSurface),
        [Comp.Snackbar.ContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.Snackbar.ContainerShape] = new TokenRef(Sys.Shape.ExtraSmall),
        [Comp.Snackbar.IconColor] = new TokenRef(Sys.Color.InverseOnSurface),
        [Comp.Snackbar.FocusIconColor] = new TokenRef(Sys.Color.InverseOnSurface),
        [Comp.Snackbar.HoverIconColor] = new TokenRef(Sys.Color.InverseOnSurface),
        [Comp.Snackbar.PressedIconColor] = new TokenRef(Sys.Color.InverseOnSurface),
        [Comp.Snackbar.IconSize] = 24d,
        [Comp.Snackbar.SupportingTextColor] = new TokenRef(Sys.Color.InverseOnSurface),
        [Comp.Snackbar.SupportingTextFont] = new TokenRef(Sys.TypeScale.BodyMedium.Style),
        [Comp.Snackbar.SingleLineContainerHeight] = 48d,
        [Comp.Snackbar.TwoLinesContainerHeight] = 68d,
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
