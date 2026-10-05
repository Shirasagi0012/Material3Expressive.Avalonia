namespace ComposeAnimation.Runtime;

/// <summary>
/// A stateless, vector-based decay animation specification. Decay animations have no end value;
/// the target value is a result of the animation rather than an input.
/// </summary>
public interface IVectorizedDecayAnimationSpec
{
    /// <summary>
    /// The absolute value of the velocity threshold, below which the animation is considered
    /// finished.
    /// </summary>
    double AbsVelocityThreshold { get; }

    TimeSpan GetDuration(ReadOnlySpan<double> initialValue, ReadOnlySpan<double> initialVelocity);

    void GetValue(
        TimeSpan playTime,
        ReadOnlySpan<double> initialValue,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination);

    void GetVelocity(
        TimeSpan playTime,
        ReadOnlySpan<double> initialValue,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination);

    /// <summary>Returns the target value where the animation will come to a natural stop.</summary>
    void GetTargetValue(
        ReadOnlySpan<double> initialValue,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination);
}
