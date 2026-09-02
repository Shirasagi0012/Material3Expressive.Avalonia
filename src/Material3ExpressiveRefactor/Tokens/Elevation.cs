using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation;

namespace Material3ExpressiveRefactor.Tokens;

public static class Elevation
{
    public static readonly MaterialTokenKey<float> Level1 = new();
    public static readonly MaterialTokenKey<float> Level2 = new();
    public static readonly MaterialTokenKey<float> Level3 = new();
    public static readonly MaterialTokenKey<float> Level4 = new();
    public static readonly MaterialTokenKey<float> Level5 = new();
}

public class ElevationTokenSource : ITokenSource
{
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        switch (key)
        {
            case MaterialTokenKey<float> when ReferenceEquals(key, Elevation.Level1): value = 1f; return true;
            case MaterialTokenKey<float> when ReferenceEquals(key, Elevation.Level2): value = 3f; return true;
            case MaterialTokenKey<float> when ReferenceEquals(key, Elevation.Level3): value = 6f; return true;
            case MaterialTokenKey<float> when ReferenceEquals(key, Elevation.Level4): value = 8f; return true;
            case MaterialTokenKey<float> when ReferenceEquals(key, Elevation.Level5): value = 12f; return true;
            default: value = null!; return false;
        }
    }

    public event EventHandler? Changed;
}
