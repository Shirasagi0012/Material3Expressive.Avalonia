using Material3ExpressiveRefactor.Foundation.Motion;

namespace Material3ExpressiveRefactor.Foundation.Tokens;

/// <summary>
/// Constrained derivation: a <see cref="SpringSpec" /> composed from two scalar tokens — the
/// fourth closed value form a token entry may hold (design doc §5.3, alongside literal,
/// <see cref="TokenRef" /> and <see cref="WithAlpha" />). Evaluated at the consumption side:
/// the component references resolve from the requester's position, so overriding the damping
/// (or stiffness) scalar alone still flows into every spec composed from it.
/// </summary>
/// <remarks>
/// A fully literal spec needs no derivation — store the <see cref="SpringSpec" /> itself as the
/// entry value. The visibility threshold is deliberately not a token: it is expressed in the
/// animated value's own units and belongs to the call site, matching upstream where
/// MotionScheme specs carry only damping and stiffness.
/// </remarks>
public sealed class SpringSpecRef
{
    public SpringSpecRef(MaterialTokenKey damping, MaterialTokenKey stiffness)
    {
        Damping = damping ?? throw new ArgumentNullException(nameof(damping));
        Stiffness = stiffness ?? throw new ArgumentNullException(nameof(stiffness));
    }

    public SpringSpecRef(MaterialTokenKey damping, MaterialTokenKey stiffness, double visibilityThreshold)
        : this(damping, stiffness)
    {
        VisibilityThreshold = visibilityThreshold;
    }

    /// <summary>Key resolving to the damping ratio (double).</summary>
    public MaterialTokenKey Damping { get; }

    /// <summary>Key resolving to the stiffness (double).</summary>
    public MaterialTokenKey Stiffness { get; }

    /// <summary>Literal visibility threshold, when the composed spec should not use the default.</summary>
    public double? VisibilityThreshold { get; }

    public override string ToString() => $"SpringSpecRef({Damping}, {Stiffness})";
}
