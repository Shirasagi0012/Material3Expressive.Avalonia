namespace ComposeAnimation.Runtime;

public enum RepeatMode
{
    /// <summary>Restarts the animation from the start value to the end value.</summary>
    Restart,

    /// <summary>Reverses the last iteration as the animation repeats.</summary>
    Reverse,
}

public enum StartOffsetType
{
    /// <summary>Delays the start of the animation.</summary>
    Delay,

    /// <summary>Fast forwards the animation to a given play time and starts it immediately.</summary>
    FastForward,
}

/// <summary>
/// A start offset for <see cref="RepeatableSpec{T}"/> and <see cref="InfiniteRepeatableSpec{T}"/>.
/// The offset is applied once, before the first iteration, and is not repeated.
/// </summary>
public readonly struct StartOffset
{
    /// <summary>The signed offset in milliseconds; negative for <see cref="StartOffsetType.Delay"/>.</summary>
    internal int Value { get; }

    public StartOffset(int offsetMillis, StartOffsetType offsetType = StartOffsetType.Delay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offsetMillis);
        Value = offsetMillis * (offsetType == StartOffsetType.Delay ? -1 : 1);
    }

    public int OffsetMillis => Math.Abs(Value);

    public StartOffsetType OffsetType => Value > 0 ? StartOffsetType.FastForward : StartOffsetType.Delay;
}

/// <summary>
/// Plays a <see cref="IDurationBasedAnimationSpec{T}"/> <see cref="Iterations"/> times.
/// </summary>
public sealed class RepeatableSpec<T> : IFiniteAnimationSpec<T>
{
    public RepeatableSpec(
        int iterations,
        IDurationBasedAnimationSpec<T> animation,
        RepeatMode repeatMode = RepeatMode.Restart,
        StartOffset initialStartOffset = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        ArgumentNullException.ThrowIfNull(animation);

        Iterations = iterations;
        Animation = animation;
        RepeatMode = repeatMode;
        InitialStartOffset = initialStartOffset;
    }

    public int Iterations { get; }
    public IDurationBasedAnimationSpec<T> Animation { get; }
    public RepeatMode RepeatMode { get; }
    public StartOffset InitialStartOffset { get; }

    public IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<T> converter)
        => new VectorizedRepeatableSpec(Iterations, Animation.Vectorize(converter), RepeatMode, InitialStartOffset);
}

/// <summary>
/// Plays a <see cref="IDurationBasedAnimationSpec{T}"/> infinitely. The animation never finishes on its
/// own and must be stopped by an external action.
/// </summary>
public sealed class InfiniteRepeatableSpec<T> : IAnimationSpec<T>
{
    public InfiniteRepeatableSpec(
        IDurationBasedAnimationSpec<T> animation,
        RepeatMode repeatMode = RepeatMode.Restart,
        StartOffset initialStartOffset = default)
    {
        ArgumentNullException.ThrowIfNull(animation);
        if (animation.Duration == TimeSpan.Zero && animation.Delay == TimeSpan.Zero)
            throw new ArgumentException("Animation to be infinitely repeated cannot have a 0-duration.", nameof(animation));

        Animation = animation;
        RepeatMode = repeatMode;
        InitialStartOffset = initialStartOffset;
    }

    public IDurationBasedAnimationSpec<T> Animation { get; }
    public RepeatMode RepeatMode { get; }
    public StartOffset InitialStartOffset { get; }

    public IVectorizedAnimationSpec Vectorize(ITwoWayConverter<T> converter)
        => new VectorizedInfiniteRepeatableSpec(Animation.Vectorize(converter), RepeatMode, InitialStartOffset);
}

public sealed class VectorizedRepeatableSpec : IVectorizedFiniteAnimationSpec
{
    private readonly int _iterations;
    private readonly IVectorizedDurationBasedAnimationSpec _animation;
    private readonly RepeatMode _repeatMode;
    private readonly TimeSpan _initialOffset;

    private TimeSpan IterationDuration => _animation.Delay + _animation.Duration;

    public VectorizedRepeatableSpec(
        int iterations,
        IVectorizedDurationBasedAnimationSpec animation,
        RepeatMode repeatMode = RepeatMode.Restart,
        StartOffset initialStartOffset = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        ArgumentNullException.ThrowIfNull(animation);

        _iterations = iterations;
        _animation = animation;
        _repeatMode = repeatMode;
        _initialOffset = TimeSpan.FromMilliseconds(initialStartOffset.Value);
    }

