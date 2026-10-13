using CommunityToolkit.Mvvm.ComponentModel;

namespace ComposeAnimation.Runtime;

public interface IInfiniteTransitionAnimation
{
    string Label { get; }
    bool IsFinished { get; }
}

internal interface IInfiniteTransitionAnimationNode : IInfiniteTransitionAnimation
{
    void OnPlayTimeChanged(TimeSpan playTime);
    void SkipToEnd();
    void Reset();
    void Detach();
}

/// <summary>
/// Shared-timeline driver for infinitely repeating child animations, adapted from Compose
/// <c>InfiniteTransition</c>. Unlike <see cref="Transition"/>, children join the parent play time
/// (they stay in phase) and a <see cref="DurationScale"/> change restarts every child from its
/// initial value rather than scaling remaining time.
///
/// State only (D19): no clock, lifetime or scope field. <see cref="RunAsync"/> runs the loop in a
/// <see cref="FrameScope"/>; one loop at a time (D21), a newer run cancels the current one and
/// starts after its cleanup. When a run ends because its scope ended or a newer run took over,
/// play time is kept and the next run continues it; cancelling through the caller's token or
/// <see cref="Dispose"/> resets it as before. Threading: the constructing thread owns the object.
/// </summary>
public sealed class InfiniteTransition : ObservableObject, IDisposable
{
    private readonly List<IInfiniteTransitionAnimationNode> _animations = [];
    private readonly Queue<Action> _mutations = new();
    private readonly Queue<Action> _notifications = new();
    private readonly ThreadAffinity _owner = ThreadAffinity.Current;
    private readonly MutatorMutex _runMutex = new();
    private TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _processing;
    private bool _resumePlayTime;
    private bool _disposed;
    private bool _running;
    private bool _refreshChildNeeded;
    private TimeSpan _playTime;
    private TimeSpan _lastFrameTime = AnimationConstants.UnspecifiedTime;
    private TimeSpan _startTime = AnimationConstants.UnspecifiedTime;
    private double _durationScale = 1;
    private double _appliedDurationScale = 1;

    public InfiniteTransition(string? label = "InfiniteTransition")
    {
        Label = label;
        Animations = _animations.AsReadOnly();
    }

    public string? Label { get; }
    public bool IsRunning => _running;
    public TimeSpan PlayTime => _playTime;
    public IReadOnlyList<IInfiniteTransitionAnimation> Animations { get; }

    /// <summary>
    /// Multiplier applied to wall-clock deltas when computing play time. Default is 1.
    /// Must be finite and &gt;= 0. Changing the scale restarts every child; 0 snaps each child to
    /// its target and suspends the frame loop until the scale is positive again.
    /// </summary>
    public double DurationScale
    {
        get => _durationScale;
        set
        {
            if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Mutate(() =>
            {
                if (_durationScale == value) return;
                _durationScale = value;
                Notify(nameof(DurationScale));
                _changed.TrySetResult();
            });
        }
    }

    private double EffectiveDurationScale(double scopeScale) => _durationScale * scopeScale;

    private bool HasWork => _refreshChildNeeded || _animations.Any(a => !a.IsFinished);

    public TransitionAnimationState<TValue> AnimateValue<TValue>(
        TValue initialValue,
        TValue targetValue,
        ITwoWayConverter<TValue> typeConverter,
        InfiniteRepeatableSpec<TValue> animationSpec,
        string label = "ValueAnimation")
    {
        ArgumentNullException.ThrowIfNull(typeConverter);
        ArgumentNullException.ThrowIfNull(animationSpec);
        var animation = new TransitionAnimationState<TValue>(
            this, initialValue, targetValue, typeConverter, animationSpec, label);
        Mutate(() =>
        {
            if (animation.IsDetached) return;
            AddAnimation(animation);
        });
        return animation;
    }

    public Task RunAsync(FrameScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        VerifyAccess();
        scope.VerifyAccess();
        return scope.Track(() => _runMutex.MutateAsync(mutexToken => LoopAsync(scope, cancellationToken, mutexToken),
            MutatePriority.Default, cancellationToken));
    }

