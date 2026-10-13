using CommunityToolkit.Mvvm.ComponentModel;

namespace ComposeAnimation.Runtime;

/// <summary>
/// Value holder that automatically animates its value when changed via
/// <see cref="AnimateToAsync"/>, adapted from AndroidX Animatable (Apache-2.0). If a new
/// animation is started while one is in flight, the ongoing one is canceled and the new one
/// continues from the current value and velocity, so the value change is always continuous.
/// Unlike <see cref="AnimationState{T}"/>, <see cref="Animatable{T}"/> guarantees mutual
/// exclusion of its animations via an internal <see cref="MutatorMutex"/>.
///
/// State only (D19): an <see cref="Animatable{T}"/> holds no clock, lifetime or scope. Each run
/// names the <see cref="FrameScope"/> it runs in, which supplies frames, lifetime and duration
/// scale. One run at a time (D21): a run in any scope cancels the current one, which leaves value
/// and velocity where it stopped (D22). Ending a scope cancels its runs; the state stays usable.
///
/// Threading: the thread that constructs the animatable owns it (in an app, the UI thread); every
/// member checks it. Runs additionally check their scope (UI thread, not ended). State changes raise
/// <see cref="ObservableObject.PropertyChanged"/> on that thread, so the value can be consumed
/// by Avalonia bindings directly.
///
/// Notifications are not suppressed when a property is assigned an equal value. Animation frames
/// can hold a converted <typeparamref name="T"/> (delay, integer/color rounding) while the
/// underlying velocity vector still changes; <c>SetProperty</c> would drop those ticks.
/// </summary>
public sealed class Animatable<T> : ObservableObject, IAnimationValue<T>
{
    private readonly ITwoWayConverter<T> _converter;
    private readonly ThreadAffinity _owner = ThreadAffinity.Current;
    private readonly AnimationState<T> _internalState;
    private readonly MutatorMutex _mutatorMutex = new();
    private readonly SpringSpec<T> _defaultSpringSpec;
    private readonly AnimationVector _negativeInfinityBounds;
    private readonly AnimationVector _positiveInfinityBounds;
    private AnimationVector _lowerBoundVector;
    private AnimationVector _upperBoundVector;
    private T _targetValue;
    private bool _hasLowerBound;
    private bool _hasUpperBound;

    public Animatable(
        ITwoWayConverter<T> typeConverter,
        T initialValue,
        T? visibilityThreshold = default,
        string? label = null
    )
    {
        ArgumentNullException.ThrowIfNull(typeConverter);
        _converter = typeConverter;
        _internalState = new AnimationState<T>(typeConverter, initialValue);
        _targetValue = initialValue;
        _defaultSpringSpec = new SpringSpec<T>(visibilityThreshold: visibilityThreshold);
        Label = label ?? "Animatable";
        _negativeInfinityBounds = CreateInfinityBounds(typeConverter.Dimension, double.NegativeInfinity);
        _positiveInfinityBounds = CreateInfinityBounds(typeConverter.Dimension, double.PositiveInfinity);
        _lowerBoundVector = _negativeInfinityBounds;
        _upperBoundVector = _positiveInfinityBounds;
    }

    /// <summary>An optional label for differentiating this animation from others in tooling.</summary>
    public string Label { get; }

    /// <summary>Current value of the animation.</summary>
    public T Value => _internalState.Value;

    /// <summary>Current velocity of the animation, converted from the velocity vector.</summary>
    public T Velocity => _internalState.Velocity;

    /// <summary>Indicates whether any animation is currently running.</summary>
    public bool IsRunning => _internalState.IsRunning;

    /// <summary>
    /// The target of the current animation. If the animation finishes un-interrupted, it will
    /// reach this value.
    /// </summary>
    public T TargetValue
    {
        get => _targetValue;
        private set
        {
            _targetValue = value;
            OnPropertyChanged(nameof(TargetValue));
        }
    }

