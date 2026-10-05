using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Scaffold's default arguments. Runtime insets and measured padding remain on the owner.</summary>
public sealed class ScaffoldTokenSource : ITokenSource
{
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        if (ReferenceEquals(key, Comp.Scaffold.FloatingActionButtonPosition)) { value = FabPosition.End; return true; }
        if (ReferenceEquals(key, Comp.Scaffold.ContentWindowInsets)) { value = default(Thickness); return true; }
        value = null!;
        return false;
    }
    public event EventHandler? Changed { add { } remove { } }
}
