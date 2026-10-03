using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class DividerTokenSource : ITokenSource
{
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        if (ReferenceEquals(key, Comp.Divider.Color)) { value = new TokenRef(Sys.Color.OutlineVariant); return true; }
        if (ReferenceEquals(key, Comp.Divider.Thickness)) { value = 1d; return true; }
        value = null!;
        return false;
    }
    public event EventHandler? Changed { add { } remove { } }
}