    public TimeSpan GetDuration(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity)
        => IterationDuration * _iterations - _initialOffset;

    public void GetValue(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    )
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        Span<double> startVelocity = stackalloc double[initial.Length];
        _animation.GetValue(
            RepetitionPlayTime(playTime),
            initial,
            target,
            GetEffectiveInitialVelocity(playTime, initial, target, initialVelocity, startVelocity),
            destination);
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
        Span<double> startVelocity = stackalloc double[initial.Length];
        _animation.GetVelocity(
            RepetitionPlayTime(playTime),
            initial,
            target,
            GetEffectiveInitialVelocity(playTime, initial, target, initialVelocity, startVelocity),
            destination);
    }

    private TimeSpan RepetitionPlayTime(TimeSpan playTime)
    {
        var postOffset = playTime + _initialOffset;
        if (postOffset <= TimeSpan.Zero)
            return TimeSpan.Zero;

        var repeats = Math.Min((long)(postOffset / IterationDuration), _iterations - 1L);
        return _repeatMode == RepeatMode.Restart || repeats % 2 == 0
            ? postOffset - IterationDuration * repeats
            : IterationDuration * (repeats + 1) - postOffset;
    }

    /// <summary>
    /// The start velocity of the 2nd and subsequent iterations is the velocity at the end of the
    /// first iteration, instead of the initial velocity.
    /// </summary>
    private ReadOnlySpan<double> GetEffectiveInitialVelocity(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> startVelocity)
    {
        if (playTime + _initialOffset <= IterationDuration)
            return initialVelocity;

        _animation.GetVelocity(IterationDuration - _initialOffset, initial, target, initialVelocity, startVelocity);
        return startVelocity;
    }
}

public sealed class VectorizedInfiniteRepeatableSpec : IVectorizedAnimationSpec
{
    private readonly IVectorizedDurationBasedAnimationSpec _animation;
    private readonly RepeatMode _repeatMode;
    private readonly TimeSpan _initialOffset;

    private TimeSpan IterationDuration => _animation.Delay + _animation.Duration;

    public VectorizedInfiniteRepeatableSpec(
        IVectorizedDurationBasedAnimationSpec animation,
        RepeatMode repeatMode = RepeatMode.Restart,
        StartOffset initialStartOffset = default)
    {
        ArgumentNullException.ThrowIfNull(animation);

        _animation = animation;
        _repeatMode = repeatMode;
        _initialOffset = TimeSpan.FromMilliseconds(initialStartOffset.Value);
    }

    public bool IsInfinite => true;

    public TimeSpan GetDuration(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity)
        => TimeSpan.MaxValue;

    public void GetValue(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> destination
    )
    {
        VectorValidation.Validate(initial, target, initialVelocity, destination.Length);
        Span<double> startVelocity = stackalloc double[initial.Length];
        _animation.GetValue(
            RepetitionPlayTime(playTime),
            initial,
            target,
            GetEffectiveInitialVelocity(playTime, initial, target, initialVelocity, startVelocity),
            destination);
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
        Span<double> startVelocity = stackalloc double[initial.Length];
        _animation.GetVelocity(
            RepetitionPlayTime(playTime),
            initial,
            target,
            GetEffectiveInitialVelocity(playTime, initial, target, initialVelocity, startVelocity),
            destination);
    }

    private TimeSpan RepetitionPlayTime(TimeSpan playTime)
    {
        var postOffset = playTime + _initialOffset;
        if (postOffset <= TimeSpan.Zero)
            return TimeSpan.Zero;

        var repeats = (long)(postOffset / IterationDuration);
        return _repeatMode == RepeatMode.Restart || repeats % 2 == 0
            ? postOffset - IterationDuration * repeats
            : IterationDuration * (repeats + 1) - postOffset;
    }

    private ReadOnlySpan<double> GetEffectiveInitialVelocity(
        TimeSpan playTime,
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        Span<double> startVelocity)
    {
        if (playTime + _initialOffset <= IterationDuration)
            return initialVelocity;

        _animation.GetVelocity(IterationDuration - _initialOffset, initial, target, initialVelocity, startVelocity);
        return startVelocity;
    }
}
