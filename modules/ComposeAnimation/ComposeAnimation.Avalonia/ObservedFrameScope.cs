using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// A pass-through view of a scope that observes frame-source failures for a component (former
/// ObservedClock / FailureObservedClock). Not an async layer: the wait is the inner scope's own,
/// so runs keep resuming inline in the batch. Observers run innermost first: this one before the
/// caller's own <c>onFault</c>, both before the continuation.
/// </summary>
internal sealed class ObservedFrameScope(FrameScope inner, Action<Exception, CancellationToken> observe) : FrameScope
{
    /// <summary>Observes like the former component clocks: any failure except the caller's own cancellation.</summary>
    internal static ObservedFrameScope Faulting(FrameScope inner)
    {
        ObservedFrameScope? scope = null;
        scope = new ObservedFrameScope(inner, (error, token) =>
        {
            if (error is not OperationCanceledException || !token.IsCancellationRequested) scope!.Faulted = true;
        });
        return scope;
    }

    public FrameScope Inner => inner;
    public bool Faulted { get; private set; }

    public override CancellationToken Lifetime => inner.Lifetime;

    public override double DurationScale
    {
        get => inner.DurationScale;
        set => inner.DurationScale = value;
    }

    internal override ValueTask<TimeSpan> WaitFrameAsync(CancellationToken cancellationToken, Action<Exception>? onFault)
        => inner.WaitFrameAsync(cancellationToken, error =>
        {
            observe(error, cancellationToken);
            onFault?.Invoke(error);
        });

    internal override double EffectiveDurationScale => inner.EffectiveDurationScale;
    internal override void VerifyAccess() => inner.VerifyAccess();
    internal override bool IsEnding => inner.IsEnding;
    internal override Task<T> Track<T>(Func<Task<T>> run) => inner.Track(run);
    internal override Task Track(Func<Task> run) => inner.Track(run);
    internal override void ReportRunFailure(Exception error) => inner.ReportRunFailure(error);
    internal override void RegisterPlaybackChanged<T>(T owner, Action<T> changed) => inner.RegisterPlaybackChanged(owner, changed);
}
