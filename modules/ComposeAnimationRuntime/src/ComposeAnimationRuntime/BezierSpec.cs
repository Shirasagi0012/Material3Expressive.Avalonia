namespace ComposeAnimationRuntime;

public sealed class CubicBezierSpec<T> : IDurationAnimationSpec<T>
{
    private readonly AnimationVector _control1;
    private readonly AnimationVector _control2;

    public CubicBezierSpec(IAnimationVectorConverter<T> converter, TimeSpan duration,
        T control1, T control2, Func<double, double>? easing = null)
    {
        Converter = converter ?? throw new ArgumentNullException(nameof(converter));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        _control1 = converter.ConvertToVector(control1);
        _control2 = converter.ConvertToVector(control2);
        Duration = duration;
        Easing = easing ?? Easings.Linear;
    }

    public IAnimationVectorConverter<T> Converter { get; }
    public TimeSpan Duration { get; }
    public Func<double, double> Easing { get; }

    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        AnimationValidation.Vectors(initialValue, initialVelocity, targetValue, _control1, _control2);
        var start = initialValue.Copy();
        var end = targetValue.Copy();
        return new SampledAnimation(Duration.TotalSeconds, seconds =>
        {
            var t = Easing(Math.Clamp(seconds / Duration.TotalSeconds, 0, 1));
            var inverse = 1 - t;
            var value = start.Copy();
            for (var index = 0; index < value.Dimension; index++)
                value[index] = (float)(inverse * inverse * inverse * start[index]
                    + 3 * inverse * inverse * t * _control1[index]
                    + 3 * inverse * t * t * _control2[index]
                    + t * t * t * end[index]);
            return value;
        });
    }
}
