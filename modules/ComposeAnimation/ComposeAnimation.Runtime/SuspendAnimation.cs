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
    /// <param name="clock">The frame clock driving the animation.</param>
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
    public static Task AnimateToAsync<T>(
        this AnimationState<T> state,
        T targetValue,
        IAnimationFrameClock clock,
        IAnimationSpec<T>? animationSpec = null,
        bool sequentialAnimation = false,
        Action<AnimationRun<T>>? block = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(clock);
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
            clock,
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
    /// <param name="clock">The frame clock driving the animation.</param>
    /// <param name="sequentialAnimation">
    /// Whether to use the state's <see cref="AnimationState{T}.LastFrameTime"/> as the start time
    /// (true), or start on the next frame (false, the default).
    /// </param>
    /// <param name="block">
    /// Invoked on every frame with the <see cref="AnimationRun{T}"/>; call
    /// <see cref="AnimationRun{T}.CancelAnimation"/> from it to end the animation early.
    /// </param>
    public static Task AnimateDecayAsync<T>(
        this AnimationState<T> state,
        IDecayAnimationSpec<T> animationSpec,
        IAnimationFrameClock clock,
        bool sequentialAnimation = false,
        Action<AnimationRun<T>>? block = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(animationSpec);
        ArgumentNullException.ThrowIfNull(clock);
        var anim = new DecayAnimation<T>(
            animationSpec,
            state.TypeConverter,
            state.Value,
            initialVelocityVector: AnimationVector.From(state.VelocityVector));
        return state.AnimateAsync(
            anim,
            clock,
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
    /// <see cref="AnimateToAsync{T}(AnimationState{T}, T, IAnimationFrameClock, IAnimationSpec{T}?, bool, Action{AnimationRun{T}}?, CancellationToken)"/>
    /// instead.
    /// </summary>
    /// <param name="clock">The frame clock driving the animation.</param>
    /// <param name="typeConverter">Converts <typeparamref name="T"/> from and to <see cref="AnimationVector"/>.</param>
    /// <param name="initialValue">The value the animation starts from.</param>
    /// <param name="targetValue">The value the animation runs to.</param>
    /// <param name="initialVelocity">The initial velocity; 0 by default.</param>
    /// <param name="animationSpec">The spec that defines the animation curve. A spring by default.</param>
    /// <param name="block">Invoked on every frame with the current value and velocity.</param>
    public static Task AnimateAsync<T>(
        this IAnimationFrameClock clock,
        ITwoWayConverter<T> typeConverter,
        T initialValue,
        T targetValue,
        T? initialVelocity = default,
        IAnimationSpec<T>? animationSpec = null,
        Action<T, T>? block = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clock);
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
            clock,
            startTime: AnimationConstants.UnspecifiedTime,
            block is null ? null : scope => block(scope.Value, scope.Velocity),
            cancellationToken);
    }

    /// <summary>
    /// One-shot animation for <see cref="double"/> values: animates from
    /// <paramref name="initialValue"/> to <paramref name="targetValue"/>, reporting the up-to-date
    /// value and velocity to <paramref name="block"/> on every frame.
    /// </summary>
    /// <param name="clock">The frame clock driving the animation.</param>
    /// <param name="initialValue">The value the animation starts from.</param>
    /// <param name="targetValue">The value the animation runs to.</param>
    /// <param name="initialVelocity">The initial velocity; 0 by default.</param>
    /// <param name="animationSpec">The spec that defines the animation curve. A spring by default.</param>
    /// <param name="block">Invoked on every frame with the current value and velocity.</param>
    public static Task AnimateAsync(
        this IAnimationFrameClock clock,
        double initialValue,
        double targetValue,
        double initialVelocity = 0,
        IAnimationSpec<double>? animationSpec = null,
        Action<double, double>? block = null,
        CancellationToken cancellationToken = default)
        => clock.AnimateAsync(new DoubleConverter(), initialValue, targetValue, initialVelocity,
            animationSpec, block, cancellationToken);

    /// <summary>
    /// One-shot decay animation for <see cref="double"/> values: slows down from
    /// <paramref name="initialVelocity"/> starting at <paramref name="initialValue"/> until the
    /// velocity reaches 0. Often used after a fling gesture.
    /// </summary>
    /// <param name="clock">The frame clock driving the animation.</param>
    /// <param name="initialValue">The value the animation starts from.</param>
    /// <param name="initialVelocity">The initial velocity of the animation.</param>
    /// <param name="animationSpec">
    /// Defines the decay animation, such as <see cref="DecayAnimationSpec.ExponentialDecay{T}"/>.
    /// </param>
    /// <param name="block">Invoked on every frame with the current value and velocity.</param>
    public static Task AnimateDecayAsync(
        this IAnimationFrameClock clock,
        double initialValue,
        double initialVelocity,
        IDecayAnimationSpec<double> animationSpec,
        Action<double, double>? block = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(animationSpec);
        var converter = new DoubleConverter();
        var initialVelocityVector = new AnimationVector(1);
        initialVelocityVector[0] = initialVelocity;
        var anim = new DecayAnimation<double>(animationSpec, converter, initialValue, initialVelocity);
        var state = new AnimationState<double>(converter, initialValue, initialVelocityVector);
        return state.AnimateAsync(
            anim,
            clock,
            startTime: AnimationConstants.UnspecifiedTime,
            block is null ? null : scope => block(scope.Value, scope.Velocity),
            cancellationToken);
    }

    /// <summary>
    /// Runs the given <paramref name="animation"/> against <paramref name="state"/> from start to
    /// finish by awaiting one frame at a time from the clock, so cancellation unwinds the loop via
    /// <see cref="OperationCanceledException"/> thrown at the frame await. Mirrors the internal
    /// animate function upstream.
    /// </summary>
    internal static async Task AnimateAsync<T>(
        this AnimationState<T> state,
        Animation<T> animation,
        IAnimationFrameClock clock,
        TimeSpan startTime,
        Action<AnimationRun<T>>? onFrame,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(clock);
        clock.VerifyAccess();

        AnimationRun<T>? scope = null;
        var controlled = (clock as IAnimationLifetimeClock)?.AnimationLifetime is not null;
        TimeSpan? previousFrame = null;
        var accumulated = TimeSpan.Zero;
        void Sample(TimeSpan frame, double durationScale)
        {
            if (!controlled)
            {
                DoAnimationFrameWithScale(state, animation, scope!, frame, durationScale, onFrame);
                return;
            }
            durationScale = ((IAnimationLifetimeClock)clock).DurationScale;
            if (durationScale > 0 && previousFrame is { } previous)
                accumulated += TimeSpan.FromTicks(checked((long)Math.Round(Math.Max(0, (frame - previous).Ticks) / durationScale)));
            previousFrame = frame;
            var playTime = durationScale == 0 ? (animation.IsInfinite ? TimeSpan.Zero : animation.Duration) : accumulated;
            DoAnimationFrame(state, animation, scope!, frame, playTime, onFrame);
        }
        try
        {
            if (!AnimationConstants.IsUnspecified(startTime))
            {
                // A start time is supplied when this animation continues an interrupted one:
                // process the first frame immediately on the previous animation's timebase.
                scope = CreateScope(state, animation, startTime);
                Sample(startTime, MotionDurationScale.ScaleFactor);
            }
            else
            {
                var firstFrame = await clock.NextFrameAsync(cancellationToken).ConfigureAwait(true);
                cancellationToken.ThrowIfCancellationRequested();
                scope = CreateScope(state, animation, firstFrame);
                Sample(firstFrame, MotionDurationScale.ScaleFactor);
            }

            while (scope.IsRunning)
            {
                var durationScale = ((clock as IAnimationLifetimeClock)?.DurationScale ?? MotionDurationScale.ScaleFactor);
                var frameTime = await clock.NextFrameAsync(cancellationToken).ConfigureAwait(true);
                cancellationToken.ThrowIfCancellationRequested();
                Sample(frameTime, durationScale);
            }
        }
        catch (OperationCanceledException)
        {
            if (scope is not null)
            {
                scope.IsRunning = false;
                if (scope.LastFrameTime == state.LastFrameTime)
                {
                    // There hasn't been another animation.
                    state.IsRunning = false;
                }
            }
            throw;
        }
    }

    private static AnimationRun<T> CreateScope<T>(
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

    private static void DoAnimationFrameWithScale<T>(
        AnimationState<T> state,
        Animation<T> animation,
        AnimationRun<T> scope,
        TimeSpan frameTime,
        double durationScale,
        Action<AnimationRun<T>>? onFrame)
    {
        TimeSpan playTime;
        if (durationScale == 0)
        {
            // Snap finite animations to the end; infinite specs keep a zero play time (Compose).
            playTime = animation.IsInfinite ? TimeSpan.Zero : animation.Duration;
        }
        else
        {
            var delta = frameTime - scope.StartTime;
            if (delta < TimeSpan.Zero)
                delta = TimeSpan.Zero;
            playTime = TimeSpan.FromTicks(checked((long)Math.Round(delta.Ticks / durationScale)));
        }

        DoAnimationFrame(state, animation, scope, frameTime, playTime, onFrame);
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