    private async Task<bool> LoopAsync(FrameScope scope, CancellationToken cancellationToken, CancellationToken mutexToken)
    {
        VerifyAccess();
        // The run's one token: caller, scope lifetime and mutex (Dispose or a newer run).
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scope.Lifetime, mutexToken);
        var token = linked.Token;
        using var playback = scope.RegisterPlaybackChanged(this, static animation => animation._changed.TrySetResult());
        var keepPlayTime = false;
        try
        {
            // No PlayTimeAccumulator here: upstream parity. A scale change restarts the infinite
            // animations from the start, which the accumulator would not
            // (InfiniteTransitionTest.DurationScale_SlowsThenRestartOnChange; see DurationScale docs).
            // One token check per frame: here once, then after each frame wait. The change waits
            // observe the token themselves.
            token.ThrowIfCancellationRequested();
            while (true)
            {
                if (_disposed) throw new OperationCanceledException(token);

                if (EffectiveDurationScale(scope.EffectiveDurationScale) == 0)
                {
                    Mutate(() => SkipToEndAll());
                    await WaitForChangeAsync(token).ConfigureAwait(true);
                    continue;
                }

                if (!HasWork)
                {
                    await WaitForChangeAsync(token).ConfigureAwait(true);
                    continue;
                }

                // The scope's own wait (no async layer): resumes inline in the frame batch.
                var frame = await scope.WaitFrameAsync(token, null).ConfigureAwait(true);
                token.ThrowIfCancellationRequested();
                if (!_disposed) OnFrame(frame, scope.EffectiveDurationScale);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            // Scope ended or a newer run took over: the next run continues this play time.
            keepPlayTime = true;
            throw;
        }
        finally
        {
            Execute(() =>
            {
                _lastFrameTime = AnimationConstants.UnspecifiedTime;
                _startTime = AnimationConstants.UnspecifiedTime;
                _resumePlayTime = keepPlayTime && !_disposed;
                if (!_resumePlayTime) _playTime = TimeSpan.Zero;
                SetRunning(false);
                Notify(nameof(PlayTime));
            });
        }
    }

    internal void OnFrame(TimeSpan frameTime) => OnFrame(frameTime, 1);

    private void OnFrame(TimeSpan frameTime, double scopeScale)
    {
        Mutate(() =>
        {
            if (_disposed) return;
            if (!AnimationConstants.IsUnspecified(_lastFrameTime) && frameTime < _lastFrameTime)
                throw new ArgumentOutOfRangeException(nameof(frameTime));
            _lastFrameTime = frameTime;
            var scale = EffectiveDurationScale(scopeScale);

            if (AnimationConstants.IsUnspecified(_startTime) || _appliedDurationScale != scale)
            {
                if (_resumePlayTime && AnimationConstants.IsUnspecified(_startTime) && _appliedDurationScale == scale && scale > 0)
                {
                    // A new run continues the play time of a run its scope or a newer run ended.
                    _startTime = frameTime - TimeSpan.FromTicks(checked((long)Math.Round(_playTime.Ticks * scale)));
                }
                else
                {
                    _startTime = frameTime;
                    foreach (var animation in _animations) animation.Reset();
                }
                _resumePlayTime = false;
                _appliedDurationScale = scale;
            }

            if (scale == 0)
            {
                SkipToEndAll();
                return;
            }

            _playTime = TimeSpan.FromTicks(
                checked((long)Math.Round((frameTime - _startTime).Ticks / scale)));
            Pulse(_playTime);
            Notify(nameof(PlayTime));
        });
    }

    private void Pulse(TimeSpan playTime)
    {
        _refreshChildNeeded = false;
        var allFinished = true;
        foreach (var animation in _animations)
        {
            if (!animation.IsFinished) animation.OnPlayTimeChanged(playTime);
            if (!animation.IsFinished) allFinished = false;
        }
        SetRunning(!allFinished);
    }

    private void SkipToEndAll()
    {
        _appliedDurationScale = 0;
        _refreshChildNeeded = false;
        foreach (var animation in _animations) animation.SkipToEnd();
        SetRunning(false);
    }

    private void SetRunning(bool value)
    {
        if (_running == value) return;
        _running = value;
        Notify(nameof(IsRunning));
    }

    private void AddAnimation(IInfiniteTransitionAnimationNode animation)
    {
        _animations.Add(animation);
        _refreshChildNeeded = true;
        _changed.TrySetResult();
    }

    internal void RemoveAnimation(IInfiniteTransitionAnimationNode animation)
    {
        _animations.Remove(animation);
        _changed.TrySetResult();
    }

    internal void AnimationChanged()
    {
        _refreshChildNeeded = true;
        _changed.TrySetResult();
    }

