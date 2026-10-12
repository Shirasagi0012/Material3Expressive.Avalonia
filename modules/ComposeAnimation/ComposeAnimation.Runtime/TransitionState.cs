using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComposeAnimation.Runtime;

public abstract class TransitionState<T> : ObservableObject
{
    private T _currentState;
    private T _targetState;
    private bool _isRunning;
    private int _updateDepth;
    private bool _changed;
    internal event Action? Changed;

    private protected TransitionState(T initialState)
    {
        _currentState = initialState;
        _targetState = initialState;
    }

    public T CurrentState
    {
        get => _currentState;
        internal set => SetProperty(ref _currentState, value);
    }

    public T TargetState
    {
        get => _targetState;
        internal set => SetProperty(ref _targetState, value);
    }

    internal bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    internal abstract void TransitionConfigured(Transition<T> transition);
    internal abstract void TransitionRemoved();

    protected void BeginUpdate() => _updateDepth++;

    protected void EndUpdate()
    {
        if (--_updateDepth == 0 && _changed)
        {
            _changed = false;
            Changed?.Invoke();
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_updateDepth > 0) _changed = true;
        else Changed?.Invoke();
    }
}

public sealed class MutableTransitionState<T>(T initialState) : TransitionState<T>(initialState)
{
    // C# cannot widen an overridden setter. Both properties use the base storage.
    public new T TargetState
    {
        get => base.TargetState;
        set => base.TargetState = value;
    }

    public bool IsIdle => EqualityComparer<T>.Default.Equals(CurrentState, TargetState) && !IsRunning;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(CurrentState) or nameof(TargetState) or nameof(IsRunning))
            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsIdle)));
    }

    internal override void TransitionConfigured(Transition<T> transition) { }
    internal override void TransitionRemoved() { }
}

public sealed class DeferredTransitionState<T>(T initialState) : TransitionState<T>(initialState)
{
    private T? _pendingTargetState;
    private bool _hasPendingTargetState;

    // Unconstrained T? is not Nullable<T> for value types; presence needs a separate flag.
    public T? PendingTargetState => _pendingTargetState;

    public bool HasPendingTargetState => _hasPendingTargetState;

    public void Defer(T targetState)
    {
        if (EqualityComparer<T>.Default.Equals(TargetState, targetState))
            SetPendingTarget(false, default);
        else
            SetPendingTarget(true, targetState);
    }

    public void AnimateTo(T targetState)
    {
        // A synchronous observer must see the whole update that Compose sees after recomposition.
        BeginUpdate();
        try
        {
            SetPendingTarget(false, default);
            TargetState = targetState;
        }
        finally { EndUpdate(); }
    }

    private void SetPendingTarget(bool hasValue, T? value)
    {
        var presenceChanged = _hasPendingTargetState != hasValue;
        var valueChanged = !EqualityComparer<T>.Default.Equals(_pendingTargetState, value);
        if (!presenceChanged && !valueChanged) return;

        _hasPendingTargetState = hasValue;
        _pendingTargetState = value;
        OnPropertyChanged(nameof(PendingTargetState));
        if (presenceChanged)
            OnPropertyChanged(nameof(HasPendingTargetState));
    }

    internal override void TransitionConfigured(Transition<T> transition) { }
    internal override void TransitionRemoved() { }
}

/// <summary>
/// A transition state whose progress can be controlled explicitly.  Seeking and animation are
/// serialized in the same way as Compose's <c>SeekableTransitionState</c>: a newer operation
/// cancels the operation currently waiting for frames, and the transition tree is sampled as one
/// unit on the clock that configured it.
/// </summary>
public sealed class SeekableTransitionState<T>(T initialState) : TransitionState<T>(initialState)
{
    private readonly MutatorMutex _mutatorMutex = new();
    private Transition<T>? _transition;
    private double _fraction;

    /// <summary>
    /// Progress from <see cref="CurrentState"/> to <see cref="TargetState"/>.  A completed
    /// operation resets this value to zero, matching the transition's idle representation.
    /// </summary>
    public double Fraction
    {
        get => _fraction;
        internal set => SetProperty(ref _fraction, value);
    }

