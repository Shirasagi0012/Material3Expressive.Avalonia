using System.ComponentModel;

namespace ComposeAnimation.Runtime;

/// <summary>A read-only animation sample with notifications on its owning thread.</summary>
public interface IAnimationValue<out T> : INotifyPropertyChanged
{
    T Value { get; }
}

// Runtime knows only this contract, never an Avalonia scope or visual.
internal interface IAnimationLifetimeClock
{
    AnimationLifetime? AnimationLifetime { get; }
    double DurationScale { get; }
}

internal sealed class AnimationLifetime
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly HashSet<Task> _operations = [];
    private readonly List<(WeakReference<object> Owner, Action<object> End)> _end = [];
    private readonly List<(WeakReference<object> Owner, Action<object> Changed)> _playback = [];
    internal bool IsEnding { get; private set; }
    internal AnimationLifetime() => Token = _cancellation.Token;
    internal CancellationToken Token { get; }
    internal int PendingCount => _operations.Count;
    internal Action<Exception>? UnmanagedFailure { get; set; }

    internal void VerifyActive(object owner) => ObjectDisposedException.ThrowIf(IsEnding, owner);
    internal void Register<T>(T owner, Action<T> end) where T : class
    {
        VerifyActive(this);
        _end.RemoveAll(entry => !entry.Owner.TryGetTarget(out _));
        _end.Add((new WeakReference<object>(owner), target => end((T)target)));
    }
    internal void MarkEnding() => IsEnding = true;
    internal void RegisterPlayback<T>(T owner, Action<T> changed) where T : class
    {
        _playback.RemoveAll(entry => !entry.Owner.TryGetTarget(out _));
        _playback.Add((new WeakReference<object>(owner), target => changed((T)target)));
    }
    internal void PlaybackChanged()
    {
        if (IsEnding) return;
        foreach (var entry in _playback.ToArray())
            if (entry.Owner.TryGetTarget(out var owner)) entry.Changed(owner);
    }
    internal void Cancel()
    {
        List<Exception>? errors = null;
        try { _cancellation.Cancel(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        var actions = _end.ToArray();
        _end.Clear();
        _playback.Clear();
        foreach (var entry in actions)
            if (entry.Owner.TryGetTarget(out var owner))
                try { entry.End(owner); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException(errors);
    }

    internal Task<T> Track<T>(Func<Task<T>> operation)
    {
        Task<T>? task = null;
        Track((Func<Task>)(() => task = operation()));
        return task!;
    }

    internal Task Track(Func<Task> operation)
    {
        VerifyActive(this);
        var entry = new TaskCompletionSource();
        _operations.Add(entry.Task); // Before synchronous callbacks can dispose the scope.
        Task task;
        try { task = operation(); }
        catch { _operations.Remove(entry.Task); entry.TrySetResult(); throw; }
        _ = ObserveAsync(task, entry);
        return task;
    }

    private async Task ObserveAsync(Task task, TaskCompletionSource entry)
    {
        try { await task.ConfigureAwait(true); }
        catch
        {
            if (task.IsFaulted)
                foreach (var error in task.Exception!.InnerExceptions) UnmanagedFailure?.Invoke(error);
        }
        finally { _operations.Remove(entry.Task); entry.TrySetResult(); }
    }
    internal async Task DrainAsync()
    {
        while (_operations.Count > 0)
            try { await Task.WhenAll(_operations.ToArray()).ConfigureAwait(true); } catch { }
        _cancellation.Dispose();
    }
}
