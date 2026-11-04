using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class ScrimTokenSource : ITokenSource
{
    private static readonly WithAlpha Color = new(Sys.Color.Scrim, .32);
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    { value = Color; return ReferenceEquals(key, Comp.Scrim.ContainerColor); }
    public event EventHandler? Changed { add { } remove { } }
}
