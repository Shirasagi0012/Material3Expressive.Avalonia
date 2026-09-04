namespace ComposeAnimationRuntime;

public interface IAnimationSpec<T>
{
    VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue);
}

public interface IDurationAnimationSpec<T> : IAnimationSpec<T>
{
    TimeSpan Duration { get; }
}

public readonly record struct AnimationState(AnimationVector Value, AnimationVector Velocity, bool IsFinished);

public abstract class VectorAnimation
{
    public abstract double DurationSeconds { get; }
    public abstract AnimationState Sample(TimeSpan playTime);
    protected static void ValidateTime(TimeSpan playTime) => ArgumentOutOfRangeException.ThrowIfLessThan(playTime, TimeSpan.Zero);
}

internal sealed class SampledAnimation(double durationSeconds, Func<double, AnimationVector> position, Func<double, AnimationVector>? velocity = null) : VectorAnimation
{
    public override double DurationSeconds { get; } = durationSeconds;
    public override AnimationState Sample(TimeSpan playTime)
    {
        ValidateTime(playTime);
        var seconds = Math.Min(playTime.TotalSeconds, DurationSeconds);
        var value = position(seconds);
        var speed = velocity?.Invoke(seconds) ?? Zero(value);
        return new AnimationState(value, speed, playTime.TotalSeconds >= DurationSeconds);
    }
    private static AnimationVector Zero(AnimationVector source) { var result = source.Copy(); result.Reset(); return result; }
}

internal static class AnimationValidation
{
    internal static void Vectors(params AnimationVector[] vectors)
    {
        if (vectors.Length == 0) throw new ArgumentException("At least one vector is required.", nameof(vectors));
        var dimension = vectors[0].Dimension;
        if (dimension <= 0) throw new ArgumentException("Animation vectors must have at least one component.");
        foreach (var vector in vectors)
        {
            ArgumentNullException.ThrowIfNull(vector);
            if (vector.Dimension != dimension) throw new ArgumentException("Animation vector dimensions must match.");
            for (var index = 0; index < dimension; index++)
                if (!float.IsFinite(vector[index])) throw new ArgumentException("Animation vector components must be finite.");
        }
    }
    internal static AnimationVector Lerp(AnimationVector start, AnimationVector target, double fraction)
    { var result = start.Copy(); for (var index = 0; index < result.Dimension; index++) result[index] += (float)((target[index] - start[index]) * fraction); return result; }
}

public readonly record struct Keyframe<T>(TimeSpan Time, T Value, Func<double, double>? Easing = null);

public sealed class KeyframesSpec<T> : IDurationAnimationSpec<T>
{
    public KeyframesSpec(IAnimationVectorConverter<T> converter, TimeSpan duration, params Keyframe<T>[] keyframes)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        if (keyframes is null || keyframes.Length == 0) throw new ArgumentException("At least one keyframe is required.", nameof(keyframes));
        if (keyframes.Any(frame => frame.Time < TimeSpan.Zero || frame.Time > duration)) throw new ArgumentException("Keyframe times must be within the animation duration.", nameof(keyframes));
        Converter = converter; Duration = duration; Keyframes = keyframes.OrderBy(frame => frame.Time).ToArray();
    }
    public IAnimationVectorConverter<T> Converter { get; }
    public TimeSpan Duration { get; }
    public IReadOnlyList<Keyframe<T>> Keyframes { get; }
    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        AnimationValidation.Vectors(initialValue, initialVelocity, targetValue);
        var frames = Keyframes.Select(frame => (frame.Time.TotalSeconds, Value: Converter.ConvertToVector(frame.Value), frame.Easing ?? Easings.Linear)).ToArray();
        return new SampledAnimation(Duration.TotalSeconds, seconds =>
        {
            var left = frames[0]; var right = frames[^1];
            foreach (var frame in frames) if (frame.Item1 >= seconds) { right = frame; break; } else left = frame;
            var fraction = right.Item1 <= left.Item1 ? 1 : (seconds - left.Item1) / (right.Item1 - left.Item1);
            return AnimationValidation.Lerp(left.Value, right.Value, right.Item3(Math.Clamp(fraction, 0, 1)));
        });
    }
}

public sealed class RepeatableSpec<T> : IAnimationSpec<T>
{
    public RepeatableSpec(IDurationAnimationSpec<T> animation, int iterations, bool reverse = false)
    { ArgumentNullException.ThrowIfNull(animation); ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(iterations, 0); Animation = animation; Iterations = iterations; Reverse = reverse; }
    public IDurationAnimationSpec<T> Animation { get; }
    public int Iterations { get; }
    public bool Reverse { get; }
    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        var inner = Animation.Create(initialValue, initialVelocity, targetValue); var duration = Animation.Duration.TotalSeconds * Iterations;
        return new SampledAnimation(duration, seconds => { var iteration = Math.Min((int)(seconds / Animation.Duration.TotalSeconds), Iterations - 1); var local = seconds - iteration * Animation.Duration.TotalSeconds; var sample = inner.Sample(TimeSpan.FromSeconds(local)).Value; return Reverse && iteration % 2 == 1 ? AnimationValidation.Lerp(targetValue, initialValue, local / Animation.Duration.TotalSeconds) : sample; });
    }
}

public sealed class InfiniteRepeatableSpec<T> : IAnimationSpec<T>
{
    public InfiniteRepeatableSpec(IDurationAnimationSpec<T> animation, bool reverse = false) { ArgumentNullException.ThrowIfNull(animation); Animation = animation; Reverse = reverse; }
    public IDurationAnimationSpec<T> Animation { get; }
    public bool Reverse { get; }
    public VectorAnimation Create(AnimationVector initialValue, AnimationVector initialVelocity, AnimationVector targetValue)
    {
        var inner = Animation.Create(initialValue, initialVelocity, targetValue);
        return new SampledAnimation(double.PositiveInfinity, seconds => { var iteration = (int)(seconds / Animation.Duration.TotalSeconds); var local = seconds - iteration * Animation.Duration.TotalSeconds; return Reverse && iteration % 2 == 1 ? AnimationValidation.Lerp(targetValue, initialValue, local / Animation.Duration.TotalSeconds) : inner.Sample(TimeSpan.FromSeconds(local)).Value; });
    }
}
