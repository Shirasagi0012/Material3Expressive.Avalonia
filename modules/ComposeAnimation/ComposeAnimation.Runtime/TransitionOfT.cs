namespace ComposeAnimation.Runtime;

public partial class Transition<TState> : Transition
{
    private readonly TransitionState<TState> _transitionState;
    private TState _targetState;
    private bool _updatingState;
    private TState? _pendingTargetState;
    private bool _hasPendingTargetState;

    public Transition(TState initialState, IFrameSource clock, string? label = null)
        : this(new MutableTransitionState<TState>(initialState), clock, null, label) { }

    public Transition(TransitionState<TState> transitionState, IFrameSource clock, string? label = null)
        : this(transitionState, clock, null, label) { }

    internal Transition(TransitionState<TState> transitionState, IFrameSource clock,
        Transition? parent, string? label) : base(clock, parent, label)
    {
        ArgumentNullException.ThrowIfNull(transitionState);
        _transitionState = transitionState;
        _targetState = transitionState.CurrentState;
        Segment = new(CurrentState, CurrentState);
        transitionState.TransitionConfigured(this);
        transitionState.Changed += OnStateChanged;
        SynchronizeState();
    }

    public TState CurrentState => _transitionState.CurrentState;
    public TState TargetState => _targetState;
    public TransitionSegment<TState> Segment { get; private set; }
    public TState? PendingTargetState => _pendingTargetState;
    public bool HasPendingTargetState => _hasPendingTargetState;
    /// <summary>
    /// The incoming target while <see cref="TargetChanging"/> runs, before <see cref="TargetState"/>
    /// is assigned. Default outside that window.
    /// </summary>
    internal TState ChangingTarget { get; private set; } = default!;

    // Presentation coordinators reserve existing content before descendants see the new target.
    // Internal synchronous hooks: public PropertyChanged is deliberately published later.
    internal event Action? TargetChanging;
    internal event Action? TargetChanged;

    private protected override bool HasStateChange
        => !EqualityComparer<TState>.Default.Equals(CurrentState, TargetState);

    private void OnStateChanged()
    {
        if (!_updatingState && !IsDisposed) SynchronizeState();
    }

    private void SynchronizeState() => Mutate(() =>
    {
        if (IsSeeking) return;
        UpdateTargetCore(_transitionState.TargetState);
        if (_transitionState is DeferredTransitionState<TState> deferred)
            UpdatePendingTarget(deferred.HasPendingTargetState, deferred.PendingTargetState);
    });

    public void UpdateTarget(TState targetState) => Mutate(() =>
    {
        WriteState(() => _transitionState.TargetState = targetState, true, targetState);
        UpdateTargetCore(targetState);
    });

    private void UpdateTargetCore(TState targetState)
    {
        if (IsSeeking) return;
        if (EqualityComparer<TState>.Default.Equals(_targetState, targetState)) return;
        ChangingTarget = targetState;
        TargetChanging?.Invoke();
        var oldTarget = _targetState;
        _targetState = targetState;
        Segment = new(oldTarget, targetState);
        WriteState(() => _transitionState.CurrentState = oldTarget);
        ReconfigureAnimations(true);
        TargetChanged?.Invoke();
        ChangingTarget = default!;
        NotifyPhase(nameof(CurrentState));
        NotifyPhase(nameof(TargetState));
        Notify(nameof(Segment));
    }

    private void UpdatePendingTarget(bool hasValue, TState? value)
    {
        if (_hasPendingTargetState == hasValue &&
            EqualityComparer<TState>.Default.Equals(_pendingTargetState, value)) return;
        var previousPending = _pendingTargetState;
        var wasPendingCleared = _hasPendingTargetState && !hasValue &&
            EqualityComparer<TState>.Default.Equals(TargetState, CurrentState);
        _hasPendingTargetState = hasValue;
        _pendingTargetState = value;
        if (wasPendingCleared)
        {
            Segment = new(previousPending!, TargetState);
            WriteState(() => _transitionState.CurrentState = previousPending!);
            ReconfigureAnimations(true);
            NotifyPhase(nameof(CurrentState));
            Notify(nameof(Segment));
        }
        else
        {
            foreach (var child in Transitions) child.SynchronizeWithParent?.Invoke();
        }
        NotifyPhase(nameof(PendingTargetState));
        NotifyPhase(nameof(HasPendingTargetState));
    }

    private void WriteState(Action action, bool writesTarget = false, TState writtenTarget = default!)
    {
        var expectedTarget = writesTarget ? writtenTarget : _transitionState.TargetState;
        var deferred = _transitionState as DeferredTransitionState<TState>;
        var hadPending = deferred?.HasPendingTargetState ?? false;
        var pending = deferred is null ? default : deferred.PendingTargetState;
        var wasUpdating = _updatingState;
        _updatingState = true;
        try { action(); }
        finally
        {
            _updatingState = wasUpdating;
            // Internal writes can synchronously invoke a consumer that requests another target.
            if (!IsDisposed && !_updatingState &&
                (!EqualityComparer<TState>.Default.Equals(expectedTarget, _transitionState.TargetState) ||
                 (deferred is not null && (hadPending != deferred.HasPendingTargetState ||
                  !EqualityComparer<TState>.Default.Equals(pending, deferred.PendingTargetState)))))
                SynchronizeState();
        }
    }

