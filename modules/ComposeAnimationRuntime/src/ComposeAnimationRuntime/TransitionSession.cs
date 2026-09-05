namespace ComposeAnimationRuntime;

public enum TransitionSessionState
{
    Idle,
    Running,
    Paused,
    Completed,
    Canceled,
}

public sealed class TransitionSession
{
    private readonly List<Track> _tracks = [];
    private IDisposable? _clockRegistration;

    public TransitionSession(IAnimationClock? clock = null) => Clock = clock;
    public IAnimationClock? Clock { get; }
    public TransitionSessionState State { get; private set; } = TransitionSessionState.Idle;
    public bool IsRunning => State == TransitionSessionState.Running;
    public event Action<TransitionSessionState>? StateChanged;

    public void Add(Func<TimeSpan, bool> tick, Func<bool> isRunning)
    {
        ArgumentNullException.ThrowIfNull(tick);
        ArgumentNullException.ThrowIfNull(isRunning);
        if (State != TransitionSessionState.Idle)
            throw new InvalidOperationException("Tracks can only be added before the session starts.");
        _tracks.Add(new Track(tick, isRunning));
    }

    public void Start()
    {
        if (_tracks.Count == 0) { Complete(); return; }
        SetState(TransitionSessionState.Running);
        if (Clock is not null)
            _clockRegistration = Clock.Register(Tick, () => State is TransitionSessionState.Running or TransitionSessionState.Paused);
    }

    public bool Tick(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);
        if (State == TransitionSessionState.Paused) return true;
        if (!IsRunning) return false;
        foreach (var track in _tracks)
            track.Tick(delta);
        if (_tracks.All(track => !track.IsRunning())) Complete();
        return IsRunning;
    }

    public void Pause() { if (IsRunning) SetState(TransitionSessionState.Paused); }
    public void Resume() { if (State == TransitionSessionState.Paused) SetState(TransitionSessionState.Running); }
    public void Complete() { StopRegistration(); SetState(TransitionSessionState.Completed); }
    public void Cancel() { StopRegistration(); SetState(TransitionSessionState.Canceled); }

    private void SetState(TransitionSessionState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(state);
    }

    private void StopRegistration() { _clockRegistration?.Dispose(); _clockRegistration = null; }
    private sealed record Track(Func<TimeSpan, bool> Tick, Func<bool> IsRunning);
}
