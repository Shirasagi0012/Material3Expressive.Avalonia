using Avalonia.Threading;

namespace ComposeAnimation.Runtime;

/// <summary>
/// One clock-driven run. Tasks describe its final result, never individual frames.
/// Stop requests made inside a sample take effect before another sample, but cleanup
/// and completion wait for the current callback to leave the run's critical section.
/// </summary>
internal sealed class ClockAnimation
{
    private readonly FrameScope _scope;
    private readonly CancellationToken _stopToken;
    private readonly Func<AnimationEndReason> _stopReason;
    private readonly Action<TimeSpan, ClockAnimation> _sample;
    private readonly Action<AnimationEndReason> _cleanup;
    private readonly Action<AnimationEndReason, Exception?>? _onCompleted;
    private readonly FrameClock.Registration _frames;
    private readonly TaskCompletionSource<AnimationEndReason> _completion = new();
    private readonly ExecutionContext? _executionContext = ExecutionContext.Capture();
    private TimeSpan _frame;
    private CancellationTokenRegistration _lifetimeRegistration, _stopRegistration;
    private AnimationEndReason? _ending;
    private bool _sampling, _ended;

    internal ClockAnimation(FrameScope scope, CancellationToken stopToken,
        Func<AnimationEndReason> stopReason, Action<TimeSpan, ClockAnimation> sample,
        Action<AnimationEndReason> cleanup, Action<AnimationEndReason, Exception?>? onCompleted = null)
    {
        _scope = scope;
        _stopToken = stopToken;
        _stopReason = stopReason;
        _sample = sample;
        _cleanup = cleanup;
        _onCompleted = onCompleted;
        _frames = scope.RegisterFrames(Sample, error => End(AnimationEndReason.Interrupted, error));
    }

    internal Task<AnimationEndReason> Completion => _completion.Task;
    internal bool IsStopping => _ending.HasValue || _ended || _stopToken.IsCancellationRequested || _scope.IsEnding;

    internal void Start(bool requestFrame = true, TimeSpan? initialFrame = null, Action? initialize = null)
    {
        _lifetimeRegistration = _scope.Lifetime.Register(RequestStop);
        if (_ended) { _lifetimeRegistration.Dispose(); return; }
        _stopRegistration = _stopToken.Register(RequestStop);
        if (_ended) { _stopRegistration.Dispose(); return; }
        if (_scope.IsEnding || _stopToken.IsCancellationRequested) { RequestStop(); return; }
        // Initialization can notify users and reenter stopping just like a frame sample.
        // Keep it in the same critical section and failure/cleanup path.
        if (initialize is not null)
        {
            if (_executionContext is null) SampleCore(initialize);
            else ExecutionContext.Run(_executionContext, static state =>
            {
                var (run, initialize) = ((ClockAnimation, Action))state!;
                run.SampleCore(initialize);
            }, (this, initialize));
            if (IsStopping) return;
        }
        if (initialFrame is { } frame) Sample(frame);
        else if (requestFrame) RequestFrame();
    }

    internal void RequestFrame()
    {
        if (IsStopping) { RequestStop(); return; }
        _frames.Request();
    }

    internal void Finish(AnimationEndReason reason = AnimationEndReason.Finished)
    {
        if (_ended) return;
        _ending ??= reason;
        if (!_sampling) End(_ending.Value, null);
    }

    private void RequestStop()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RequestStop, FrameClock.ResumePriority);
            return;
        }
        if (_ended) return;
        Finish(_scope.IsEnding || _scope.Lifetime.IsCancellationRequested
            ? AnimationEndReason.ScopeEnded : _stopReason());
    }

    private void Sample(TimeSpan frame)
    {
        _frame = frame;
        if (_executionContext is null) SampleCore();
        else ExecutionContext.Run(_executionContext, static state => ((ClockAnimation)state!).SampleCore(), this);
    }

    private void SampleCore(Action? initialize = null)
    {
        if (IsStopping) { RequestStop(); return; }
        Exception? failure = null;
        _sampling = true;
        try
        {
            if (initialize is not null) initialize();
            else _sample(_frame, this);
        }
        catch (Exception error) { failure = error; }
        finally { _sampling = false; }
        if (failure is not null) End(AnimationEndReason.Interrupted, failure);
        else if (_ending is { } reason) End(reason, null);
        else if (IsStopping) RequestStop();
    }

    private void End(AnimationEndReason reason, Exception? failure)
    {
        if (_ended) return;
        _ended = true;
        _ending = reason;
        _frames.Dispose();
        _lifetimeRegistration.Dispose();
        _stopRegistration.Dispose();
        try
        {
            if (_executionContext is null) _cleanup(reason);
            else ExecutionContext.Run(_executionContext, static state =>
            {
                var run = (ClockAnimation)state!;
                run._cleanup(run._ending!.Value);
            }, this);
        }
        catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
        if (failure is null) _completion.TrySetResult(reason);
        else _completion.TrySetException(failure);
        _onCompleted?.Invoke(reason, failure);
    }
}
