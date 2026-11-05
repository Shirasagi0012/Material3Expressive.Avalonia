using PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs;
using Avalonia;
using Avalonia.Data;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

public static class AnimationScopeExtensions
{
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
