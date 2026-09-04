namespace ComposeAnimationRuntime;

/// <summary>Coordinates several animated values around a logical target state.</summary>
public sealed class MotionTransition<TState>
{
    private readonly Dictionary<string, Action<TState>> _targets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ITransitionTrack> _tracks = new(StringComparer.Ordinal);

    public MotionTransition(TState initialState) => CurrentState = TargetState = initialState;
    public TState CurrentState { get; private set; }
    public TState TargetState { get; private set; }
    public bool IsRunning { get; private set; }
    public event Action<TState>? StateChanged;

    public void Register(string name, Action<TState> applyTarget)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(applyTarget);
        if (_targets.ContainsKey(name)) throw new ArgumentException($"A track named '{name}' is already registered.", nameof(name));
        _targets.Add(name, applyTarget);
    }

    public void Register<TValue>(string name, Animatable<TValue> animation, Func<TState, TValue> target,
        Func<TState, IAnimationSpec<TValue>> specification)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(specification);
        Register(name, state => animation.AnimateTo(target(state), specification(state)));
        _tracks.Add(name, new TransitionTrack<TValue>(animation));
    }

    public void AnimateTo(TState targetState)
    {
        TargetState = targetState;
        foreach (var target in _targets.Values) target(targetState);
        IsRunning = _tracks.Values.Any(track => track.IsRunning);
        if (!IsRunning) Complete();
    }

    public bool Tick(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);
        if (!IsRunning) return false;
        foreach (var track in _tracks.Values) track.Tick(delta);
        if (_tracks.Values.All(track => !track.IsRunning)) Complete();
        return IsRunning;
    }

    public void Seek(TimeSpan playTime)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(playTime, TimeSpan.Zero);
        foreach (var track in _tracks.Values) track.Seek(playTime);
    }

    public void SnapTo(TState state)
    {
        TargetState = CurrentState = state;
        foreach (var target in _targets.Values) target(state);
        foreach (var track in _tracks.Values) track.Settle();
        IsRunning = false;
        StateChanged?.Invoke(state);
    }

    public void Stop()
    {
        foreach (var track in _tracks.Values) track.Stop();
        IsRunning = false;
    }

    public void Complete()
    {
        CurrentState = TargetState;
        IsRunning = false;
        StateChanged?.Invoke(CurrentState);
    }

    private interface ITransitionTrack
    {
        bool IsRunning { get; }
        void Tick(TimeSpan delta);
        void Seek(TimeSpan playTime);
        void Settle();
        void Stop();
    }

    private sealed class TransitionTrack<TValue>(Animatable<TValue> animation) : ITransitionTrack
    {
        public bool IsRunning => animation.IsRunning;
        public void Tick(TimeSpan delta) => animation.Tick(delta);
        public void Seek(TimeSpan playTime) => animation.Seek(playTime);
        public void Settle() => animation.Settle();
        public void Stop() => animation.Stop();
    }
}
