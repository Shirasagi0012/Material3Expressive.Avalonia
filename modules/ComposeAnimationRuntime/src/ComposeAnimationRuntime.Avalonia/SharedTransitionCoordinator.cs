using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class SharedTransitionCoordinator
{
    private readonly List<SharedBounds> _bounds = [];
    private readonly List<SharedElement> _elements = [];
    private readonly TransitionCoordinator _coordinator;

    public SharedTransitionCoordinator(IAnimationClock? clock = null, TransitionConflictPolicy conflictPolicy = TransitionConflictPolicy.IgnoreLowerPriority)
    {
        _coordinator = new TransitionCoordinator(clock, conflictPolicy);
        _coordinator.StateChanged += state => StateChanged?.Invoke(this, state);
    }

    public TransitionSessionState State => _coordinator.State;
    public bool IsRunning => _coordinator.IsRunning;
    public TransitionConflictPolicy ConflictPolicy => _coordinator.ConflictPolicy;
    public event EventHandler<TransitionSessionState>? StateChanged;

    public void Add(SharedBounds bounds)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        if (_coordinator.State != TransitionSessionState.Idle)
            throw new InvalidOperationException("Bounds can only be added before the transition starts.");
        if (_bounds.Contains(bounds)) return;
        bounds.AttachClock(Clock);
        _bounds.Add(bounds);
        _coordinator.Add((ITransitionTrack)bounds);
    }

    public void Add(SharedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (_coordinator.State != TransitionSessionState.Idle)
            throw new InvalidOperationException("Elements can only be added before the transition starts.");
        if (_elements.Contains(element)) return;
        element.AttachClock(Clock);
        _elements.Add(element);
        _coordinator.Add((ITransitionTrack)element);
    }

    public IAnimationClock? Clock => _coordinator.Clock;

    public void Start() => _coordinator.Start();
    public bool Tick(TimeSpan delta) => _coordinator.Tick(delta);
    public void Seek(TimeSpan playTime) => _coordinator.Seek(playTime);
    public void Pause() => _coordinator.Pause();
    public void Resume() => _coordinator.Resume();
    public void Complete() => _coordinator.Complete();
    public void Cancel() => _coordinator.Cancel();
}
