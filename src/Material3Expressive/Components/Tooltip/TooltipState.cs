using CommunityToolkit.Mvvm.ComponentModel;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

/// <summary>Visibility and priority arbitration for one TooltipBox.</summary>
/// <remarks>
/// ShowAsync runs until timeout, interruption, cancellation or persistent dismissal. It does not
/// complete when the tooltip first appears. Use a distinct state for each TooltipBox. The direct
/// constructor defaults to persistent, like Compose TooltipState; a TooltipBox's implicit state
/// defaults to nonpersistent, like rememberTooltipState.
/// </remarks>
public sealed class TooltipState : ObservableObject, IDisposable
{
    private readonly MutatorMutex _mutatorMutex;
    private CancellationTokenSource? _job;
    // A mounted Box supplies its existing coroutine lifetime/time source. A state used before
    // mounting still supports ShowAsync with ordinary system time, as it did previously.
    internal AnimationScope? DelayScope { get; set; }

    public TooltipState(bool initialIsVisible = false, bool isPersistent = true, MutatorMutex? mutatorMutex = null)
    {
        Transition = new(initialIsVisible);
        IsPersistent = isPersistent;
        _mutatorMutex = mutatorMutex ?? TooltipDefaults.GlobalMutatorMutex;
        Transition.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(Transition.CurrentState) or nameof(Transition.TargetState))
                OnPropertyChanged(nameof(IsVisible));
        };
    }

    public MutableTransitionState<bool> Transition { get; }
    public bool IsVisible => Transition.CurrentState || Transition.TargetState;
    public bool IsPersistent { get; }

    public Task<bool> ShowAsync(MutatePriority priority = MutatePriority.Default, CancellationToken cancellationToken = default) =>
        _mutatorMutex.MutateAsync(async ct =>
        {
            using var job = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var duration = !IsPersistent && priority != MutatePriority.UserInput
                ? TooltipDefaults.TooltipDuration : Timeout.InfiniteTimeSpan;
            _job = job;
            try
            {
                Transition.TargetState = true;
                if (DelayScope is { } scope)
                    await scope.DelayAsync(duration, job.Token);
                else
                {
                    // Unmounted states use the same nonthrowing stop contract, without a frame clock.
                    var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    using var timer = TimeProvider.System.CreateTimer(_ => done.TrySetResult(), null,
                        duration, Timeout.InfiniteTimeSpan);
                    using var registration = job.Token.Register(() => done.TrySetResult());
                    await done.Task;
                }
                // Upstream timeout is a stop, with the same dismissal in finally.
                return false;
            }
            finally
            {
                if (ReferenceEquals(_job, job)) _job = null;
                // Preserve the fixed baseline: focus/long-press control their own dismissal.
                if (priority != MutatePriority.PreventUserInput) Dismiss();
            }
        }, priority, cancellationToken, interruptedResult: () => false);

    public void Dismiss()
    {
        Transition.TargetState = false;
        if (IsPersistent) _job?.Cancel();
    }

    /// <summary>Cancels this state's show operation when its TooltipBox leaves the visual tree.</summary>
    public void OnDispose() => _job?.Cancel();
    public void Dispose() => OnDispose();
}
