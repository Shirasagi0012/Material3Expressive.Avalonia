using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class ScrollBarTokenSource : ITokenSource
{
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        if (ReferenceEquals(key, Comp.ScrollBar.Thickness)) { value = 8d; return true; }
        if (ReferenceEquals(key, Comp.ScrollBar.ThumbMinLength)) { value = 16d; return true; }
        if (ReferenceEquals(key, Comp.ScrollBar.Shape)) { value = new CornerRadius(4); return true; }
        if (ReferenceEquals(key, Comp.ScrollBar.UnhoverColor)) { value = new WithAlpha(Sys.Color.OnSurface, 0.12); return true; }
        if (ReferenceEquals(key, Comp.ScrollBar.HoverColor)) { value = new WithAlpha(Sys.Color.OnSurface, 0.50); return true; }
        if (ReferenceEquals(key, Comp.ScrollBar.HoverDuration)) { value = TimeSpan.FromMilliseconds(300); return true; }
        value = null!;
        return false;
    }
    public event EventHandler? Changed { add { } remove { } }
}
