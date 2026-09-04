namespace ComposeAnimationRuntime;

/// <summary>A retargetable value animation with explicit, deterministic frame stepping.</summary>
public sealed class Animatable<T>
{
    private readonly IAnimationVectorConverter<T> _converter;
    private AnimationVector _value;
    private AnimationVector _velocity;
    private VectorAnimation? _animation;
    private TimeSpan _playTime;
    private AnimationVector _target;

    public Animatable(T initialValue, IAnimationVectorConverter<T> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _value = converter.ConvertToVector(initialValue);
        _target = _value.Copy();
        _velocity = _value.Copy();
        _velocity.Reset();
    }

    public T Value => _converter.ConvertFromVector(_value);
    public T TargetValue => _converter.ConvertFromVector(_target);
    public T Velocity => _converter.ConvertFromVector(_velocity);
    public bool IsRunning => _animation is not null;
    public event Action<T>? ValueChanged;
    public double Progress => _animation is { DurationSeconds: > 0 and var duration } ? Math.Clamp(_playTime.TotalSeconds / duration, 0, 1) : IsRunning ? 0 : 1;

    public void AnimateTo(T target, IAnimationSpec<T> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var nextTarget = _converter.ConvertToVector(target);
        AnimationValidation.Vectors(_value, _velocity, nextTarget);
        _target = nextTarget;
        _animation = spec.Create(_value.Copy(), _velocity.Copy(), _target.Copy());
        _playTime = TimeSpan.Zero;
        if (_animation.DurationSeconds == 0)
            Finish();
    }

    public void AnimateDecay(T initialVelocity, DecaySpec<T> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var velocity = _converter.ConvertToVector(initialVelocity);
        AnimationValidation.Vectors(_value, velocity);
        _target = _value.Copy();
        _animation = spec.Create(_value.Copy(), velocity, _target.Copy());
        _playTime = TimeSpan.Zero;
    }

    public void SnapTo(T value)
    {
        _value = _converter.ConvertToVector(value);
        _target = _value.Copy();
        _velocity.Reset();
        _animation = null;
        _playTime = TimeSpan.Zero;
        ValueChanged?.Invoke(Value);
    }

    public bool Tick(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);
        if (_animation is null) return false;
        _playTime += delta;
        var state = _animation.Sample(_playTime);
        _value = state.Value;
        _velocity = state.Velocity;
        ValueChanged?.Invoke(Value);
        if (state.IsFinished) Finish();
        return IsRunning;
    }

    public void Seek(TimeSpan playTime)
    {
        if (_animation is null) return;
        ArgumentOutOfRangeException.ThrowIfLessThan(playTime, TimeSpan.Zero);
        _playTime = playTime;
        var state = _animation.Sample(playTime);
        _value = state.Value;
        _velocity = state.Velocity;
        ValueChanged?.Invoke(Value);
    }

    public void Stop() => _animation = null;
    public void Settle() { if (_animation is not null) Finish(); }

    private void Finish()
    {
        if (_animation is not null && double.IsFinite(_animation.DurationSeconds))
        {
            var state = _animation.Sample(TimeSpan.FromSeconds(_animation.DurationSeconds));
            _value = state.Value;
        }
        _value = _target.Copy();
        _velocity.Reset();
        _animation = null;
    }
}
