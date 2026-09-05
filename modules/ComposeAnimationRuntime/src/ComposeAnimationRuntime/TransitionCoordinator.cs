namespace ComposeAnimationRuntime;

public sealed class TransitionCoordinator
{
    private readonly TransitionSession _session;
    private readonly List<ITrack> _tracks = [];

    public TransitionCoordinator(IAnimationClock? clock = null)
    {
        _session = new TransitionSession(clock);
        _session.StateChanged += state => StateChanged?.Invoke(state);
    }

    public TransitionSessionState State => _session.State;
    public bool IsRunning => _session.IsRunning;
    public IAnimationClock? Clock => _session.Clock;
    public event Action<TransitionSessionState>? StateChanged;

    public void Add<T>(Animatable<T> animation)
    {
        ArgumentNullException.ThrowIfNull(animation);
        if (_session.State != TransitionSessionState.Idle)
            throw new InvalidOperationException("Tracks can only be added before the coordinator starts.");
        var existing = _tracks.FirstOrDefault(track => ReferenceEquals(track.Animation, animation));
        if (existing is not null)
        {
            if (existing.Priority >= 0) return;
        }
        _tracks.Add(new Track<T>(animation, 0));
        _session.Add(animation.Tick, () => animation.IsRunning);
    }

    public void Add(ITransitionTrack track)
        => Add(track, 0);

    public void Add(ITransitionTrack track, int priority)
    {
        ArgumentNullException.ThrowIfNull(track);
        if (_session.State != TransitionSessionState.Idle)
            throw new InvalidOperationException("Tracks can only be added before the coordinator starts.");
        var existing = _tracks.FirstOrDefault(item => ReferenceEquals(item.Animation, track));
        if (existing is not null && existing.Priority >= priority) return;
        if (existing is not null) _tracks.Remove(existing);
        _tracks.Add(new TrackAdapter(track, priority));
        _session.Add(track.Tick, () => track.IsRunning);
    }

    public void Start() => _session.Start();
    public bool Tick(TimeSpan delta) => _session.Tick(delta);
    public void Pause() { _session.Pause(); foreach (var track in _tracks) track.Pause(); }
    public void Resume() { _session.Resume(); foreach (var track in _tracks) track.Resume(); }
    public void Complete() { foreach (var track in _tracks) track.Complete(); _session.Complete(); }
    public void Cancel() { foreach (var track in _tracks) track.Cancel(); _session.Cancel(); }

    private interface ITrack
    {
        object Animation { get; }
        int Priority { get; }
        void Pause();
        void Resume();
        void Complete();
        void Cancel();
    }

    private sealed class Track<T>(Animatable<T> animation, int priority) : ITrack
    {
        public object Animation => animation;
        public int Priority => priority;
        public void Pause() => animation.Pause();
        public void Resume() => animation.Resume();
        public void Complete() => animation.Settle();
        public void Cancel() => animation.Stop();
    }

    private sealed class TrackAdapter(ITransitionTrack track, int priority) : ITrack
    {
        public object Animation => track;
        public int Priority => priority;
        public void Pause() => track.Pause();
        public void Resume() => track.Resume();
        public void Complete() => track.Complete();
        public void Cancel() => track.Cancel();
    }
}
