using System.Collections.Frozen;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Baseline;

/// <summary>
/// The six elevation levels in DIPs (design doc §7.6, revised: the level→shadow derivation moved
/// into the renderer). <c>MaterialSurface</c> turns a level into a Skia point-light shadow at draw
/// time, with the shadow color resolved from <c>md.sys.color.shadow</c>, and into the tonal tint
/// (<c>md.sys.color.surface-tint</c> overlaid on <c>md.sys.color.surface</c>). Theme-independent.
/// </summary>
public class ElevationTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Sys.Elevation.Level0] = 0d,
        [Sys.Elevation.Level1] = 1d,
        [Sys.Elevation.Level2] = 3d,
        [Sys.Elevation.Level3] = 6d,
        [Sys.Elevation.Level4] = 8d,
        [Sys.Elevation.Level5] = 12d,
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}
