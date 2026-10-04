// Translated from the fixed AndroidX SnackbarHost.kt (Apache-2.0).
using CommunityToolkit.Mvvm.ComponentModel;

namespace Material3Expressive.Components;

public enum SnackbarResult { Dismissed, ActionPerformed }
public enum SnackbarDuration { Short, Long, Indefinite }

public interface ISnackbarVisuals
{
    string Message { get; }
    string? ActionLabel { get; }
    bool WithDismissAction { get; }
    SnackbarDuration Duration { get; }
}

public interface ISnackbarData
{
    ISnackbarVisuals Visuals { get; }
    void PerformAction();
    void Dismiss();
}

/// <summary>A fair queue and the current data. The host owns display timeouts and animations.</summary>
/// <remarks>ShowSnackbarAsync completes on action/dismissal, before the host's exit animation
/// finishes. Without a host, requests wait for an explicit result or caller cancellation.</remarks>
public sealed class SnackbarHostState : ObservableObject
{
    private readonly Lock _gate = new();
    private readonly LinkedList<Waiter> _waiters = new();
    private bool _held;

    public ISnackbarData? CurrentSnackbarData
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    }

    public Task<SnackbarResult> ShowSnackbarAsync(string message, string? actionLabel = null,
        bool withDismissAction = false, SnackbarDuration? duration = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return ShowSnackbarAsync(new Visuals(message, actionLabel, withDismissAction,
            duration ?? (actionLabel is null ? SnackbarDuration.Short : SnackbarDuration.Indefinite)), cancellationToken);
    }

    public async Task<SnackbarResult> ShowSnackbarAsync(ISnackbarVisuals visuals, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visuals);
        await AcquireAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = new Data(visuals);
            using var registration = cancellationToken.Register(() => data.Completion.TrySetCanceled(cancellationToken));
            CurrentSnackbarData = data;
            return await data.Completion.Task.ConfigureAwait(true);
        }
        finally
        {
            CurrentSnackbarData = null;
            Release();
        }
    }

    // SemaphoreSlim does not guarantee FIFO, and MutatorMutex interrupts instead of queuing.
    // Keep only the fair cancellable mutex needed by SnackbarHostState, not a second public API.
    private Task AcquireAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Waiter waiter;
        lock (_gate)
        {
            if (!_held) { _held = true; return Task.CompletedTask; }
            waiter = new();
            waiter.Node = _waiters.AddLast(waiter);
        }
        waiter.Registration = token.Register(() =>
        {
            lock (_gate)
            {
                if (waiter.Node.List is null) return; // Already granted; the show body checks its token.
                _waiters.Remove(waiter.Node);
            }
            waiter.Completion.TrySetCanceled(token);
        });
        return WaitAsync();

        async Task WaitAsync()
        {
            try { await waiter.Completion.Task.ConfigureAwait(true); }
            finally { waiter.Registration.Dispose(); }
        }
    }

    private void Release()
    {
        Waiter? next;
        lock (_gate)
        {
            next = _waiters.First?.Value;
            if (next is null) _held = false;
            else _waiters.RemoveFirst();
        }
        next?.Completion.TrySetResult();
    }

    private sealed class Waiter
    {
        public readonly TaskCompletionSource Completion = new();
        public LinkedListNode<Waiter> Node = null!;
        public CancellationTokenRegistration Registration;
    }

    private sealed record Visuals(string Message, string? ActionLabel, bool WithDismissAction,
        SnackbarDuration Duration) : ISnackbarVisuals;

    private sealed class Data(ISnackbarVisuals visuals) : ISnackbarData
    {
        public ISnackbarVisuals Visuals { get; } = visuals;
        public readonly TaskCompletionSource<SnackbarResult> Completion = new();
        public void PerformAction() => Completion.TrySetResult(SnackbarResult.ActionPerformed);
        public void Dismiss() => Completion.TrySetResult(SnackbarResult.Dismissed);
    }
}
