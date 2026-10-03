using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class RadioButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.RadioButton.SelectedColor] = new TokenRef(Sys.Color.Primary),
        [Comp.RadioButton.UnselectedColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.RadioButton.DisabledSelectedColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.RadioButton.DisabledUnselectedColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}
