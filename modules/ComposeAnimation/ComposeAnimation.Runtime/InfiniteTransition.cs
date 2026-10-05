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
/// </summary>
public sealed class InfiniteTransition : ObservableObject, IDisposable
{
    private readonly List<IInfiniteTransitionAnimationNode> _animations = [];
    private readonly Queue<Action> _mutations = new();
    private readonly Queue<Action> _notifications = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly AnimationLifetime? _scopeLifetime;
    private TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _processing;
    private bool _loopActive;
    private bool _disposed;
    private bool _running;
    private bool _refreshChildNeeded;
    private TimeSpan _playTime;
    private TimeSpan _lastFrameTime = AnimationConstants.UnspecifiedTime;
    private TimeSpan _startTime = AnimationConstants.UnspecifiedTime;
    private double _durationScale = 1;
    private double _appliedDurationScale = 1;

    public InfiniteTransition(IAnimationFrameClock clock, string? label = "InfiniteTransition")
    {
        ArgumentNullException.ThrowIfNull(clock);
        clock.VerifyAccess();
        Clock = clock;
        _scopeLifetime = (clock as IAnimationLifetimeClock)?.AnimationLifetime;
        _scopeLifetime?.Register(this, static animation => animation.Dispose());
        _scopeLifetime?.RegisterPlayback(this, static animation => animation._changed.TrySetResult());
        Label = label;
        Animations = _animations.AsReadOnly();
    }

    public string? Label { get; }
    public bool IsRunning => _running;
    public TimeSpan PlayTime => _playTime;
    public IReadOnlyList<IInfiniteTransitionAnimation> Animations { get; }
    internal IAnimationFrameClock Clock { get; }

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

    private double EffectiveDurationScale => _durationScale * ((Clock as IAnimationLifetimeClock)?.DurationScale ?? 1);

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

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        VerifyAccess();
        return _scopeLifetime?.Track(() => RunCoreAsync(cancellationToken)) ?? RunCoreAsync(cancellationToken);
    }

    private async Task RunCoreAsync(CancellationToken cancellationToken)
    {
        VerifyAccess();
        if (_loopActive) throw new InvalidOperationException("This infinite transition already has a frame loop.");
        _loopActive = true;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token, _scopeLifetime?.Token ?? default);
        try
        {
            while (true)
            {
                linked.Token.ThrowIfCancellationRequested();
                if (_disposed) throw new OperationCanceledException(linked.Token);

                if (EffectiveDurationScale == 0)
                {
                    Mutate(SkipToEndAll);
                    await WaitForChangeAsync(linked.Token).ConfigureAwait(true);
                    continue;
                }

                if (!HasWork)
                {
                    await WaitForChangeAsync(linked.Token).ConfigureAwait(true);
                    continue;
                }

                var frame = await Clock.NextFrameAsync(linked.Token).ConfigureAwait(true);
                linked.Token.ThrowIfCancellationRequested();
                if (!_disposed) OnFrame(frame);
            }
        }
        finally
        {
            _loopActive = false;
            Execute(() =>
            {
                _lastFrameTime = AnimationConstants.UnspecifiedTime;
                _startTime = AnimationConstants.UnspecifiedTime;
                _playTime = TimeSpan.Zero;
                SetRunning(false);
                Notify(nameof(PlayTime));
            });
        }
    }

    internal void OnFrame(TimeSpan frameTime)
    {
        Mutate(() =>
        {
            if (_disposed) return;
            if (!AnimationConstants.IsUnspecified(_lastFrameTime) && frameTime < _lastFrameTime)
                throw new ArgumentOutOfRangeException(nameof(frameTime));
            _lastFrameTime = frameTime;

            if (AnimationConstants.IsUnspecified(_startTime) || _appliedDurationScale != EffectiveDurationScale)
            {
                _startTime = frameTime;
                foreach (var animation in _animations) animation.Reset();
                _appliedDurationScale = EffectiveDurationScale;
            }

            if (EffectiveDurationScale == 0)
            {
                SkipToEndAll();
                return;
            }

            _playTime = TimeSpan.FromTicks(
                checked((long)Math.Round((frameTime - _startTime).Ticks / EffectiveDurationScale)));
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

    internal void VerifyAccess()
    {
        Clock.VerifyAccess();
        _scopeLifetime?.VerifyActive(this);
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    internal void Mutate(Action action)
    {
        VerifyAccess();
        Execute(() => { if (!_disposed && _scopeLifetime?.IsEnding != true) action(); });
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
        Clock.VerifyAccess();
        if (_disposed) return;
        Execute(DisposeCore);
    }

    private void DisposeCore()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        foreach (var animation in _animations) animation.Detach();
        _animations.Clear();
        SetRunning(false);
        _playTime = TimeSpan.Zero;
        _lastFrameTime = AnimationConstants.UnspecifiedTime;
        _startTime = AnimationConstants.UnspecifiedTime;
        _changed.TrySetResult();
        _lifetime.Dispose();
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
            _owner.Clock.VerifyAccess();
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
