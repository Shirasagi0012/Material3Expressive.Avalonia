namespace ComposeAnimation.Runtime;

internal sealed class StartDelayAnimationSpec<T>(IFiniteAnimationSpec<T> animationSpec, TimeSpan startDelay)
    : IFiniteAnimationSpec<T>
{
    public IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<T> converter)
        => new StartDelayVectorizedAnimationSpec(animationSpec.Vectorize(converter), startDelay);
}

internal sealed class StartDelayVectorizedAnimationSpec(
    IVectorizedFiniteAnimationSpec animationSpec, TimeSpan startDelay) : IVectorizedFiniteAnimationSpec
{
    public TimeSpan GetDuration(ReadOnlySpan<double> initial, ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity)
        => animationSpec.GetDuration(initial, target, initialVelocity) + startDelay;

    public void GetValue(TimeSpan playTime, ReadOnlySpan<double> initial, ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity, Span<double> destination)
    {
        if (playTime < startDelay) initial.CopyTo(destination);
        else animationSpec.GetValue(playTime - startDelay, initial, target, initialVelocity, destination);
    }

    public void GetVelocity(TimeSpan playTime, ReadOnlySpan<double> initial, ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity, Span<double> destination)
    {
        if (playTime < startDelay) initialVelocity.CopyTo(destination);
        else animationSpec.GetVelocity(playTime - startDelay, initial, target, initialVelocity, destination);
    }

    public void GetEndVelocity(ReadOnlySpan<double> initial, ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity, Span<double> destination)
        => animationSpec.GetEndVelocity(initial, target, initialVelocity, destination);
}
