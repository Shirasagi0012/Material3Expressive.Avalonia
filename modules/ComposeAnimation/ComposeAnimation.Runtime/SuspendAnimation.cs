namespace ComposeAnimation.Runtime;

/// <summary>
/// Frame-loop driver and convenience animation entry points, adapted from AndroidX
/// SuspendAnimation.kt (Apache-2.0).
///
/// These APIs run animations <b>without any mutual exclusion</b>: starting a second animation on
/// the same <see cref="AnimationState{T}"/> while one is in flight will interleave their frame
/// writes. If you need interruption safety, use <see cref="Animatable{T}"/> instead.
/// </summary>
public static class SuspendAnimation
{
    /// <summary>
    /// Animates the given <paramref name="state"/> from its current value to
    /// <paramref name="targetValue"/>, updating the state with the up-to-date value/velocity on
    /// every frame. The state's current velocity vector is used as the initial velocity at full
    /// precision. When no <paramref name="animationSpec"/> is specified, a spring is used.
    /// </summary>
    /// <param name="state">The state holder to animate.</param>
    /// <param name="targetValue">The value the animation runs to.</param>
    /// <param name="scope">The scope the animation runs in: it supplies frames, lifetime and duration scale.</param>
    /// <param name="animationSpec">The spec that defines the animation curve. A spring by default.</param>
    /// <param name="sequentialAnimation">
    /// Whether to use the state's <see cref="AnimationState{T}.LastFrameTime"/> as the start time
    /// (true), or start on the next frame (false, the default). When an on-going animation is
    /// interrupted and a new one is started to carry over the momentum, starting on the
    /// interruption time creates a smoother animation.
    /// </param>
    /// <param name="block">
    /// Invoked on every frame with the <see cref="AnimationRun{T}"/>; call
    /// <see cref="AnimationRun{T}.CancelAnimation"/> from it to end the animation early.
    /// </param>
    public static Task<AnimationEndReason> AnimateToAsync<T>(
        FrameScope scope,
        AnimationState<T> state,
        T targetValue,
        IAnimationSpec<T>? animationSpec = null,
        bool sequentialAnimation = false,
        Action<AnimationRun<T>>? block = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(scope);
        var anim = new TargetBasedAnimation<T>(
            animationSpec ?? new SpringSpec<T>(),
            state.TypeConverter,
            state.Value,
            targetValue,
            // Pass the velocity vector through untouched, preserving precision for integer and
            // color converters.
            initialVelocityVector: AnimationVector.From(state.VelocityVector));
        return state.AnimateAsync(
            anim,
            scope,
            sequentialAnimation ? state.LastFrameTime : AnimationConstants.UnspecifiedTime,
            block,
            cancellationToken);
    }

    /// <summary>
    /// Decay animation that slows down from the current velocity and value captured in
    /// <paramref name="state"/> until the velocity reaches 0, updating the state on every frame.
    /// Often used to animate the result of a fling gesture.
    /// </summary>
    /// <param name="state">The state holder to animate.</param>
    /// <param name="animationSpec">
    /// Defines the decay animation, such as <see cref="DecayAnimationSpec.ExponentialDecay{T}"/>.
    /// </param>
    /// <param name="scope">The scope the animation runs in: it supplies frames, lifetime and duration scale.</param>
    /// <param name="sequentialAnimation">
    /// Whether to use the state's <see cref="AnimationState{T}.LastFrameTime"/> as the start time
    /// (true), or start on the next frame (false, the default).
    /// </param>
    /// <param name="block">
    /// Invoked on every frame with the <see cref="AnimationRun{T}"/>; call
    /// <see cref="AnimationRun{T}.CancelAnimation"/> from it to end the animation early.
    /// </param>
    public static Task<AnimationEndReason> AnimateDecayAsync<T>(
        FrameScope scope,
        AnimationState<T> state,
        IDecayAnimationSpec<T> animationSpec,
        bool sequentialAnimation = false,
        Action<AnimationRun<T>>? block = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(animationSpec);
        ArgumentNullException.ThrowIfNull(scope);
        var anim = new DecayAnimation<T>(
            animationSpec,
            state.TypeConverter,
            state.Value,
            initialVelocityVector: AnimationVector.From(state.VelocityVector));
        return state.AnimateAsync(
            anim,
            scope,
            sequentialAnimation ? state.LastFrameTime : AnimationConstants.UnspecifiedTime,
            block,
            cancellationToken);
    }

