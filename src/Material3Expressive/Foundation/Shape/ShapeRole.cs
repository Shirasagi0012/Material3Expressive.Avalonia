namespace Material3Expressive.Foundation;

/// <summary>
/// The shape slots a component token may reference. Mirrors androidx <c>ShapeKeyTokens</c>
/// (stamp <c>14_1_0</c>).
/// </summary>
/// <remarks>
/// Directional roles are projected from their canonical base Raw identities by squaring off the
/// other corners. Every base identity, including None and Full, can be explicitly overridden.
/// </remarks>
public enum ShapeRole
{
    CornerNone,
    CornerExtraSmall,
    CornerExtraSmallTop,
    CornerSmall,
    CornerMedium,
    CornerLarge,
    CornerLargeStart,
    CornerLargeEnd,
    CornerLargeTop,
    CornerLargeIncreased,
    CornerExtraLarge,
    CornerExtraLargeIncreased,
    CornerExtraLargeTop,
    CornerExtraExtraLarge,
    CornerFull
}
