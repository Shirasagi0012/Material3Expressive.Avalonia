namespace ComposeAnimation.Runtime;

/// <summary>
/// Uses spring animations to animate each dimension of a vector. The same spring constants are
/// used on all dimensions; the optional visibility threshold defines when the animation should be
/// considered visually close enough to its target to stop.
/// </summary>
public sealed class VectorizedSpringSpec : IVectorizedFiniteAnimationSpec
{
    private readonly IFloatAnimationSpec[] _animations;

    public double DampingRatio { get; }
    public double Stiffness { get; }

    public VectorizedSpringSpec(
        double dampingRatio,
        double stiffness,
        ReadOnlySpan<double> visibilityThreshold,
        int dimension)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dampingRatio);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stiffness);
        ArgumentOutOfRangeException.ThrowIfLessThan(dimension, 1);
        if (!visibilityThreshold.IsEmpty && visibilityThreshold.Length != dimension)
            throw new ArgumentException("Visibility threshold dimension must match the converter dimension.", nameof(visibilityThreshold));

        DampingRatio = dampingRatio;
        Stiffness = stiffness;

        _animations = new IFloatAnimationSpec[dimension];
        for (var i = 0; i < dimension; i++)
        {
            var threshold = visibilityThreshold.IsEmpty
                ? Spring.DefaultDisplacementThreshold
                : visibilityThreshold[i];
            _animations[i] = new FloatSpringSpec(dampingRatio, stiffness, threshold);
        }
    }

    public void GetValue(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    )
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        for (var i = 0; i < destination.Length; i++)
            destination[i] = _animations[i].GetValue(playTime, initial[i], target[i], initialVelocity[i]);
    }

    public void GetVelocity(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    )
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        for (var i = 0; i < destination.Length; i++)
            destination[i] = _animations[i].GetVelocity(playTime, initial[i], target[i], initialVelocity[i]);
    }

    /// <summary>Spring animations snap the transient trailing velocity to zero.</summary>
    public void GetEndVelocity(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    )
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        destination.Clear();
    }

    public TimeSpan GetDuration(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity)
    {
        VectorValidation.Validate(initial, target, initialVelocity);
        var maxDuration = TimeSpan.Zero;
        for (var i = 0; i < initial.Length; i++)
        {
            var duration = _animations[i].GetDuration(initial[i], target[i], initialVelocity[i]);
            if (duration > maxDuration)
                maxDuration = duration;
        }
        return maxDuration;
    }
}
