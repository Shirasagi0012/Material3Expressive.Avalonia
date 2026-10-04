using CommunityToolkit.Mvvm.ComponentModel;
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

    public Task ShowAsync(MutatePriority priority = MutatePriority.Default, CancellationToken cancellationToken = default) =>
        _mutatorMutex.MutateAsync(async ct =>
        {
            using var job = CancellationTokenSource.CreateLinkedTokenSource(ct);
            if (!IsPersistent && priority != MutatePriority.UserInput)
                job.CancelAfter(TooltipDefaults.TooltipDuration);
            _job = job;
            try
            {
                Transition.TargetState = true;
                await Task.Delay(Timeout.InfiniteTimeSpan, job.Token);
                return true;
            }
            finally
            {
                if (ReferenceEquals(_job, job)) _job = null;
                // Preserve the fixed baseline: focus/long-press control their own dismissal.
                if (priority != MutatePriority.PreventUserInput) Dismiss();
            }
        }, priority, cancellationToken);

    public void Dismiss()
    {
        Transition.TargetState = false;
        if (IsPersistent) _job?.Cancel();
    }

    /// <summary>Cancels this state's show operation when its TooltipBox leaves the visual tree.</summary>
    public void OnDispose() => _job?.Cancel();
    public void Dispose() => OnDispose();
}