    private async Task WaitForChangeAsync(CancellationToken cancellationToken)
    {
        await _changed.Task.WaitAsync(cancellationToken).ConfigureAwait(true);
        _changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    internal void VerifyThreadAccess() => _owner.Verify(this);

    internal void VerifyAccess()
    {
        _owner.Verify(this);
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    internal void Mutate(Action action)
    {
        VerifyAccess();
        Execute(() => { if (!_disposed) action(); });
    }

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
        finally
        {
            _mutations.Clear();
            _notifications.Clear();
            _processing = false;
        }
    }

    internal void Publish(Action notification) => _notifications.Enqueue(notification);
    private void Notify(string name) => Publish(() => OnPropertyChanged(name));

    public void Dispose()
    {
        VerifyThreadAccess();
        if (_disposed) return;
        Execute(DisposeCore);
    }

    private void DisposeCore()
    {
        if (_disposed) return;
        _disposed = true;
        _runMutex.CancelCurrent();
        foreach (var animation in _animations) animation.Detach();
        _animations.Clear();
        SetRunning(false);
        _playTime = TimeSpan.Zero;
        _lastFrameTime = AnimationConstants.UnspecifiedTime;
        _startTime = AnimationConstants.UnspecifiedTime;
        _changed.TrySetResult();
        Notify(nameof(PlayTime));
    }

    public sealed class TransitionAnimationState<TValue> : ObservableObject, IInfiniteTransitionAnimationNode, IAnimationValue<TValue>, IDisposable
    {
        private readonly InfiniteTransition _owner;
        private InfiniteRepeatableSpec<TValue> _animationSpec;
        private bool _detached;
        private bool _startOnTheNextFrame;
        private TimeSpan _playTimeOffset;

        internal TransitionAnimationState(
            InfiniteTransition owner,
            TValue initialValue,
            TValue targetValue,
            ITwoWayConverter<TValue> typeConverter,
            InfiniteRepeatableSpec<TValue> animationSpec,
            string label)
        {
            _owner = owner;
            TypeConverter = typeConverter;
            Label = label;
            InitialValue = initialValue;
            TargetValue = targetValue;
            _animationSpec = animationSpec;
            Value = initialValue;
            Animation = new TargetBasedAnimation<TValue>(animationSpec, typeConverter, initialValue, targetValue);
        }

        public ITwoWayConverter<TValue> TypeConverter { get; }
        public string Label { get; }
        public TValue Value { get; private set; }
        public TValue InitialValue { get; private set; }
        public TValue TargetValue { get; private set; }
        public IAnimationSpec<TValue> AnimationSpec => _animationSpec;
        public TargetBasedAnimation<TValue> Animation { get; private set; }
        public bool IsFinished { get; private set; }
        internal bool IsDetached => _detached;

        void IInfiniteTransitionAnimationNode.OnPlayTimeChanged(TimeSpan playTime) => OnPlayTimeChanged(playTime);

        private void OnPlayTimeChanged(TimeSpan playTime)
        {
            if (_detached) return;
            _owner._refreshChildNeeded = false;
            if (_startOnTheNextFrame)
            {
                _startOnTheNextFrame = false;
                _playTimeOffset = playTime;
            }

            var local = playTime - _playTimeOffset;
            var old = Value;
            var wasFinished = IsFinished;
            Value = Animation.GetValue(local);
            IsFinished = Animation.IsFinished(local);
            if (!EqualityComparer<TValue>.Default.Equals(old, Value)) Notify(nameof(Value));
            if (wasFinished != IsFinished) Notify(nameof(IsFinished));
        }

        void IInfiniteTransitionAnimationNode.SkipToEnd()
        {
            if (_detached) return;
            var old = Value;
            Value = Animation.TargetValue;
            _startOnTheNextFrame = true;
            if (!EqualityComparer<TValue>.Default.Equals(old, Value)) Notify(nameof(Value));
        }

        void IInfiniteTransitionAnimationNode.Reset() => _startOnTheNextFrame = true;

        public void Update(
            TValue initialValue,
            TValue targetValue,
            InfiniteRepeatableSpec<TValue>? animationSpec = null)
        {
            VerifyAttached();
            _owner.Mutate(() =>
            {
                if (_detached) return;
                var spec = animationSpec ?? _animationSpec;
                if (EqualityComparer<TValue>.Default.Equals(InitialValue, initialValue) &&
                    EqualityComparer<TValue>.Default.Equals(TargetValue, targetValue) &&
                    ReferenceEquals(_animationSpec, spec))
                    return;
                UpdateValues(initialValue, targetValue, spec);
            });
        }

        private void UpdateValues(
            TValue initialValue,
            TValue targetValue,
            InfiniteRepeatableSpec<TValue> animationSpec)
        {
            InitialValue = initialValue;
            TargetValue = targetValue;
            _animationSpec = animationSpec;
            Animation = new TargetBasedAnimation<TValue>(animationSpec, TypeConverter, initialValue, targetValue);
            IsFinished = false;
            _startOnTheNextFrame = true;
            _owner.AnimationChanged();
            Notify(nameof(InitialValue));
            Notify(nameof(TargetValue));
            Notify(nameof(AnimationSpec));
            Notify(nameof(Animation));
            Notify(nameof(IsFinished));
        }

        private void Notify(string name) => _owner.Publish(() =>
        {
            if (!_detached) OnPropertyChanged(name);
        });

        private void VerifyAttached()
        {
            _owner.VerifyAccess();
            ObjectDisposedException.ThrowIf(_detached, this);
        }

        public void Dispose()
        {
            _owner.VerifyThreadAccess();
            if (_detached) return;
            _owner.Mutate(() =>
            {
                _detached = true;
                _owner.RemoveAnimation(this);
            });
        }

        void IInfiniteTransitionAnimationNode.Detach() => _detached = true;
    }
}
