using System.Collections.Frozen;
using Avalonia.Media;
using Avalonia.Styling;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Foundation.Tokens.Sources;

/// <summary>
/// The six elevation levels and their shadows (design doc §7.6). Levels are plain DIPs; shadows
/// are baked <see cref="BoxShadows"/> literals following the M3 web recipe — one key shadow
/// (black at 30%) plus one ambient shadow (black at 15%) per level. The shadow color is baked
/// rather than derived from <c>Sys.Color.Shadow</c>: multi-input derivation is explicitly out of
/// scope (§5.3), and tonal elevation tint is deferred (§12). Theme-independent.
/// </summary>
public class ElevationTokenSource : ITokenSource
{
    private static readonly Color KeyShadowColor = Color.FromArgb(0x4D, 0, 0, 0);
    private static readonly Color AmbientShadowColor = Color.FromArgb(0x26, 0, 0, 0);

    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Sys.Elevation.Level0] = 0d,
        [Sys.Elevation.Level1] = 1d,
        [Sys.Elevation.Level2] = 3d,
        [Sys.Elevation.Level3] = 6d,
        [Sys.Elevation.Level4] = 8d,
        [Sys.Elevation.Level5] = 12d,

        [Sys.Elevation.Level0Shadow] = default(BoxShadows),
        [Sys.Elevation.Level1Shadow] = Shadows(Key(1, 2, 0), Ambient(1, 3, 1)),
        [Sys.Elevation.Level2Shadow] = Shadows(Key(1, 2, 0), Ambient(2, 6, 2)),
        [Sys.Elevation.Level3Shadow] = Shadows(Key(1, 3, 0), Ambient(4, 8, 3)),
        [Sys.Elevation.Level4Shadow] = Shadows(Key(2, 3, 0), Ambient(6, 10, 4)),
        [Sys.Elevation.Level5Shadow] = Shadows(Key(4, 4, 0), Ambient(8, 12, 6)),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }

    private static BoxShadow Key(double offsetY, double blur, double spread) =>
        new() { OffsetY = offsetY, Blur = blur, Spread = spread, Color = KeyShadowColor };

    private static BoxShadow Ambient(double offsetY, double blur, double spread) =>
        new() { OffsetY = offsetY, Blur = blur, Spread = spread, Color = AmbientShadowColor };

    private static BoxShadows Shadows(BoxShadow key, BoxShadow ambient) =>
        new(key, new[] { ambient });
}