    private protected override void SetStateRunning(bool value)
        => WriteState(() => _transitionState.IsRunning = value);

    private protected override void CompleteState()
    {
        WriteState(() => _transitionState.CurrentState = TargetState);
        NotifyPhase(nameof(CurrentState));
    }

    private protected override void RemoveState()
    {
        TargetChanging = null;
        TargetChanged = null;
        _transitionState.Changed -= OnStateChanged;
        _transitionState.TransitionRemoved();
    }

    public TransitionAnimationState<TValue> AnimateValue<TValue>(ITwoWayConverter<TValue> typeConverter,
        Func<TState, TValue> targetValueByState,
        Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>>? transitionSpec = null,
        string label = "ValueAnimation")
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(typeConverter);
        ArgumentNullException.ThrowIfNull(targetValueByState);
        var animation = new TransitionAnimationState<TValue>(this, typeConverter, targetValueByState,
            transitionSpec ?? (_ => new SpringSpec<TValue>()), label);
        Mutate(() =>
        {
            if (animation.IsDetached) return;
            ((ITransitionAnimationNode)animation).Reconfigure(false, IsSeeking, false);
            if (IsSeeking) ((ITransitionAnimationNode)animation).Seek(PlayTime);
            AddAnimation(animation);
        });
        return animation;
    }

    public Transition<TChild> CreateChildTransition<TChild>(Func<TState, TChild> transformToChildState,
        string label = "ChildTransition")
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(transformToChildState);
        var state = new MutableTransitionState<TChild>(transformToChildState(CurrentState));
        var child = new Transition<TChild>(state, Clock, this, $"{Label} > {label}");
        child.SynchronizeWithParent = () =>
        {
            if (IsSeeking)
                child.SeekCore(transformToChildState(CurrentState), transformToChildState(TargetState), PlayTime,
                    isRunning: IsRunning);
            else
            {
                child.UpdateTargetCore(transformToChildState(TargetState));
                child.UpdatePendingTarget(HasPendingTargetState,
                    HasPendingTargetState ? transformToChildState(PendingTargetState!) : default);
            }
        };
        Mutate(() =>
        {
            if (child.IsDisposed) return;
            child.SynchronizeWithParent();
            AddTransition(child);
        });
        return child;
    }

    public DeferredAnimation<TValue> CreateDeferredAnimation<TValue>(ITwoWayConverter<TValue> typeConverter,
        string label = "DeferredAnimation")
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(typeConverter);
        return new(this, typeConverter, label);
    }

    public void SetPlaytimeAfterInitialAndTargetStateEstablished(TState initialState, TState targetState,
        TimeSpan playTime)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(playTime, TimeSpan.Zero);
        if (ParentTransition is not null)
            throw new InvalidOperationException("Seek the root transition to keep child timelines synchronized.");
        Mutate(() => SeekCore(initialState, targetState, playTime));
    }

    private void SeekCore(TState initialState, TState targetState, TimeSpan playTime,
        bool preserveCurrentValues = true, bool isRunning = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(playTime, TimeSpan.Zero);

        RevokePreparationsForSeeking();
        // Seeking is a tree-wide mode.  The root sets this before recursing into children, while
        // a child can still be initialized independently when it is attached to a seeking parent.
        var wasSeeking = IsSeeking;
        var segmentChanged = !EqualityComparer<TState>.Default.Equals(CurrentState, initialState) ||
            !EqualityComparer<TState>.Default.Equals(_targetState, targetState);
        if (!wasSeeking)
        {
            IsSeeking = true;
            NotifyPhase(nameof(IsSeeking));
        }
        SetRunningRecursively(isRunning);
        SetSeekTime(playTime);
        ChangingTarget = targetState;
        TargetChanging?.Invoke();
        _targetState = targetState;
        Segment = new(initialState, targetState);
        WriteState(() =>
        {
            _transitionState.CurrentState = initialState;
            _transitionState.TargetState = targetState;
        });
        // Reconfigure even when the state pair is unchanged.  Target-value selectors are regular
        // C# closures (rather than Compose snapshot reads), so this is how RefreshAnimations and
        // a parent seek pick up external inputs that changed since the previous sample.
        ReconfigureAnimations(true, preserveCurrentValues && wasSeeking && segmentChanged);
        SeekAnimations(playTime);
        ChangingTarget = default!;
        NotifyPhase(nameof(CurrentState));
        NotifyPhase(nameof(TargetState));
        Notify(nameof(Segment));
    }

    /// <summary>
    /// Initializes or retargets a seekable transition and samples it immediately.  This is kept
    /// separate from the public tooling API because seekable state operations must also update the
    /// associated <see cref="TransitionState{TState}"/> atomically.
    /// </summary>
    internal void SetSeekableState(TState initialState, TState targetState, TimeSpan playTime,
        bool preserveCurrentValues = true)
        => Mutate(() => SeekCore(initialState, targetState, playTime, preserveCurrentValues));
}

public sealed class DeferredTransition<TState>(DeferredTransitionState<TState> transitionState,
    IFrameSource clock, string? label = null) : Transition<TState>(transitionState, clock, label);
