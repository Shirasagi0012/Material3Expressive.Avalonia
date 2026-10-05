using System.Collections.Frozen;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class CheckBoxTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.CheckBox.ContentPadding] = new Avalonia.Thickness(8, 0, 0, 0),
        [Comp.CheckBox.ShapeAnimation] = new TokenRef(Sys.Motion.Spring.SpatialDefault),
        [Comp.CheckBox.ColorAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault),
        [Comp.CheckBox.UncheckColorAnimation] = new TokenRef(Sys.Motion.Spring.EffectsFast),
        [Comp.CheckBox.CheckedCheckmarkColor] = new TokenRef(Sys.Color.OnPrimary),
        [Comp.CheckBox.UncheckedCheckmarkColor] = Color.FromArgb(0, 0, 0, 0),
        [Comp.CheckBox.CheckedBoxColor] = new TokenRef(Sys.Color.Primary),
        [Comp.CheckBox.UncheckedBoxColor] = Color.FromArgb(0, 0, 0, 0),
        [Comp.CheckBox.DisabledCheckedBoxColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.CheckBox.DisabledUncheckedBoxColor] = Color.FromArgb(0, 0, 0, 0),
        [Comp.CheckBox.DisabledIndeterminateBoxColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.CheckBox.CheckedBorderColor] = new TokenRef(Sys.Color.Primary),
        [Comp.CheckBox.UncheckedBorderColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.CheckBox.DisabledBorderColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.CheckBox.DisabledUncheckedBorderColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.CheckBox.DisabledIndeterminateBorderColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.CheckBox.DisabledCheckmarkColor] = new TokenRef(Sys.Color.Surface),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
