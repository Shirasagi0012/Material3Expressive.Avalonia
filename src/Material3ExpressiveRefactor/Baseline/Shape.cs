using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Baseline;

/// <summary>
/// The Expressive corner scale (design doc §4.2/§4.3): answers <c>md.sys.shape.*</c> as composed
/// <see cref="CornerRadius"/> values and <c>md.ref.shape.*</c> as the raw corner scalars they are
/// composed from. Composition is production-time data — the sys tokens are literals, so
/// overriding a ref scalar does not propagate into them (same read-only contract as
/// <c>ref.palette.*</c>, §5.6). Theme-independent.
/// </summary>
public class ShapeTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Ref.Shape.CornerNone] = 0d,
        [Ref.Shape.CornerExtraSmall] = 4d,
        [Ref.Shape.CornerSmall] = 8d,
        [Ref.Shape.CornerMedium] = 12d,
        [Ref.Shape.CornerLarge] = 16d,
        [Ref.Shape.CornerLargeIncreased] = 20d,
        [Ref.Shape.CornerExtraLarge] = 28d,
        [Ref.Shape.CornerExtraLargeIncreased] = 32d,
        [Ref.Shape.CornerExtraExtraLarge] = 48d,

        [Sys.Shape.None] = new CornerRadius(0),
        [Sys.Shape.ExtraSmall] = new CornerRadius(4),
        [Sys.Shape.ExtraSmallTop] = new CornerRadius(4, 4, 0, 0),
        [Sys.Shape.Small] = new CornerRadius(8),
        [Sys.Shape.Medium] = new CornerRadius(12),
        [Sys.Shape.Large] = new CornerRadius(16),
        [Sys.Shape.LargeTop] = new CornerRadius(16, 16, 0, 0),
        [Sys.Shape.LargeEnd] = new CornerRadius(0, 16, 16, 0),
        [Sys.Shape.LargeIncreased] = new CornerRadius(20),
        [Sys.Shape.ExtraLarge] = new CornerRadius(28),
        [Sys.Shape.ExtraLargeTop] = new CornerRadius(28, 28, 0, 0),
        [Sys.Shape.ExtraLargeIncreased] = new CornerRadius(32),
        [Sys.Shape.ExtraExtraLarge] = new CornerRadius(48),
        // Stadium: deliberately far larger than any real control extent; rendering clamps the
        // radii to a half-pill. CornerRadius is absolute DIPs — there is no percent form.
        // TODO: Find if there is better way to express a 'full' corner radius
        [Sys.Shape.Full] = new CornerRadius(1000),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}
