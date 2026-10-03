using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

// Fixed DialogTokens v0_210. AlertDialog uses tonal elevation 0 and no shadow;
// ContainerElevation is retained as a token, not substituted into Surface.
public sealed class DialogTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.Dialog.ActionLabelTextColor] = new TokenRef(Sys.Color.Primary),
        [Comp.Dialog.ActionFocusLabelTextColor] = new TokenRef(Sys.Color.Primary),
        [Comp.Dialog.ActionHoverLabelTextColor] = new TokenRef(Sys.Color.Primary),
        [Comp.Dialog.ActionPressedLabelTextColor] = new TokenRef(Sys.Color.Primary),
        [Comp.Dialog.ActionLabelTextFont] = new TokenRef(Sys.TypeScale.LabelLarge.Style),
        [Comp.Dialog.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerHigh),
        [Comp.Dialog.ContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.Dialog.ContainerShape] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.Dialog.HeadlineColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.Dialog.HeadlineFont] = new TokenRef(Sys.TypeScale.HeadlineSmall.Style),
        [Comp.Dialog.SupportingTextColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.Dialog.SupportingTextFont] = new TokenRef(Sys.TypeScale.BodyMedium.Style),
        [Comp.Dialog.IconColor] = new TokenRef(Sys.Color.Secondary),
        [Comp.Dialog.IconSize] = 24d,
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