    /// <summary>Immediately places the whole transition at <paramref name="targetState"/>.</summary>
    public Task SnapToAsync(T targetState, CancellationToken cancellationToken = default)
    {
        var transition = _transition;
        if (transition is null) return Task.CompletedTask;
        transition.VerifyAccess();

        if (EqualityComparer<T>.Default.Equals(CurrentState, targetState) &&
            EqualityComparer<T>.Default.Equals(TargetState, targetState) && !transition.IsPreparing)
            return Task.CompletedTask;

        return _mutatorMutex.MutateAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                transition.SetSeekableState(targetState, targetState, TimeSpan.Zero,
                    preserveCurrentValues: false);
                Fraction = 0;
                transition.FinishSeeking();
                return Task.FromResult(true);
            },
            MutatePriority.Default,
            cancellationToken);
    }

    public Task SeekToAsync(double fraction, CancellationToken cancellationToken = default)
        => SeekToAsync(TargetState, fraction, cancellationToken);

    /// <summary>
    /// Samples the transition at <paramref name="fraction"/>.  Unlike <see cref="SnapToAsync"/>,
    /// this leaves the transition in seeking mode so that subsequent calls can move freely in both
    /// directions without starting a normal frame loop.
    /// </summary>
    public Task SeekToAsync(T targetState, double fraction,
        CancellationToken cancellationToken = default)
    {
        ValidateFraction(fraction);
        var transition = _transition;
        if (transition is null) return Task.CompletedTask;
        transition.VerifyAccess();

        return _mutatorMutex.MutateAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();
                var oldTarget = TargetState;
                if (!EqualityComparer<T>.Default.Equals(targetState, oldTarget))
                {
                    // A target change starts a new segment at the former target, exactly as
                    // Compose does.  This also makes child transitions receive the new segment.
                    transition.SetSeekableState(oldTarget, targetState, TimeSpan.Zero);
                }
                else if (EqualityComparer<T>.Default.Equals(CurrentState, targetState))
                {
                    return Task.FromResult(true);
                }
                else
                {
                    transition.SetSeekingMode(true);
                }

                Fraction = fraction;
                transition.SeekToPlayTime(ToPlayTime(transition.TotalDuration, fraction));
                return Task.FromResult(true);
            },
            MutatePriority.Default,
            cancellationToken);
    }

    public Task AnimateToAsync(FrameScope scope, IFiniteAnimationSpec<double>? animationSpec = null,
        CancellationToken cancellationToken = default)
        => AnimateToAsync(scope, TargetState, animationSpec, cancellationToken);

    /// <summary>
    /// Animates the progress to the end of the current segment.  The optional animation spec
    /// controls progress itself; the transition's child animations are sampled at the resulting
    /// fraction on every frame. Frames come from <paramref name="scope"/>. Like any run of the
    /// transition, the seek run takes the transition's run mutex, so it cancels a running
    /// <see cref="Transition.RunAsync"/> (and vice versa) instead of racing it.
    /// </summary>
    public Task AnimateToAsync(FrameScope scope, T targetState, IFiniteAnimationSpec<double>? animationSpec = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var transition = _transition;
        if (transition is null) return Task.CompletedTask;
        transition.VerifyAccess();
        scope.VerifyAccess();

        // Lock order: the state mutex first, then the transition's run mutex (everywhere).
        return scope.Track(() => _mutatorMutex.MutateAsync(
            stateToken => transition.RunMutex.MutateAsync(
                runToken => AnimateCoreAsync(scope, transition, targetState, animationSpec, stateToken, runToken),
                MutatePriority.Default,
                stateToken),
            MutatePriority.Default,
            cancellationToken));
    }

    private async Task<bool> AnimateCoreAsync(FrameScope scope, Transition<T> transition, T targetState,
        IFiniteAnimationSpec<double>? animationSpec, CancellationToken stateToken, CancellationToken runToken)
    {
        // The run's one token: state mutex (caller token included), run mutex and scope lifetime.
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stateToken, runToken, scope.Lifetime);
        var cancellationToken = linked.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var oldTarget = TargetState;
        if (!EqualityComparer<T>.Default.Equals(targetState, oldTarget))
        {
            transition.SetSeekableState(oldTarget, targetState, TimeSpan.Zero);
            Fraction = 0;
        }
        else if (EqualityComparer<T>.Default.Equals(CurrentState, targetState))
        {
            return true;
        }
        else
        {
            transition.SetSeekingMode(true);
        }

        var totalDuration = transition.TotalDuration;
        var startFraction = Math.Clamp(Fraction, 0, 1);
        if (startFraction >= 1 || totalDuration <= TimeSpan.Zero || transition.DurationScale * scope.EffectiveDurationScale <= 0)
        {
            Fraction = 1;
            transition.SeekToPlayTime(totalDuration);
            Fraction = 0;
            transition.FinishSeeking();
            return true;
        }

        // With no explicit spec Compose traverses the transition timeline linearly and keeps
        // using the current total duration if composition adds a child while this is running.  An
        // explicit spec animates the fraction itself and has an independent duration.
        var animation = animationSpec is null
            ? null
            : new TargetBasedAnimation<double>(animationSpec, new DoubleConverter(), startFraction, 1d);

        transition.SetSeekingMode(true);
        var running = false;
        try
        {
            var firstFrame = await scope.WaitFrameAsync(cancellationToken, null);
            var previousFrame = firstFrame;
            var animationPlayTime = TimeSpan.Zero;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var frame = await scope.WaitFrameAsync(cancellationToken, null);
                
                if (frame < previousFrame)
                    throw new ArgumentOutOfRangeException(nameof(frame),
                        "Animation frame time moved backwards.");
                
                // The transition's own scale times the scope's, read per frame (D21).
                var durationScale = transition.DurationScale * scope.EffectiveDurationScale;
                var delta = frame - previousFrame;
                var playTime = durationScale <= 0
                    ? animation?.Duration ?? transition.TotalDuration
                    : animationPlayTime += TimeSpan.FromTicks(Math.Max(0,
                        (long)Math.Round(delta.Ticks / durationScale)));
                previousFrame = frame;
                var currentDuration = transition.TotalDuration;
                var progress = animation is null
                    ? currentDuration <= TimeSpan.Zero
                        ? 1d
                        : startFraction + (1d - startFraction) *
                            Math.Clamp((double)animationPlayTime.Ticks / currentDuration.Ticks, 0, 1)
                    : Math.Clamp(animation.GetValue(playTime), 0, 1);
                Fraction = progress;
                transition.SeekToPlayTime(ToPlayTime(transition.TotalDuration, progress));
                if (!running)
                {
                    transition.SetRunningMode(true);
                    running = true;
                }
                if (animation?.IsFinished(playTime) ?? progress >= 1) 
                    break;
            }
        }
        catch
        {
            if (running && !transition.IsDisposed) 
                transition.SetRunningMode(false);
            throw;
        }

        Fraction = 1;
        transition.SeekToPlayTime(transition.TotalDuration);
        
        Fraction = 0;
        transition.FinishSeeking();
        return true;
    }

    private static void ValidateFraction(double fraction)
    {
        if (!double.IsFinite(fraction) || fraction is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(fraction),
                "Fraction must be a finite value between 0 and 1.");
    }

    private static TimeSpan ToPlayTime(TimeSpan duration, double fraction)
    {
        if (duration <= TimeSpan.Zero || fraction <= 0) return TimeSpan.Zero;
        if (fraction >= 1) return duration;
        return TimeSpan.FromTicks((long)Math.Round(duration.Ticks * fraction));
    }

    internal override void TransitionConfigured(Transition<T> transition)
    {
        if (_transition is not null && !ReferenceEquals(_transition, transition))
            throw new InvalidOperationException(
                "An instance of SeekableTransitionState cannot be used by different Transitions.");
        _transition = transition;
    }

    internal override void TransitionRemoved()
    {
        _transition = null;
        _mutatorMutex.CancelCurrent();
    }
}
