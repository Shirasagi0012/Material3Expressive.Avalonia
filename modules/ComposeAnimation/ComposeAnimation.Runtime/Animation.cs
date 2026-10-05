namespace ComposeAnimation.Runtime;

public abstract class Animation<T>
{
    public abstract T TargetValue { get; }
    public abstract TimeSpan Duration { get; }
    public virtual bool IsInfinite => false;

    public abstract T GetValue(TimeSpan playTime);
    public abstract T GetVelocity(TimeSpan playTime);

    /// <summary>Copies unprojected velocity, preserving precision for integer and color converters.</summary>
    public abstract void GetVelocityVector(TimeSpan playTime, Span<double> destination);

    public virtual AnimationSample<T> Sample(TimeSpan playTime) =>
        new(playTime < TimeSpan.Zero ? TimeSpan.Zero : playTime,
            GetValue(playTime), GetVelocity(playTime), IsFinished(playTime));

    public virtual bool IsFinished(TimeSpan playTime) =>
        !IsInfinite && playTime >= Duration;
}

public readonly record struct AnimationSample<T>(
    TimeSpan PlayTime,
    T Value,
    T Velocity,
    bool IsFinished);

public sealed class TargetBasedAnimation<T> : Animation<T>
{
    private readonly ITwoWayConverter<T> _converter;
    private readonly IVectorizedAnimationSpec _spec;
    private readonly AnimationVector _initial;
    private readonly AnimationVector _target;
    private readonly AnimationVector _initialVelocity;
    private readonly AnimationVector _valueBuffer;
    private readonly AnimationVector _velocityBuffer;
    private readonly T _initialValue;
    private readonly T _targetValue;

    public TargetBasedAnimation(
        IAnimationSpec<T> spec,
        ITwoWayConverter<T> converter,
        T initialValue,
        T targetValue,
        T? initialVelocity = default,
        AnimationVector? initialVelocityVector = null)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(converter);
        if (converter.Dimension <= 0)
            throw new ArgumentOutOfRangeException(nameof(converter), "Converter dimension must be positive.");

        _converter = converter;
        _initialValue = initialValue;
        _targetValue = targetValue;
        _initial = new AnimationVector(converter.Dimension);
        _target = new AnimationVector(converter.Dimension);
        _initialVelocity = new AnimationVector(converter.Dimension);
        _valueBuffer = new AnimationVector(converter.Dimension);
        _velocityBuffer = new AnimationVector(converter.Dimension);

        converter.To(initialValue, _initial.AsSpan());
        converter.To(targetValue, _target.AsSpan());

        if (initialVelocityVector is not null)
            _initialVelocity.CopyFrom(initialVelocityVector.AsReadOnlySpan());
        else if (initialVelocity is { })
            converter.To(initialVelocity, _initialVelocity.AsSpan());

        _spec = spec.Vectorize(converter);
        
        Duration = _spec.GetDuration(_initial.AsReadOnlySpan(), _target.AsReadOnlySpan(), _initialVelocity.AsReadOnlySpan());
    }

    public T InitialValue => _initialValue;
    public override T TargetValue => _targetValue;
    public T InitialVelocity => _converter.From(_initialVelocity.AsReadOnlySpan());
    public override TimeSpan Duration { get; }
    public override bool IsInfinite => _spec.IsInfinite;

    public override T GetValue(TimeSpan playTime)
    {
        if (IsFinished(playTime)) return TargetValue;
        _spec.GetValue(
            playTime, 
            _initial.AsReadOnlySpan(), 
            _target.AsReadOnlySpan(),
            _initialVelocity.AsReadOnlySpan(), 
            _valueBuffer.AsSpan());
        
        return _converter.From(_valueBuffer.AsReadOnlySpan());
    }

    public override T GetVelocity(TimeSpan playTime)
    {
        GetVelocityVector(playTime, _velocityBuffer.AsSpan());
        return _converter.From(_velocityBuffer.AsReadOnlySpan());
    }

    public override void GetVelocityVector(TimeSpan playTime, Span<double> destination)
    {
        if (destination.Length != _initial.Dimension)
            throw new ArgumentException("Velocity dimension must match the animation.", nameof(destination));
        if (IsFinished(playTime))
            _spec.GetEndVelocity(_initial.AsReadOnlySpan(), _target.AsReadOnlySpan(), _initialVelocity.AsReadOnlySpan(), destination);
        else
            _spec.GetVelocity(playTime, _initial.AsReadOnlySpan(), _target.AsReadOnlySpan(), _initialVelocity.AsReadOnlySpan(), destination);
    }

    public T GetEndVelocity()
    {
        _spec.GetEndVelocity(
            _initial.AsReadOnlySpan(),
            _target.AsReadOnlySpan(),
            _initialVelocity.AsReadOnlySpan(),
            _velocityBuffer.AsSpan());
        
        return _converter.From(_velocityBuffer.AsReadOnlySpan());
    }

    
    public override AnimationSample<T> Sample(TimeSpan playTime)
    {
        if (playTime < TimeSpan.Zero)
            playTime = TimeSpan.Zero;

        return new AnimationSample<T>(
            playTime,
            GetValue(playTime),
            GetVelocity(playTime),
            IsFinished(playTime)
        );
    }
}

