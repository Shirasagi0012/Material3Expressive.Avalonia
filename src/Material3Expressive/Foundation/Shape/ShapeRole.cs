namespace Material3Expressive.Foundation;

/// <summary>
/// The shape slots a component token may reference. Mirrors androidx <c>ShapeKeyTokens</c>
/// (stamp <c>14_1_0</c>).
/// </summary>
/// <remarks>
/// Only eight of these are configurable on a <see cref="ShapeScheme" />. The directional roles are
/// derived from their base role by squaring off the other corners, and
/// <see cref="ShapeRole.CornerNone" /> and <see cref="ShapeRole.CornerFull" /> are fixed — a theme
/// cannot redefine "no rounding" or "fully rounded", which matches upstream.
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
