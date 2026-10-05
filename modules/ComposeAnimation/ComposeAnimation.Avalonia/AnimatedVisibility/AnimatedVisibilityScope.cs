using System.ComponentModel;
using Avalonia;
using Avalonia.Data;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

public enum EnterExitState { PreEnter, Visible, PostExit }

public sealed class VisibilityScopeEventArgs(AnimatedVisibilityScope scope) : EventArgs
{
    public AnimatedVisibilityScope Scope { get; } = scope;
}

/// <summary>
/// One presentation's animations and bindings. The host owns its lifetime; do not run or dispose
/// its Transition. Independent animations do not participate in exit completion.
/// </summary>
public sealed class AnimatedVisibilityScope
{
    private readonly List<IDisposable> _resources = [];
    private readonly HashSet<VisibilityMotion> _motions = [];
    private readonly TransitionPreparationLease? _gate;
    private bool _measured;
    internal bool IsDisposed { get; private set; }
    public Transition<EnterExitState> Transition { get; }

    internal AnimatedVisibilityScope(Transition<EnterExitState> transition)
    {
        Transition = transition;
        // A scope created by a manual seek is a preview and must not acquire preparation.
        _gate = transition.IsSeeking ? null : transition.BeginPreparation();
    }

    /// <summary>
    /// Reserves this presentation before its visibility target changes. Register the shared
    /// animation before releasing; dispose on failure/cancellation. The scope owns cleanup on
    /// detach. Manual seeking revokes the lease, so delayed callbacks must check IsActive.
    /// </summary>
    public TransitionPreparationLease BeginPreparation()
    {
        Transition.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return Transition.BeginPreparation();
    }

    /// <summary>
    /// Projects a value on an Animation-priority binding without replacing its ordinary binding.
    /// Use OneWay presentation bindings: TwoWay bindings can write samples back to their source.
    /// Disposing the returned lease releases only the binding, not the registered animation.
    /// </summary>
    public IDisposable Bind<T>(AvaloniaObject target, StyledProperty<T> property,
        Transition<EnterExitState>.TransitionAnimationState<T> animation)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(animation);
        Transition.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (animation.Owner != Transition)
            throw new ArgumentException("The animation must belong to this scope's Transition.", nameof(animation));
        var lease = target.Bind(property, new AnimationObservable<T>(animation), BindingPriority.Animation);
        return Own(lease);
    }

    /// <summary>Releases a view subscription or other resource when this presentation ends.</summary>
    public IDisposable Own(IDisposable resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        Transition.VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var lease = new ResourceLease(_resources, resource);
        _resources.Add(lease);
        return lease;
    }

    internal void Register(VisibilityMotion motion) => _motions.Add(motion);
    internal void Unregister(VisibilityMotion motion) => _motions.Remove(motion);
    internal void Measured()
    {
        if (!_measured)
        {
            _measured = true;
            _gate?.Dispose();
        }
    }

    internal bool IsReady => _measured;

    internal void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        List<Exception>? errors = null;
        void Release(Action release)
        {
            try { release(); }
            catch (Exception error) { (errors ??= []).Add(error); }
        }
        foreach (var motion in _motions.ToArray()) Release(motion.Dispose);
        foreach (var resource in _resources.ToArray()) Release(resource.Dispose);
        _motions.Clear();
        _resources.Clear();
        Release(Transition.Dispose);
        if (errors is not null) throw new AggregateException("Visibility scope cleanup failed.", errors);
    }

    private sealed class ResourceLease(List<IDisposable> owner, IDisposable resource) : IDisposable
    {
        private IDisposable? _resource = resource;
        public void Dispose()
        {
            var resource = Interlocked.Exchange(ref _resource, null);
            if (resource is null) return;
            owner.Remove(this);
            resource.Dispose();
        }
    }

    private sealed class AnimationObservable<T>(Transition<EnterExitState>.TransitionAnimationState<T> animation) : IObservable<T>
    {
        public IDisposable Subscribe(IObserver<T> observer)
        {
            void Changed(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(animation.Value)) observer.OnNext(animation.Value);
            }
            animation.PropertyChanged += Changed;
            try { observer.OnNext(animation.Value); }
            catch { animation.PropertyChanged -= Changed; throw; }
            return new ActionDisposable(() => animation.PropertyChanged -= Changed);
        }
    }
}

internal sealed class ActionDisposable(Action action) : IDisposable
{
    private Action? _action = action;
    public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
}
