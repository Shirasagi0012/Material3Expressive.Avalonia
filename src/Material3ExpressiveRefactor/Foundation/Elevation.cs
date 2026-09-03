using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Tokens;

namespace Material3ExpressiveRefactor.Foundation;

public class ElevationTokenSource : ITokenSource
{
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        if (MaterialTokens.Sys.Elevation.Defaults.TryGetValue(key, out var f))
        {
            value = f;
            return true;
        }

        value = null!;
        return false;
    }

    public event EventHandler? Changed;
}
