using CommunityToolkit.Mvvm.ComponentModel;

namespace ComposeAnimation.Runtime;

public interface ITransitionAnimation
{
    string Label { get; }
    TimeSpan Duration { get; }
    bool IsFinished { get; }
}

internal interface ITransitionAnimationNode : ITransitionAnimation
{
    void Reconfigure(bool reset, bool seeking, bool preserveCurrentValue);
    void Pulse(TimeSpan playTime, bool scaleToEnd);
    void Seek(TimeSpan playTime);
    void Detach();
}

public abstract class Transition : ObservableObject, IDisposable
{
    private readonly List<ITransitionAnimationNode> _animations = [];
    private readonly List<Transition> _transitions = [];
    private readonly HashSet<TransitionPreparationLease> _preparations = [];
    private readonly HashSet<TransitionPreparationLease> _runtimeRetentions = [];
    private readonly Queue<Action> _mutations = new();
    private readonly Queue<Action> _notifications = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly AnimationLifetime? _scopeLifetime;
    // Mutations are confined to the owning clock thread. Wake the idle loop inline so
    // it registers for the next frame batch before the mutation returns.
    private TaskCompletionSource _changed = new();
    private bool _processing;
    private bool _loopActive;
    private bool _disposed;
    private bool _running;
    private bool _phaseQueued;
    private bool _updateChildrenNeeded;
    private TimeSpan _playTime;
    private TimeSpan _lastFrameTime = AnimationConstants.UnspecifiedTime;
    private TimeSpan _startTime = AnimationConstants.UnspecifiedTime;
    private double _durationScale = 1;

    private protected Transition(IAnimationFrameClock clock, Transition? parent, string? label)
    {
        ArgumentNullException.ThrowIfNull(clock);
        clock.VerifyAccess();
        Clock = clock;
        _scopeLifetime = (clock as IAnimationLifetimeClock)?.AnimationLifetime;
        _scopeLifetime?.Register(this, static animation => animation.Dispose());
        ParentTransition = parent;
        Label = label;
        Animations = _animations.AsReadOnly();
        Transitions = _transitions.AsReadOnly();
    }

    public Transition? ParentTransition { get; }
    public string? Label { get; }
    public bool IsRunning => _running;
    public bool IsSeeking { get; private protected set; }
    /// <summary>Whether this transition or a descendant is waiting for preparation.</summary>
    public bool IsPreparing => _preparations.Count > 0 || _transitions.Any(t => t.IsPreparing);
    public TimeSpan PlayTime => Root._playTime;
    public IReadOnlyList<ITransitionAnimation> Animations { get; }
    public IReadOnlyList<Transition> Transitions { get; }
    public TimeSpan TotalDuration => _animations.Select(a => a.Duration)
        .Concat(_transitions.Select(t => t.TotalDuration)).DefaultIfEmpty().Max();

    // MotionDurationScale comes from a coroutine context upstream; the Avalonia host sets it here.
    public double DurationScale
    {
        get => Root._durationScale;
        set
        {
            if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Mutate(() =>
            {
                Root._durationScale = value;
                Root.Notify(nameof(DurationScale));
            });
        }
    }

    internal IAnimationFrameClock Clock { get; }
    internal bool IsDisposed => _disposed;
    internal Action? SynchronizeWithParent { get; set; }
    // One delivery per mutation batch, after that batch. Play time, segment, duration, and
    // sampled values stay on PropertyChanged.
    internal event Action? PhaseChanged;
    private protected Transition Root => ParentTransition?.Root ?? this;
    private protected abstract bool HasStateChange { get; }
    private protected abstract void SetStateRunning(bool value);
    private protected abstract void CompleteState();
    private protected abstract void RemoveState();

    private bool HasWork => !IsSeeking && (_preparations.Count > 0 || _runtimeRetentions.Count > 0 || _updateChildrenNeeded || HasStateChange ||
        _animations.Any(a => !a.IsFinished) || _transitions.Any(t => t.HasWork));

