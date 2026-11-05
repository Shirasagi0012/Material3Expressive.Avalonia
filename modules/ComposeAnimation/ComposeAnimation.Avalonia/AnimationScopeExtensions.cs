using PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs;
using Avalonia;
using Avalonia.Data;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

public static class AnimationScopeExtensions
{
    /// <summary>Schedules one effect after the frame's animation samples have been captured.
    /// Disposing the handle or ending the scope cancels an uncommitted effect.</summary>
    /// <remarks>Calls between frames wait for the next frame; calls during sampling join that
    /// frame. Writes from a commit are captured in the next batch. This does not wait for rendering.</remarks>
    public static IDisposable ScheduleFrameCommit(this AnimationScope scope, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        scope.VerifyActive();
        return new ScheduledFrameCommit(scope, action);
    }

    /// <summary>Binds an animation value at Animation priority, publishing coalesced samples
    /// at frame end before observing writes made by that batch's effects.</summary>
    /// <remarks>The initial value is available immediately. Subsequent values are captured
    /// before any scheduled effect executes. The projection must be a pure conversion.
    /// Use BindAnimatedValue when the consumer reads the animation directly during drawing.</remarks>
    public static IDisposable BindFrameCommittedValue<TSource, TTarget>(this AnimationScope scope,
        AvaloniaObject target, StyledProperty<TTarget> property, IAnimationValue<TSource> source,
        Func<TSource, TTarget> project)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(project);
        scope.VerifyActive();
        return new FrameCommittedBinding<TSource, TTarget>(scope, target, property, source, project);
    }

    /// <summary>Binds samples at frame end with an identity projection.</summary>
    public static IDisposable BindFrameCommittedValue<T>(this AnimationScope scope,
        AvaloniaObject target, StyledProperty<T> property, IAnimationValue<T> source)
        => BindFrameCommittedValue(scope, target, property, source, static value => value);

    public static IDisposable SubscribeValue<T>(this AnimationScope scope,
        IAnimationValue<T> source, Action<T> observer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(observer);
        scope.VerifyActive();
        var active = true;
        void Changed(object? sender, PropertyChangedEventArgs args)
        {
            if (!active || scope.IsEnding) return;
            scope.VerifyActive();
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IAnimationValue<T>.Value))
                observer(source.Value);
        }
        var subscription = scope.RegisterResource(() => { active = false; source.PropertyChanged -= Changed; });
        source.PropertyChanged += Changed;
        try { observer(source.Value); }
        catch { subscription.Dispose(); throw; }
        return subscription;
    }

    public static IDisposable BindAnimatedValue<T>(this AnimationScope scope, AvaloniaObject target,
        StyledProperty<T> property, IAnimationValue<T> source)
        => BindAnimatedValue(scope, target, property, source, static value => value);

    /// <summary>Projects sampled animation values into an Avalonia property at Animation priority.</summary>
    /// <remarks>The projection must be a pure conversion. It does not create another animation or
    /// change sampling time. Disposing the binding or ending the scope restores underlying values.</remarks>
    public static IDisposable BindAnimatedValue<TSource, TTarget>(this AnimationScope scope, AvaloniaObject target,
        StyledProperty<TTarget> property, IAnimationValue<TSource> source, Func<TSource, TTarget> project)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(project);
        scope.VerifyActive();
        var samples = new AnimationSampleObservable<TTarget>(project(source.Value));
        IDisposable? contribution = null;
        IDisposable? subscription = null;
        var resource = scope.RegisterResource(() =>
        {
            try { subscription?.Dispose(); }
            finally { contribution?.Dispose(); }
        }, binding: true);
        try
        {
            contribution = target.Bind(property, samples, BindingPriority.Animation);
            if (scope.IsEnding) { contribution.Dispose(); return resource; }
            subscription = scope.SubscribeValue(source, value => samples.Publish(project(value)));
            if (scope.IsEnding) { subscription.Dispose(); contribution.Dispose(); }
            return resource;
        }
        catch { resource.Dispose(); throw; }
    }
}