    /// <summary>
    /// Lower bound of the animation, null by default (meaning no lower bound). Bounds can be
    /// changed using <see cref="UpdateBounds"/>. The animation stops as soon as *any* dimension
    /// specified in the lower bound is reached.
    /// </summary>
    public T? LowerBound
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(LowerBound));
        }
    }

    /// <summary>
    /// Upper bound of the animation, null by default (meaning no upper bound). Bounds can be
    /// changed using <see cref="UpdateBounds"/>. The animation stops as soon as *any* dimension
    /// specified in the upper bound is reached.
    /// </summary>
    public T? UpperBound
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(UpperBound));
        }
    }

    /// <summary>A snapshot copy of the internal animation state.</summary>
    public AnimationState<T> State => _internalState.Copy();

    /// <summary>
    /// Starts an animation from <see cref="Value"/> to <paramref name="targetValue"/>. If an
    /// animation is already in flight, it is canceled and the new animation continues from the
    /// current value and velocity. When no
    /// <paramref name="animationSpec"/> is specified, a spring is used.
    ///
    /// If the animation gets interrupted (by another animation, <see cref="Stop"/>, or
    /// <see cref="SnapToAsync"/>), the returned task completes as canceled. Once the animation
    /// ends, its velocity is reset to 0; the state at the point of interruption is captured in
    /// the returned <see cref="AnimationResult{T}"/>.
    /// </summary>
    public Task<AnimationResult<T>> AnimateToAsync(
        FrameScope scope,
        T targetValue,
        IAnimationSpec<T>? animationSpec = null,
        Action<Animatable<T>>? block = null,
        MutatePriority priority = MutatePriority.Default,
        CancellationToken cancellationToken = default
    )
    {
        VerifyRun(scope);
        var initialVelocity = AnimationVector.From(_internalState.VelocityVector);
        var animation = new TargetBasedAnimation<T>(
            animationSpec ?? _defaultSpringSpec,
            _converter,
            Value,
            targetValue,
            initialVelocityVector: initialVelocity);
        return scope.Track(() => RunAnimationAsync(scope, animation, initialVelocity, block, priority, cancellationToken));
    }

    /// <summary>Starts an animation with an explicitly supplied initial velocity.</summary>
    public Task<AnimationResult<T>> AnimateToAsync(
        FrameScope scope,
        T targetValue,
        IAnimationSpec<T>? animationSpec,
        T initialVelocity,
        Action<Animatable<T>>? block = null,
        MutatePriority priority = MutatePriority.Default,
        CancellationToken cancellationToken = default
    )
    {
        VerifyRun(scope);
        var initialVelocityVector = new AnimationVector(_converter.Dimension);
        _converter.To(initialVelocity, initialVelocityVector.AsSpan());
        var anim = new TargetBasedAnimation<T>(
            animationSpec ?? _defaultSpringSpec,
            _converter,
            Value,
            targetValue,
            initialVelocity);
        return scope.Track(() => RunAnimationAsync(scope, anim, initialVelocityVector, block, priority, cancellationToken));
    }

    /// <summary>
    /// Starts a decay animation that slows down from <paramref name="initialVelocity"/>, starting
    /// at the current <see cref="Value"/>, until the velocity reaches 0. Often used after a fling
    /// gesture. Cancellation semantics match <see cref="AnimateToAsync"/>.
    /// </summary>
    public Task<AnimationResult<T>> AnimateDecayAsync(
        FrameScope scope,
        T initialVelocity,
        IDecayAnimationSpec<T> animationSpec,
        Action<Animatable<T>>? block = null,
        MutatePriority priority = MutatePriority.Default,
        CancellationToken cancellationToken = default
    )
    {
        VerifyRun(scope);
        ArgumentNullException.ThrowIfNull(animationSpec);
        var initialVelocityVector = new AnimationVector(_converter.Dimension);
        _converter.To(initialVelocity, initialVelocityVector.AsSpan());
        var anim = new DecayAnimation<T>(animationSpec, _converter, Value, initialVelocity);
        return scope.Track(() => RunAnimationAsync(scope, anim, initialVelocityVector, block, priority, cancellationToken));
    }

    // All the different types of animation code paths eventually converge to this method.
    private async Task<AnimationResult<T>> RunAnimationAsync(
        FrameScope scope,
        Animation<T> animation,
        AnimationVector initialVelocity,
        Action<Animatable<T>>? block,
        MutatePriority priority,
        CancellationToken cancellationToken
    )
    {
        // Capture the start time before the mutex is entered: when this animation interrupts
        // another one, it re-anchors onto the interrupted animation's last frame time.
        var startTime = _internalState.LastFrameTime;
        return await _mutatorMutex.MutateAsync(
            mutationToken => RunAnimationCoreAsync(scope, animation, initialVelocity, block, startTime, cancellationToken, mutationToken),
            priority,
            cancellationToken);
    }

    private async Task<AnimationResult<T>> RunAnimationCoreAsync(
        FrameScope scope,
        Animation<T> animation,
        AnimationVector initialVelocity,
        Action<Animatable<T>>? block,
        TimeSpan startTime,
        CancellationToken cancellationToken,
        CancellationToken mutexToken
    )
    {
        // The run's one token: caller, scope lifetime and mutex (a newer run in any scope).
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scope.Lifetime, mutexToken);
        var mutationToken = linked.Token;
        try
        {
            mutationToken.ThrowIfCancellationRequested();
            if (scope.IsEnding) throw new OperationCanceledException(mutationToken);
            initialVelocity.AsReadOnlySpan().CopyTo(_internalState.MutableVelocityVector);
            TargetValue = animation.TargetValue;
            mutationToken.ThrowIfCancellationRequested();
            _internalState.IsRunning = true;
            OnPropertyChanged(nameof(IsRunning));

            var endState = _internalState.Copy();
            endState.FinishedTime = AnimationConstants.UnspecifiedTime;
            var clampingNeeded = false;

            await endState.AnimateAsync(animation, scope, startTime,
                run =>
                {
                    mutationToken.ThrowIfCancellationRequested();
                    if (scope.IsEnding) throw new OperationCanceledException(mutationToken);
                    _internalState.UpdateFrom(run);
                    var value = run.Value;
                    var clamped = ClampToBounds(value);
                    if (!EqualityComparer<T>.Default.Equals(clamped, value))
                    {
                        _internalState.Value = clamped;
                        endState.Value = clamped;
                        OnFrameChanged();
                        mutationToken.ThrowIfCancellationRequested();
                        block?.Invoke(this);
                        run.CancelAnimation();
                        clampingNeeded = true;
                    }
                    else
                    {
                        OnFrameChanged();
                        mutationToken.ThrowIfCancellationRequested();
                        block?.Invoke(this);
                    }
                },
                mutationToken).ConfigureAwait(true);

            return new AnimationResult<T>(endState,
                clampingNeeded ? AnimationEndReason.BoundReached : AnimationEndReason.Finished);
        }
        finally
        {
            EndAnimation();
        }
    }

    /// <summary>
    /// Sets the current value to <paramref name="targetValue"/> immediately, canceling any
    /// on-going animation. If bounds are set, the value is clamped to them.
    /// </summary>
    public Task SnapToAsync(T targetValue, CancellationToken cancellationToken = default)
    {
        VerifyAccess();
        return SnapCoreAsync(targetValue, cancellationToken);
    }

    private async Task SnapCoreAsync(T targetValue, CancellationToken cancellationToken)
    {
        await _mutatorMutex.MutateAsync(
            mutationToken =>
            {
                mutationToken.ThrowIfCancellationRequested();
                EndAnimation();
                mutationToken.ThrowIfCancellationRequested();
                var clampedValue = ClampToBounds(targetValue);
                _internalState.Value = clampedValue;
                TargetValue = clampedValue;
                OnPropertyChanged(nameof(Value));
                return Task.FromResult(true);
            }, MutatePriority.Default, cancellationToken);
    }

    private void VerifyRun(FrameScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        scope.VerifyAccess();
        _owner.Verify(this);
    }

    private void VerifyAccess() => _owner.Verify(this);

    /// <summary>
    /// Stops any on-going animation in its track, without skipping to the target value. Returns
    /// immediately; the canceled animation's task completes as canceled once it has unwound.
    /// Consider <see cref="SnapToAsync"/> if the value should also jump to a given value.
    ///
    /// Deviation from upstream: this is a cancellation signal rather than a mutation, so it is
    /// safe to call from within an animation frame callback.
    /// </summary>
    public void Stop()
    {
        VerifyAccess();
        _mutatorMutex.CancelCurrent();
    }

    /// <summary>
    /// Cancels the on-going animation like <see cref="Stop"/> and waits until it has unwound.
    /// Must not be called from within an animation frame callback (it would await itself).
    /// </summary>
    public async Task StopAsync()
    {
        VerifyAccess();
        var task = _mutatorMutex.CurrentCompletion;
        _mutatorMutex.CancelCurrent();
        if (task is not null)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    /// <summary>
    /// Updates the lower and/or upper bound. A null bound means no bound on that side. If an
    /// animation is running, the new bounds apply from the next frame and may end it with
    /// <see cref="AnimationEndReason.BoundReached"/>; otherwise the current value is clamped
    /// immediately.
    /// </summary>
    public void UpdateBounds(AnimatableBounds<T> bounds)
    {
        VerifyAccess();
        var lowerVector = bounds.Lower.HasValue ? ToVector(bounds.Lower.Value) : _negativeInfinityBounds;
        var upperVector = bounds.Upper.HasValue ? ToVector(bounds.Upper.Value) : _positiveInfinityBounds;

        for (var i = 0; i < lowerVector.Dimension; i++)
        {
            if (lowerVector[i] > upperVector[i])
            {
                throw new ArgumentException(
                    "Lower bound must be no greater than upper bound on all dimensions. " +
                    $"The provided lower bound exceeds the upper bound on index {i}.");
            }
        }

        _lowerBoundVector = lowerVector;
        _upperBoundVector = upperVector;
        _hasLowerBound = bounds.Lower.HasValue;
        _hasUpperBound = bounds.Upper.HasValue;
        LowerBound = bounds.Lower.HasValue ? bounds.Lower.Value : default;
        UpperBound = bounds.Upper.HasValue ? bounds.Upper.Value : default;

        if (!IsRunning)
        {
            var clampedValue = ClampToBounds(Value);
            if (EqualityComparer<T>.Default.Equals(clampedValue, Value)) return;
            _internalState.Value = clampedValue;
            OnPropertyChanged(nameof(Value));
        }
    }

    public readonly record struct AnimatableBounds<T>(AnimatableBounds<T>.Bound<T> Lower, AnimatableBounds<T>.Bound<T> Upper)
    {
        public static AnimatableBounds<T> Unbounded => default;
        public AnimatableBounds<T> WithLower(T lower) => this with { Lower = lower };
        public AnimatableBounds<T> WithUpper(T upper) => this with { Upper = upper };
        public AnimatableBounds<T> WithoutLower() => this with { Lower = Bound<T>.Unbounded };
        public AnimatableBounds<T> WithoutUpper() => this with { Upper = Bound<T>.Unbounded };

        public readonly record struct Bound<T>(T Value)
        {
            public static Bound<T> Unbounded => default;
            public bool HasValue { get; } = true;

            public T Value
            {
                get => HasValue
                    ? field
                    : throw new InvalidOperationException("Bound is not set.");
            } = Value;

            public static implicit operator Bound<T>(T value) => new(value);
        }
    }

    private static AnimationVector CreateInfinityBounds(int dimension, double value)
    {
        var bounds = new AnimationVector(dimension);
        bounds.AsSpan().Fill(value);
        return bounds;
    }

    private AnimationVector ToVector(T value)
    {
        var vector = new AnimationVector(_converter.Dimension);
        _converter.To(value, vector.AsSpan());
        return vector;
    }

    private T ClampToBounds(T value)
    {
        if (ReferenceEquals(_lowerBoundVector, _negativeInfinityBounds) &&
            ReferenceEquals(_upperBoundVector, _positiveInfinityBounds))
        {
            // Expect this to be the most common use case
            return value;
        }

        var vector = ToVector(value);
        var clamped = false;
        for (var i = 0; i < vector.Dimension; i++)
        {
            var component = vector[i];
            if (component < _lowerBoundVector[i] || component > _upperBoundVector[i])
            {
                clamped = true;
                vector[i] = Math.Min(Math.Max(component, _lowerBoundVector[i]), _upperBoundVector[i]);
            }
        }

        return clamped ? _converter.From(vector.AsReadOnlySpan()) : value;
    }

    private void EndAnimation()
    {
        _internalState.ResetAfterRun();
        // Only an in-flight interruption shares the previous frame's timebase (captured
        // before entering the mutex). A later run must start on its own first frame.
        _internalState.LastFrameTime = AnimationConstants.UnspecifiedTime;
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(Velocity));
    }

    private void OnFrameChanged()
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(Velocity));
    }
}

/// <summary>
/// Information about an animation at the point it ended: <see cref="EndState"/> captures
/// value/velocity/frame time of the last frame, and <see cref="EndReason"/> describes why the
/// animation ended.
/// </summary>
public readonly record struct AnimationResult<T>(
    AnimationState<T> EndState,
    AnimationEndReason EndReason
);
