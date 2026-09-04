namespace ComposeAnimationRuntime;

/// <summary>Adapts a stateless trajectory function into an animation specification.</summary>
public sealed class CustomSpec<T> : IDurationAnimationSpec<T>
{
    private readonly Func<TimeSpan, AnimationVector, AnimationVector> _sample;
    private readonly Func<TimeSpan, AnimationVector, AnimationVector>? _sampleVelocity;

    public CustomSpec(IAnimationVectorConverter<T> converter, TimeSpan duration,
        Func<TimeSpan, AnimationVector, AnimationVector> sample,
        Func<TimeSpan, AnimationVector, AnimationVector>? sampleVelocity = null)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(sample);
        Converter = converter;
        Duration = duration;
        _sample = sample;
        _sampleVelocity = sampleVelocity;
    }

    public IAnimationVectorConverter<T> Converter { get; }
    public TimeSpan Duration { get; }

    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        AnimationValidation.Vectors(initialValue, initialVelocity, targetValue);
        var start = initialValue.Copy();
        return new CustomAnimation(Duration.TotalSeconds, playTime => _sample(playTime, start),
            _sampleVelocity is null ? null : playTime => _sampleVelocity(playTime, start));
    }

    private sealed class CustomAnimation(double durationSeconds,
        Func<TimeSpan, AnimationVector> sample,
        Func<TimeSpan, AnimationVector>? sampleVelocity) : VectorAnimation
    {
        public override double DurationSeconds => durationSeconds;
        public override AnimationState Sample(TimeSpan playTime)
        {
            ValidateTime(playTime);
            var clamped = durationSeconds == 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(Math.Min(playTime.TotalSeconds, durationSeconds));
            var value = sample(clamped);
            var velocity = sampleVelocity?.Invoke(clamped) ?? Zero(value);
            return new AnimationState(value, velocity, playTime.TotalSeconds >= durationSeconds);
        }

        private static AnimationVector Zero(AnimationVector value) { var result = value.Copy(); result.Reset(); return result; }
    }
}