    /// <summary>
    /// One-shot target based animation for any type <typeparamref name="T"/>: animates from
    /// <paramref name="initialValue"/> to <paramref name="targetValue"/>, reporting the up-to-date
    /// value and velocity to <paramref name="block"/> on every frame. When no
    /// <paramref name="animationSpec"/> is specified, a spring is used. If there's a need to
    /// access the animation state (start time, target, etc.), use
    /// <see cref="AnimateToAsync{T}(FrameScope, AnimationState{T}, T, IAnimationSpec{T}?, bool, Action{AnimationRun{T}}?, CancellationToken)"/>
    /// instead.
    /// </summary>
    /// <param name="scope">The scope the animation runs in: it supplies frames, lifetime and duration scale.</param>
    /// <param name="typeConverter">Converts <typeparamref name="T"/> from and to <see cref="AnimationVector"/>.</param>
    /// <param name="initialValue">The value the animation starts from.</param>
    /// <param name="targetValue">The value the animation runs to.</param>
    /// <param name="initialVelocity">The initial velocity; 0 by default.</param>
    /// <param name="animationSpec">The spec that defines the animation curve. A spring by default.</param>
    /// <param name="block">Invoked on every frame with the current value and velocity.</param>
    public static Task<AnimationEndReason> AnimateAsync<T>(
        FrameScope scope,
        ITwoWayConverter<T> typeConverter,
        T initialValue,
        T targetValue,
        T? initialVelocity = default,
        IAnimationSpec<T>? animationSpec = null,
        Action<T, T>? block = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(typeConverter);
        var initialVelocityVector = new AnimationVector(typeConverter.Dimension);
        if (initialVelocity is not null)
            typeConverter.To(initialVelocity, initialVelocityVector.AsSpan());
        var anim = new TargetBasedAnimation<T>(
            animationSpec ?? new SpringSpec<T>(),
            typeConverter,
            initialValue,
            targetValue,
            initialVelocityVector: initialVelocityVector);
        var state = new AnimationState<T>(typeConverter, initialValue, initialVelocityVector);
        return state.AnimateAsync(
            anim,
            scope,
            startTime: AnimationConstants.UnspecifiedTime,
            block is null ? null : run => block(run.Value, run.Velocity),
            cancellationToken);
    }

    /// <summary>
    /// One-shot animation for <see cref="double"/> values: animates from
    /// <paramref name="initialValue"/> to <paramref name="targetValue"/>, reporting the up-to-date
    /// value and velocity to <paramref name="block"/> on every frame.
    /// </summary>
    /// <param name="scope">The scope the animation runs in: it supplies frames, lifetime and duration scale.</param>
    /// <param name="initialValue">The value the animation starts from.</param>
    /// <param name="targetValue">The value the animation runs to.</param>
    /// <param name="initialVelocity">The initial velocity; 0 by default.</param>
    /// <param name="animationSpec">The spec that defines the animation curve. A spring by default.</param>
    /// <param name="block">Invoked on every frame with the current value and velocity.</param>
    public static Task<AnimationEndReason> AnimateAsync(
        FrameScope scope,
        double initialValue,
        double targetValue,
        double initialVelocity = 0,
        IAnimationSpec<double>? animationSpec = null,
        Action<double, double>? block = null,
        CancellationToken cancellationToken = default)
        => AnimateAsync(scope, new DoubleConverter(), initialValue, targetValue, initialVelocity,
            animationSpec, block, cancellationToken);