    /// <summary>
    /// Prevents completion while layout or other presentation data is being prepared. Acquire
    /// before changing the target, register the real animations before releasing the lease, and
    /// release in a finally block on failure/cancellation. Existing animations keep advancing;
    /// the lease adds no duration and is unaffected by DurationScale (including zero).
    /// Multiple leases are independent. Release is idempotent and uses the owning clock thread.
    /// Disposal and entering manual seeking revoke outstanding leases. A delayed callback must
    /// check IsActive before installing its results. Acquisition during seeking is not supported.
    /// </summary>
    public TransitionPreparationLease BeginPreparation()
    {
        VerifyAccess();
        if (IsSeeking) throw new InvalidOperationException("Preparation cannot begin during manual seeking.");
        var lease = new TransitionPreparationLease(this);
        Mutate(() =>
        {
            if (!lease.IsActive) return;
            if (IsSeeking) { lease.Revoke(); return; }
            _preparations.Add(lease);
            PreparationChanged();
        });
        return lease;
    }

    internal TransitionPreparationLease BeginRuntimeRetention()
    {
        VerifyAccess();
        var lease = new TransitionPreparationLease(this, runtime: true);
        Mutate(() => { if (!lease.IsActive || IsSeeking) { lease.Revoke(); return; } _runtimeRetentions.Add(lease); });
        return lease;
    }

    internal void ReleasePreparation(TransitionPreparationLease lease)
    {
        Clock.VerifyAccess();
        if (!lease.IsActive) return;
        lease.Revoke();
        Root.Execute(() =>
        {
            if (_preparations.Remove(lease)) PreparationChanged();
            else _runtimeRetentions.Remove(lease);
        });
    }

    private void PreparationChanged()
    {
        for (Transition? current = this; current is not null; current = current.ParentTransition)
        {
            current.NotifyPhase(nameof(IsPreparing));
            // Mark the branch busy immediately, including when current == target. Releasing
            // does not complete it synchronously: a frame must evaluate any newly added work.
            if (current.IsPreparing && !current.IsSeeking) current.SetRunning(true);
        }
        AnimationChanged();
    }

    private protected void RevokePreparationsForSeeking()
    {
        foreach (var child in _transitions) child.RevokePreparationsForSeeking();
        if (_preparations.Count == 0) return;
        foreach (var lease in _preparations) lease.Revoke();
        _preparations.Clear();
        foreach (var lease in _runtimeRetentions) lease.Revoke();
        _runtimeRetentions.Clear();
        PreparationChanged();
    }

