namespace ComposeAnimationRuntime;

public sealed class PathSpec<T> : IDurationAnimationSpec<T>
{
    private readonly IReadOnlyList<T> _points;
    private readonly IReadOnlyList<Func<double, double>> _easings;

    public PathSpec(IAnimationVectorConverter<T> converter, TimeSpan duration,
        IReadOnlyList<T> points, IReadOnlyList<Func<double, double>>? easings = null)
    {
        Converter = converter ?? throw new ArgumentNullException(nameof(converter));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        if (points is null || points.Count < 2)
            throw new ArgumentException("At least two path points are required.", nameof(points));
        _points = points.ToArray();
        _easings = easings is null
            ? Enumerable.Repeat(Easings.Linear, _points.Count).ToArray()
            : easings.ToArray();
        if (_easings.Count != _points.Count)
            throw new ArgumentException("There must be one easing function per path segment.", nameof(easings));
        Duration = duration;
    }

    public IAnimationVectorConverter<T> Converter { get; }
    public TimeSpan Duration { get; }

    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        AnimationValidation.Vectors(initialValue, initialVelocity, targetValue);
        var points = new List<AnimationVector> { initialValue.Copy() };
        points.AddRange(_points.Select(Converter.ConvertToVector));
        points[^1] = targetValue.Copy();
        AnimationValidation.Vectors(points.ToArray());
        var segmentDuration = Duration.TotalSeconds / (points.Count - 1);
        return new SampledAnimation(Duration.TotalSeconds, seconds =>
        {
            var segment = Math.Min((int)(seconds / segmentDuration), points.Count - 2);
            var local = Math.Clamp((seconds - segment * segmentDuration) / segmentDuration, 0, 1);
            var fraction = _easings[segment](local);
            return AnimationValidation.Lerp(points[segment], points[segment + 1], fraction);
        });
    }
}
