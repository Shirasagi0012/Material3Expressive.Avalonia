namespace ComposeAnimation.Runtime;

/// <summary>
/// Per-frame view of a running animation, adapted from AndroidX AnimationScope (Apache-2.0).
/// A run view is created for each animation run and updated on every frame; it is only meaningful
/// for the duration of that run.
/// </summary>
public sealed class AnimationRun<T>
{
    private readonly AnimationVector _velocityVector;
    private readonly Action _onCancel;

    internal AnimationRun(
        ITwoWayConverter<T> typeConverter,
        T initialValue,
        ReadOnlySpan<double> initialVelocityVector,
        T targetValue,
        TimeSpan startTime,
        Action onCancel)
    {
        TypeConverter = typeConverter;
        Value = initialValue;
        _velocityVector = AnimationVector.From(initialVelocityVector);
        TargetValue = targetValue;
        StartTime = startTime;
        LastFrameTime = startTime;
        FinishedTime = AnimationConstants.UnspecifiedTime;
        IsRunning = true;
        _onCancel = onCancel;
    }

    /// <summary>Converter between <typeparamref name="T"/> and <see cref="AnimationVector"/>.</summary>
    public ITwoWayConverter<T> TypeConverter { get; }

    /// <summary>Current value of the animation.</summary>
    public T Value { get; internal set; }

    /// <summary>Current velocity vector of the animation.</summary>
    public ReadOnlySpan<double> VelocityVector => _velocityVector.AsReadOnlySpan();

    /// <summary>Current velocity, converted from <see cref="VelocityVector"/>.</summary>
    public T Velocity => TypeConverter.From(_velocityVector.AsReadOnlySpan());

    /// <summary>Target value of the animation.</summary>
    public T TargetValue { get; }

    /// <summary>Start time of the animation in the frame clock's timebase.</summary>
    public TimeSpan StartTime { get; }

    /// <summary>Timestamp of the last frame the animation was updated on.</summary>
    public TimeSpan LastFrameTime { get; internal set; }

    /// <summary>Timestamp of the frame on which the animation finished, or <see cref="AnimationConstants.UnspecifiedTime"/> if it has not.</summary>
    public TimeSpan FinishedTime { get; internal set; }

    /// <summary>Indicates whether the animation is currently running.</summary>
    public bool IsRunning { get; internal set; }

    internal Span<double> MutableVelocityVector => _velocityVector.AsSpan();

    /// <summary>
    /// Cancels the animation that this scope corresponds to. The scope is not updated any more
    /// after this call.
    /// </summary>
    public void CancelAnimation()
    {
        IsRunning = false;
        _onCancel();
    }
}