    // Explicit lifetime and registration replace remember/DisposableEffect, not animation sampling.
    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        VerifyAccess();
        return _scopeLifetime?.Track(() => RunCoreAsync(cancellationToken)) ?? RunCoreAsync(cancellationToken);
    }

    private async Task RunCoreAsync(CancellationToken cancellationToken)
    {
        VerifyAccess();
        if (ParentTransition is not null) throw new InvalidOperationException("A child uses its parent's frame loop.");
        if (_loopActive) throw new InvalidOperationException("This transition already has a frame loop.");
        _loopActive = true;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token, _scopeLifetime?.Token ?? default);
        try
        {
            while (true)
            {
                linked.Token.ThrowIfCancellationRequested();
                if (!HasWork)
                {
                    await _changed.Task.WaitAsync(linked.Token).ConfigureAwait(true);
                    _changed = new TaskCompletionSource();
                    continue;
                }
                var frame = await Clock.NextFrameAsync(linked.Token).ConfigureAwait(true);
                linked.Token.ThrowIfCancellationRequested();
                if (!IsSeeking && !_disposed) OnFrame(frame);
            }
        }
        finally
        {
            _loopActive = false;
            _lastFrameTime = AnimationConstants.UnspecifiedTime;
            _startTime = AnimationConstants.UnspecifiedTime;
            Execute(() => SetRunningRecursively(false));
        }
    }

    internal void OnFrame(TimeSpan frameTime)
    {
        Mutate(() =>
        {
            if (IsSeeking) return;
            if (!AnimationConstants.IsUnspecified(_lastFrameTime) && frameTime < _lastFrameTime)
                throw new ArgumentOutOfRangeException(nameof(frameTime));
            var scale = _durationScale * ((Clock as IAnimationLifetimeClock)?.DurationScale ?? 1);
            if (AnimationConstants.IsUnspecified(_startTime))
                _startTime = frameTime - TimeSpan.FromTicks(checked((long)(_playTime.Ticks * scale)));
            if (scale > 0)
            {
                if (_scopeLifetime is not null)
                {
                    if (!AnimationConstants.IsUnspecified(_lastFrameTime))
                        _playTime += TimeSpan.FromTicks(checked((long)Math.Round((frameTime - _lastFrameTime).Ticks / scale)));
                }
                else _playTime = TimeSpan.FromTicks(checked((long)Math.Round((frameTime - _startTime).Ticks / scale)));
            }
            _lastFrameTime = frameTime;
            Pulse(_playTime, scale == 0);
            Notify(nameof(PlayTime));
        });
    }

    private void Pulse(TimeSpan playTime, bool scaleToEnd)
    {
        if (_disposed || IsSeeking || !HasWork) return;
        SetRunning(true);
        _updateChildrenNeeded = false;
        foreach (var animation in _animations)
            if (!animation.IsFinished) animation.Pulse(playTime, scaleToEnd);
        foreach (var transition in _transitions) transition.Pulse(playTime, scaleToEnd);

        if (_preparations.Count == 0 && _animations.All(a => a.IsFinished) && _transitions.All(t => !t.HasWork))
        {
            CompleteState();
            SetRunning(false);
            if (ParentTransition is null)
            {
                _playTime = TimeSpan.Zero;
                _lastFrameTime = AnimationConstants.UnspecifiedTime;
                _startTime = AnimationConstants.UnspecifiedTime;
            }
        }
    }

    private void SetRunning(bool value)
    {
        // Stopping a frame loop does not release presentation reservations. Keep state.IsIdle
        // false until their owner releases them (or disposal/seeking revokes them).
        value |= IsPreparing && !IsSeeking;
        if (_running == value) return;
        _running = value;
        SetStateRunning(value);
        NotifyPhase(nameof(IsRunning));
    }

    private protected void SetRunningRecursively(bool value)
    {
        SetRunning(value);
        foreach (var transition in _transitions) transition.SetRunningRecursively(value);
    }

    /// <summary>
    /// Puts this transition (and all of its children) in or out of manual play-time mode.
    ///
    /// A seekable transition owns its frame loop, rather than using <see cref="RunAsync"/>.  The
    /// flag therefore has to be changed as one tree-wide operation: a child sampling from a
    /// different clock mode would otherwise immediately overwrite the value selected by its
    /// parent.
    /// </summary>
    internal void SetSeekingMode(bool value)
    {
        VerifyAccess();
        Root.Execute(() =>
        {
            if (value) RevokePreparationsForSeeking();
            SetSeekingRecursively(value);
        });
    }

    internal void SetRunningMode(bool value)
    {
        Mutate(() => SetRunningRecursively(value));
    }

    private void SetSeekingRecursively(bool value)
    {
        if (IsSeeking != value)
        {
            IsSeeking = value;
            NotifyPhase(nameof(IsSeeking));
        }

        if (value) SetRunning(false);
        foreach (var transition in _transitions) transition.SetSeekingRecursively(value);
    }

    /// <summary>Samples every animation in the tree at the supplied play time.</summary>
    internal void SeekToPlayTime(TimeSpan playTime)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(playTime, TimeSpan.Zero);
        Mutate(() =>
        {
            if (!IsSeeking) SetSeekingRecursively(true);
            SetSeekTime(playTime);
            SeekAnimations(playTime);
        });
    }

    /// <summary>
    /// Ends a seekable operation.  Sampling at the end is deliberately performed by the caller;
    /// this method only commits the state and returns the tree to normal frame-loop mode.
    /// </summary>
    internal void FinishSeeking()
    {
        Mutate(() =>
        {
            SnapAnimationsToEnd();
            CompleteState();
            SetRunningRecursively(false);
            SetSeekingRecursively(false);
            _playTime = TimeSpan.Zero;
            _lastFrameTime = AnimationConstants.UnspecifiedTime;
            _startTime = AnimationConstants.UnspecifiedTime;
            Notify(nameof(PlayTime));
        });
    }

    private protected void SnapAnimationsToEnd()
    {
        foreach (var animation in _animations) animation.Pulse(animation.Duration, true);
        foreach (var transition in _transitions) transition.SnapAnimationsToEnd();
    }

    internal void AddAnimation(ITransitionAnimationNode animation)
    {
        _animations.Add(animation);
        AnimationChanged(!animation.IsFinished);
    }

    internal void RemoveAnimation(ITransitionAnimationNode animation)
    {
        _animations.Remove(animation);
        AnimationChanged();
    }

    internal void AddTransition(Transition transition)
    {
        _transitions.Add(transition);
        AnimationChanged(transition.HasWork);
    }

    internal void AnimationChanged(bool requireFrame = true)
    {
        _updateChildrenNeeded |= requireFrame;
        Notify(nameof(TotalDuration));
        ParentTransition?.AnimationChanged(requireFrame);
        if (requireFrame) Root._changed.TrySetResult();
    }

    private protected void ReconfigureAnimations(bool reset, bool preserveCurrentValue = false)
    {
        foreach (var animation in _animations)
            animation.Reconfigure(reset, IsSeeking, preserveCurrentValue);
        foreach (var child in _transitions) child.SynchronizeWithParent?.Invoke();
        AnimationChanged();
    }

    public void RefreshAnimations() => Mutate(() =>
    {
        ReconfigureAnimations(false);
        if (IsSeeking) SeekAnimations(PlayTime);
    });

    private protected void SetSeekTime(TimeSpan playTime)
    {
        Root._lastFrameTime = AnimationConstants.UnspecifiedTime;
        Root._startTime = AnimationConstants.UnspecifiedTime;
        Root._playTime = playTime;
    }

    private protected void SeekAnimations(TimeSpan playTime)
    {
        // Sampling is shared by manual seeking and seekable playback. The operation that
        // enters/exits seeking owns running state; a sample must not stop active playback.
        foreach (var animation in _animations) animation.Seek(playTime);
        foreach (var transition in _transitions)
            if (transition.IsSeeking) transition.SeekAnimations(playTime);
        Notify(nameof(PlayTime));
        Notify(nameof(IsSeeking));
    }

    internal void VerifyAccess()
    {
        Clock.VerifyAccess();
        _scopeLifetime?.VerifyActive(this);
        ObjectDisposedException.ThrowIf(_disposed || Root._disposed, this);
    }

    internal void Mutate(Action action)
    {
        VerifyAccess();
        Root.Execute(() => { if (!_disposed && _scopeLifetime?.IsEnding != true) action(); });
    }

    // PropertyChanged handlers can synchronously mutate the tree; finish the current batch first.
    private void Execute(Action action)
    {
        _mutations.Enqueue(action);
        if (_processing) return;
        _processing = true;
        try
        {
            while (_mutations.Count > 0 || _notifications.Count > 0)
            {
                while (_mutations.TryDequeue(out var mutation)) mutation();
                while (_notifications.TryDequeue(out var notification)) notification();
            }
        }
        catch
        {
            // A queued phase callback may be discarded below. Clear the coalesce flag or the
            // next phase change never delivers.
            ResetPhaseQueue();
            throw;
        }
        finally
        {
            _mutations.Clear();
            _notifications.Clear();
            _processing = false;
        }
    }

    internal void Publish(Action notification) => Root._notifications.Enqueue(notification);
    private protected void Notify(string name) => Publish(() => OnPropertyChanged(name));

    // PropertyChanged still carries the name. PhaseChanged is the coalesced structural signal.
    private protected void NotifyPhase(string name)
    {
        Notify(name);
        if (_phaseQueued) return;
        _phaseQueued = true;
        Publish(() =>
        {
            _phaseQueued = false;
            PhaseChanged?.Invoke();
        });
    }

    private void ResetPhaseQueue()
    {
        _phaseQueued = false;
        foreach (var child in _transitions) child.ResetPhaseQueue();
    }

    public void Dispose()
    {
        Clock.VerifyAccess();
        if (_disposed) return;
        Root.Execute(DisposeCore);
    }

    private void DisposeCore()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        foreach (var lease in _preparations) lease.Revoke();
        _preparations.Clear();
        NotifyPhase(nameof(IsPreparing));
        foreach (var child in _transitions.ToArray()) child.DisposeCore();
        foreach (var animation in _animations) animation.Detach();
        _animations.Clear();
        _transitions.Clear();
        CompleteState();
        SetRunning(false);
        if (ParentTransition is null)
        {
            _playTime = TimeSpan.Zero;
            _lastFrameTime = AnimationConstants.UnspecifiedTime;
            _startTime = AnimationConstants.UnspecifiedTime;
        }
        RemoveState();
        SynchronizeWithParent = null;
        if (ParentTransition is { } parent)
        {
            parent._transitions.Remove(this);
            parent.PreparationChanged();
        }
        _lifetime.Dispose();
    }
}

public readonly record struct TransitionSegment<TState>(TState InitialState, TState TargetState)
{
    public bool IsTransitioningTo(TState initialState, TState targetState)
        => EqualityComparer<TState>.Default.Equals(InitialState, initialState) &&
           EqualityComparer<TState>.Default.Equals(TargetState, targetState);
}
