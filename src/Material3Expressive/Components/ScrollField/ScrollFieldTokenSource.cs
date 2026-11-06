using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class ScrollFieldTokenSource : ITokenSource
{
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        value = (double)(200f / 3f);
        return ReferenceEquals(key, Comp.ScrollField.ItemHeight);
    }
    public event EventHandler? Changed { add { } remove { } }
}