/// <summary>
/// A decay animation with a fixed start value and start velocity. Unlike
/// <see cref="TargetBasedAnimation{T}"/>, the end value is a result of the animation rather than
/// an input.
/// </summary>
public sealed class DecayAnimation<T> : Animation<T>
{
    private readonly ITwoWayConverter<T> _converter;
    private readonly IVectorizedDecayAnimationSpec _spec;
    private readonly AnimationVector _initial;
    private readonly AnimationVector _initialVelocity;
    private readonly AnimationVector _target;
    private readonly AnimationVector _valueBuffer;
    private readonly AnimationVector _velocityBuffer;

    /// <param name="initialVelocityVector">
    /// When supplied, the start velocity is taken from this vector instead of
    /// <paramref name="initialVelocity"/>, preserving precision for integer and color converters.
    /// </param>
    public DecayAnimation(
        IDecayAnimationSpec<T> spec,
        ITwoWayConverter<T> converter,
        T initialValue,
        T? initialVelocity = default,
        AnimationVector? initialVelocityVector = null)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(converter);

        _converter = converter;
        _initial = new AnimationVector(converter.Dimension);
        _initialVelocity = new AnimationVector(converter.Dimension);
        _target = new AnimationVector(converter.Dimension);
        _valueBuffer = new AnimationVector(converter.Dimension);
        _velocityBuffer = new AnimationVector(converter.Dimension);

        converter.To(initialValue, _initial.AsSpan());

        if (initialVelocityVector is not null)
            _initialVelocity.CopyFrom(initialVelocityVector.AsReadOnlySpan());
        else if (initialVelocity is not null)
            converter.To(initialVelocity, _initialVelocity.AsSpan());

        _spec = spec.Vectorize(converter);
        _spec.GetTargetValue(_initial.AsReadOnlySpan(), _initialVelocity.AsReadOnlySpan(), _target.AsSpan());
        Duration = _spec.GetDuration(_initial.AsReadOnlySpan(), _initialVelocity.AsReadOnlySpan());
    }

    public T InitialValue => _converter.From(_initial.AsReadOnlySpan());
    public T InitialVelocity => _converter.From(_initialVelocity.AsReadOnlySpan());
    public override T TargetValue => _converter.From(_target.AsReadOnlySpan());
    public override TimeSpan Duration { get; }

    public override T GetValue(TimeSpan playTime)
    {
        if (playTime < TimeSpan.Zero) playTime = TimeSpan.Zero;
        if (IsFinished(playTime)) return TargetValue;
        _spec.GetValue(playTime, _initial.AsReadOnlySpan(), _initialVelocity.AsReadOnlySpan(), _valueBuffer.AsSpan());
        return _converter.From(_valueBuffer.AsReadOnlySpan());
    }

    public override T GetVelocity(TimeSpan playTime)
    {
        GetVelocityVector(playTime, _velocityBuffer.AsSpan());
        return _converter.From(_velocityBuffer.AsReadOnlySpan());
    }

    public override void GetVelocityVector(TimeSpan playTime, Span<double> destination)
    {
        if (destination.Length != _initial.Dimension)
            throw new ArgumentException("Velocity dimension must match the animation.", nameof(destination));
        if (playTime < TimeSpan.Zero) playTime = TimeSpan.Zero;
        if (IsFinished(playTime)) playTime = Duration;
        _spec.GetVelocity(playTime, _initial.AsReadOnlySpan(), _initialVelocity.AsReadOnlySpan(), destination);
    }
}
