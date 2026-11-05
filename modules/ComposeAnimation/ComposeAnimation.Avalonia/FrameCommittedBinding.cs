using Avalonia;
using Avalonia.Data;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

// Shares AnimatedState's root queue: every value is captured before any effect in
// the batch executes. A write made by an effect belongs to the following batch.
internal sealed class FrameCommittedBinding<TSource, TTarget> : IAnimatedStateCommit, IDisposable
{
    private readonly AnimationScope _scope;
    private readonly AnimatedStateCoordinator _coordinator;
    private readonly IAnimationValue<TSource> _source;
    private readonly Func<TSource, TTarget> _project;
    private readonly AnimationSampleObservable<TTarget> _samples;
    private IDisposable? _subscription, _contribution, _resource;
    private bool _disposed;

    internal FrameCommittedBinding(AnimationScope scope, AvaloniaObject target,
        StyledProperty<TTarget> property, IAnimationValue<TSource> source, Func<TSource, TTarget> project)
    {
        _scope = scope;
        _source = source;
        _project = project;
        _coordinator = AnimatedStateCoordinator.Get(scope, AnimationCommitPhase.AfterFrame);
        _samples = new(project(source.Value));
        _resource = scope.RegisterResource(Dispose, binding: true);
        try
        {
            _contribution = target.Bind(property, _samples, BindingPriority.Animation);
            if (_disposed) { _contribution.Dispose(); return; }
            var initial = true;
            _subscription = scope.SubscribeValue(source, _ =>
            {
                if (!initial) _coordinator.Schedule(this);
                initial = false;
            });
            if (_disposed) _subscription.Dispose();
        }
        catch { Dispose(); throw; }
    }

    public Action CaptureCommit()
    {
        // Project at capture time too: an effect may change both source and its context.
        TTarget value;
        try { value = _project(_source.Value); }
        catch (Exception error)
        {
            // Reject only this contribution, after the queue has finished enumerating.
            // A user projection must not discard unrelated bindings/effects in this frame.
            return () => RejectCommitFrame(error, report: true);
        }
        return () =>
        {
            if (_disposed) return;
            try { _samples.Publish(value); }
            catch (Exception error) { RejectCommitFrame(error, report: true); }
        };
    }

    public void RejectCommitFrame(Exception error, bool report)
    {
        Dispose();
        if (report) _scope.ReportFailure(error, "Frame-committed binding");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _coordinator.Remove(this);
        try { _subscription?.Dispose(); }
        finally
        {
            try { _contribution?.Dispose(); }
            finally { _resource?.Dispose(); }
        }
    }
}

internal sealed class ScheduledFrameCommit : IAnimatedStateCommit, IDisposable
{
    private readonly AnimationScope _scope;
    private readonly AnimatedStateCoordinator _coordinator;
    private readonly Action _action;
    private IDisposable? _resource;
    private bool _disposed;

    internal ScheduledFrameCommit(AnimationScope scope, Action action)
    {
        _scope = scope;
        _action = action;
        _coordinator = AnimatedStateCoordinator.Get(scope, AnimationCommitPhase.AfterFrame);
        _resource = scope.RegisterResource(Dispose);
        _coordinator.Schedule(this);
    }

    public Action CaptureCommit() => () =>
    {
        if (_disposed) return;
        Dispose();
        try { _action(); }
        catch (Exception error) { _scope.ReportFailure(error, "Frame commit"); }
    };

    public void RejectCommitFrame(Exception error, bool report)
    {
        Dispose();
        if (report) _scope.ReportFailure(error, "Frame commit");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _coordinator.Remove(this);
        _resource?.Dispose();
    }
}
