using System.Diagnostics.CodeAnalysis;

namespace ComposeAnimation.Runtime;

/// <summary>
/// A spring animation spec that uses the given spring constants (i.e. damping ratio and
/// stiffness). The optional visibility threshold defines when the animation should be considered
/// visually close enough to round off to its target.
/// </summary>
public class SpringSpec<T> : IFiniteAnimationSpec<T>
{
    private readonly bool _hasVisibilityThreshold;

    public SpringSpec(
        double dampingRatio = Spring.DampingRatioNoBouncy,
        double stiffness = Spring.StiffnessMedium,
        T? visibilityThreshold = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dampingRatio);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stiffness);

        DampingRatio = dampingRatio;
        Stiffness = stiffness;
        VisibilityThreshold = visibilityThreshold;
        _hasVisibilityThreshold = !EqualityComparer<T>.Default.Equals(visibilityThreshold, default);
    }

    public double DampingRatio { get; set; }
    public double Stiffness { get; set; }
    public T? VisibilityThreshold { get; }

    public IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<T> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        double[]? threshold = null;
        if (_hasVisibilityThreshold)
        {
            threshold = new double[converter.Dimension];
            converter.To(VisibilityThreshold, threshold);
        }

        return new VectorizedSpringSpec(DampingRatio, Stiffness, threshold.AsSpan(), converter.Dimension);
    }
}
