namespace ComposeAnimationRuntime;

/// <summary>Interpolates pairs of vector components along an elliptical arc.</summary>
public sealed class ArcSpec<T> : IDurationAnimationSpec<T>
{
    public ArcSpec(IAnimationVectorConverter<T> converter, TimeSpan duration, bool clockwise = true, Func<double, double>? easing = null)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        Converter = converter; Duration = duration; Clockwise = clockwise; Easing = easing ?? Easings.FastOutSlowIn;
    }
    public IAnimationVectorConverter<T> Converter { get; }
    public TimeSpan Duration { get; }
    public bool Clockwise { get; }
    public Func<double, double> Easing { get; }
    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        AnimationValidation.Vectors(initialValue, initialVelocity, targetValue);
        if (initialValue.Dimension % 2 != 0) throw new ArgumentException("Arc animation requires an even number of components.", nameof(initialValue));
        var start = initialValue.Copy(); var target = targetValue.Copy();
        return new ArcAnimation(Duration.TotalSeconds, start, target, Clockwise, Easing);
    }
    private sealed class ArcAnimation(double duration, AnimationVector start, AnimationVector target, bool clockwise, Func<double, double> easing) : VectorAnimation
    {
        public override double DurationSeconds => duration;
        public override AnimationState Sample(TimeSpan playTime)
        {
            ValidateTime(playTime);
            var fraction = Math.Clamp(playTime.TotalSeconds / duration, 0, 1); var t = Math.Clamp(easing(fraction), 0, 1); var value = start.Copy();
            for (var index = 0; index < value.Dimension; index += 2)
            {
                var centerX = (start[index] + target[index]) / 2; var centerY = (start[index + 1] + target[index + 1]) / 2;
                var radiusX = Math.Abs(target[index] - start[index]) / 2; var radiusY = Math.Abs(target[index + 1] - start[index + 1]) / 2;
                var angle = (clockwise ? 1 : -1) * Math.PI * t;
                value[index] = (float)(centerX + radiusX * Math.Cos(Math.PI - angle));
                value[index + 1] = (float)(centerY + radiusY * Math.Sin(Math.PI - angle));
            }
            if (fraction >= 1) value = target.Copy();
            var velocity = value.Copy(); velocity.Reset();
            return new AnimationState(value, velocity, playTime.TotalSeconds >= duration);
        }
    }
}
