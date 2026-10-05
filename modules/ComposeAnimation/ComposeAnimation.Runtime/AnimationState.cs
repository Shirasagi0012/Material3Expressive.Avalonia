namespace ComposeAnimation.Runtime;

/// <summary>
/// Execution state, adapted from AndroidX AnimationState (Apache-2.0).
/// Mutated by the runtime; copies own independent velocity storage.
/// </summary>
public sealed class AnimationState<T>
{
    private readonly AnimationVector _velocityVector;

    public AnimationState(
        ITwoWayConverter<T> typeConverter,
        T initialValue,
        AnimationVector? initialVelocityVector = null,
        TimeSpan? lastFrameTime = null,
        TimeSpan? finishedTime = null,
        bool isRunning = false
    )
    {
        ArgumentNullException.ThrowIfNull(typeConverter);
        ArgumentOutOfRangeException.ThrowIfLessThan(typeConverter.Dimension, 1);
        
        if (initialVelocityVector is not null &&
            initialVelocityVector.Dimension != typeConverter.Dimension)
            throw new ArgumentException("Velocity dimension must match the converter.", nameof(initialVelocityVector));

        TypeConverter = typeConverter;
        Value = initialValue;
        _velocityVector = initialVelocityVector?.Clone() ?? new AnimationVector(typeConverter.Dimension);
        LastFrameTime = lastFrameTime ?? AnimationConstants.UnspecifiedTime;
        FinishedTime = finishedTime ?? AnimationConstants.UnspecifiedTime;
        IsRunning = isRunning;
    }

    public ITwoWayConverter<T> TypeConverter { get; }
    public T Value { get; internal set; }
    public ReadOnlySpan<double> VelocityVector => _velocityVector.AsReadOnlySpan();
    public T Velocity => TypeConverter.From(VelocityVector);
    public bool IsRunning { get; internal set; }
    public bool IsFinished => !AnimationConstants.IsUnspecified(FinishedTime);

    /// <summary>
    /// Timestamp in the frame clock's timebase, not elapsed play time.
    /// <see cref="AnimationConstants.UnspecifiedTime"/> until the first frame.
    /// </summary>
    public TimeSpan LastFrameTime { get; internal set; }

    /// <summary>
    /// Frame timestamp when the animation finished, or
    /// <see cref="AnimationConstants.UnspecifiedTime"/> if it has not finished.
    /// </summary>
    public TimeSpan FinishedTime { get; internal set; }

    public AnimationState<T> Copy() =>
        new(TypeConverter, Value, _velocityVector, LastFrameTime, FinishedTime, IsRunning);

    internal Span<double> MutableVelocityVector => _velocityVector.AsSpan();

    /// <summary>Populates all fields from an <see cref="AnimationScope{T}"/>; mirrors updateState upstream.</summary>
    internal void UpdateFrom(AnimationScope<T> scope)
    {
        Value = scope.Value;
        _velocityVector.CopyFrom(scope.VelocityVector);
        FinishedTime = scope.FinishedTime;
        LastFrameTime = scope.LastFrameTime;
        IsRunning = scope.IsRunning;
    }

    internal void ResetAfterRun()
    {
        _velocityVector.Clear();
        IsRunning = false;
    }
}
