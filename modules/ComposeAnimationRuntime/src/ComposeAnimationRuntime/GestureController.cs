using System.Numerics;

namespace ComposeAnimationRuntime;

public enum GestureState { Idle, Dragging, Settling, Completed, Canceled }

public sealed class GestureController
{
    private readonly Animatable<Vector2> _position;
    private readonly Vector2VectorConverter _converter;
    private Vector2 _min;
    private Vector2 _max;
    private Vector2 _last;
    private TimeSpan _lastTime;
    private Vector2 _releaseVelocity;

    public GestureController(Vector2 initial = default)
    {
        _converter = new Vector2VectorConverter();
        _position = new Animatable<Vector2>(initial, _converter);
    }

    public Vector2 Position => _position.Value;
    public Vector2 Velocity => _position.Velocity;
    public GestureState State { get; private set; } = GestureState.Idle;
    public bool IsRunning => _position.IsRunning;
    public event Action<Vector2>? PositionChanged;

    public void SetBounds(Vector2 minimum, Vector2 maximum)
    {
        if (minimum.X > maximum.X || minimum.Y > maximum.Y) throw new ArgumentOutOfRangeException(nameof(maximum));
        _min = minimum; _max = maximum;
        SnapTo(Position);
    }

    public void Begin(Vector2 pointer, TimeSpan timestamp)
    {
        _position.Stop();
        _releaseVelocity = Vector2.Zero;
        _last = pointer; _lastTime = timestamp;
        State = GestureState.Dragging;
    }

    public void Update(Vector2 pointer, TimeSpan timestamp)
    {
        if (State != GestureState.Dragging) return;
        if (timestamp < _lastTime)
            timestamp = _lastTime;
        var delta = pointer - _last;
        var elapsed = timestamp - _lastTime;
        var next = Clamp(Position + delta);
        SnapTo(next);
        if (elapsed > TimeSpan.Zero) _releaseVelocity = (pointer - _last) / (float)elapsed.TotalSeconds;
        _position.Stop();
        _last = pointer; _lastTime = timestamp;
    }

    public void End(bool fling = true, Vector2? settleTarget = null)
    {
        if (State != GestureState.Dragging) return;
        State = GestureState.Settling;
        if (settleTarget is { } target)
            _position.AnimateTo(Clamp(target), new SpringAnimationSpec<Vector2>(_converter, dampingRatio: 0.82f, stiffness: 420f));
        else if (fling && _releaseVelocity.LengthSquared() > 0.0001f)
            _position.AnimateDecay(_releaseVelocity, new DecaySpec<Vector2>(_converter, 8));
        else SnapTo(Position);
    }

    public bool Tick(TimeSpan delta)
    {
        var running = _position.Tick(delta);
        if (State == GestureState.Settling)
        {
            var bounded = Clamp(Position);
            if (bounded != Position)
            {
                SnapTo(bounded);
                _position.Stop();
                running = false;
            }
        }
        PositionChanged?.Invoke(Position);
        if (!running && State == GestureState.Settling) State = GestureState.Completed;
        return running;
    }

    public void Cancel() { _position.Stop(); State = GestureState.Canceled; }
    private void SnapTo(Vector2 value) { _position.SnapTo(Clamp(value)); PositionChanged?.Invoke(Position); }
    private Vector2 Clamp(Vector2 value) => Vector2.Clamp(value, _min, _max);
}

internal sealed class Vector2VectorConverter : IAnimationVectorConverter<Vector2>
{
    public Vector2 VisibilityThreshold => new(0.01f);
    public AnimationVector ConvertToVector(Vector2 value) => new AnimationVector2D(value.X, value.Y);
    public Vector2 ConvertFromVector(AnimationVector vector) => new(vector[0], vector[1]);
}
