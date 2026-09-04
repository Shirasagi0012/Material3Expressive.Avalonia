namespace ComposeAnimationRuntime;

public sealed class TweenSpec<T> : IDurationAnimationSpec<T>
{
    public TweenSpec(IAnimationVectorConverter<T> converter, TimeSpan duration, Func<double, double>? easing = null, TimeSpan delay = default)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        Converter = converter; ActiveDuration = duration; Delay = delay; Duration = duration + delay; Easing = easing ?? Easings.Linear;
    }
    public IAnimationVectorConverter<T> Converter { get; }
    public TimeSpan ActiveDuration { get; }
    public TimeSpan Delay { get; }
    public TimeSpan Duration { get; }
    public Func<double, double> Easing { get; }
    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        AnimationValidation.Vectors(initialValue, initialVelocity, targetValue);
        var start = initialValue.Copy(); var target = targetValue.Copy();
        return new SampledAnimation(Duration.TotalSeconds, seconds =>
        {
            if (seconds < Delay.TotalSeconds) return start.Copy();
            var fraction = ActiveDuration == TimeSpan.Zero ? 1 : Math.Clamp((seconds - Delay.TotalSeconds) / ActiveDuration.TotalSeconds, 0, 1);
            return AnimationValidation.Lerp(start, target, fraction >= 1 ? 1 : Math.Clamp(Easing(fraction), 0, 1));
        });
    }
}

public sealed class SnapSpec<T> : IDurationAnimationSpec<T>
{
    public SnapSpec(IAnimationVectorConverter<T> converter, TimeSpan delay = default)
    {
        ArgumentNullException.ThrowIfNull(converter); ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero); Converter = converter; Delay = delay;
    }
    public IAnimationVectorConverter<T> Converter { get; }
    public TimeSpan Delay { get; }
    public TimeSpan Duration => Delay;
    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        AnimationValidation.Vectors(initialValue, initialVelocity, targetValue);
        var start = initialValue.Copy(); var target = targetValue.Copy();
        return new SampledAnimation(Duration.TotalSeconds, seconds => (seconds < Duration.TotalSeconds ? start : target).Copy(), _ => { var zero = start.Copy(); zero.Reset(); return zero; });
    }
}