    /// <summary>
    /// One-shot decay animation for <see cref="double"/> values: slows down from
    /// <paramref name="initialVelocity"/> starting at <paramref name="initialValue"/> until the
    /// velocity reaches 0. Often used after a fling gesture.
    /// </summary>
    /// <param name="scope">The scope the animation runs in: it supplies frames, lifetime and duration scale.</param>
    /// <param name="initialValue">The value the animation starts from.</param>
    /// <param name="initialVelocity">The initial velocity of the animation.</param>
    /// <param name="animationSpec">
    /// Defines the decay animation, such as <see cref="DecayAnimationSpec.ExponentialDecay{T}"/>.
    /// </param>
    /// <param name="block">Invoked on every frame with the current value and velocity.</param>
    public static Task<AnimationEndReason> AnimateDecayAsync(
        FrameScope scope,
        double initialValue,
        double initialVelocity,
        IDecayAnimationSpec<double> animationSpec,
        Action<double, double>? block = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(animationSpec);
        var converter = new DoubleConverter();
        var initialVelocityVector = new AnimationVector(1);
        initialVelocityVector[0] = initialVelocity;
        var anim = new DecayAnimation<double>(animationSpec, converter, initialValue, initialVelocity);
        var state = new AnimationState<double>(converter, initialValue, initialVelocityVector);
        return state.AnimateAsync(
            anim,
            scope,
            startTime: AnimationConstants.UnspecifiedTime,
            block is null ? null : run => block(run.Value, run.Velocity),
            cancellationToken);
    }

    /// <summary>Samples on the shared clock; the task completes only after the run stops.</summary>
    internal static Task<AnimationEndReason> AnimateAsync<T>(
        this AnimationState<T> state, Animation<T> animation, FrameScope scope,
        TimeSpan startTime, Action<AnimationRun<T>>? onFrame, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(scope);
        scope.VerifyAccess();
        AnimationRun<T>? run = null;
        var clock = new PlayTimeAccumulator();
        var driver = new ClockAnimation(scope, cancellationToken, () => AnimationEndReason.Canceled,
            (frame, active) =>
            {
                run ??= CreateRun(state, animation, frame);
                var durationScale = scope.EffectiveDurationScale;
                var accumulated = clock.Advance(frame, durationScale);
                var playTime = durationScale == 0 ? (animation.IsInfinite ? TimeSpan.Zero : animation.Duration) : accumulated;
                DoAnimationFrame(state, animation, run, frame, playTime, onFrame);
                if (!run.IsRunning) active.Finish();
                else active.RequestFrame();
            },
            reason =>
            {
                if (run is null || reason == AnimationEndReason.Finished) return;
                run.IsRunning = false;
                if (run.LastFrameTime == state.LastFrameTime) state.IsRunning = false;
            });
        driver.Start(initialFrame: AnimationConstants.IsUnspecified(startTime) ? null : startTime);
        return driver.Completion;
    }

    private static AnimationRun<T> CreateRun<T>(
        AnimationState<T> state,
        Animation<T> animation,
        TimeSpan startTime)
    {
        var initialVelocity = new AnimationVector(state.TypeConverter.Dimension);
        animation.GetVelocityVector(TimeSpan.Zero, initialVelocity.AsSpan());
        return new AnimationRun<T>(
            state.TypeConverter,
            animation.GetValue(TimeSpan.Zero),
            initialVelocity.AsReadOnlySpan(),
            animation.TargetValue,
            startTime,
            onCancel: () => state.IsRunning = false);
    }

    // Impl detail, invoked every frame; mirrors AnimationScope.doAnimationFrame upstream.
    private static void DoAnimationFrame<T>(
        AnimationState<T> state,
        Animation<T> animation,
        AnimationRun<T> scope,
        TimeSpan frameTime,
        TimeSpan playTime,
        Action<AnimationRun<T>>? onFrame)
    {
        scope.LastFrameTime = frameTime;
        scope.Value = animation.GetValue(playTime);
        animation.GetVelocityVector(playTime, scope.MutableVelocityVector);
        if (animation.IsFinished(playTime))
        {
            // TODO: end time isn't necessarily last frame time (matches upstream)
            scope.FinishedTime = frameTime;
            scope.IsRunning = false;
        }
        state.UpdateFrom(scope);
        onFrame?.Invoke(scope);
    }
}
