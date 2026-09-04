namespace ComposeAnimationRuntime;

/// <summary>Coordinates named animated values around a target state.</summary>
public sealed class MotionTransition<TState>
{
    private readonly Dictionary<string, Action<TState>> _targets = new();
    private readonly List<Func<TimeSpan, bool>> _animations = [];
    public MotionTransition(TState initialState) => CurrentState = TargetState = initialState;
    public TState CurrentState { get; private set; }
    public TState TargetState { get; private set; }
    public bool IsRunning { get; private set; }

    public void Register(string name, Action<TState> applyTarget)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(applyTarget);
        _targets[name] = applyTarget;
    }

    public void Register<TValue>(string name, Animatable<TValue> animation, Func<TState, TValue> target,
        Func<TState, IAnimationSpec<TValue>> specification)
    {
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(specification);
        Register(name, state => animation.AnimateTo(target(state), specification(state)));
        _animations.Add(animation.Tick);
    }

    public void AnimateTo(TState targetState)
    {
        TargetState = targetState;
        foreach (var track in _targets.Values)
            track(targetState);
        IsRunning = _animations.Count > 0;
    }

    public bool Tick(TimeSpan delta)
    {
        if (!IsRunning)
            return false;

        var running = false;
        foreach (var animation in _animations)
            running |= animation(delta);
        if (!running)
            Complete();
        return IsRunning;
    }

    public void Complete() { CurrentState = TargetState; IsRunning = false; }
}
