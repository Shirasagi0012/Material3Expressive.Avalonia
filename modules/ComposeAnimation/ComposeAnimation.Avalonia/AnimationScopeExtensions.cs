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
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(source);
        scope.VerifyActive();
        var samples = new AnimationSampleObservable<T>(source.Value);
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
            subscription = scope.SubscribeValue(source, samples.Publish);
            if (scope.IsEnding) { subscription.Dispose(); contribution.Dispose(); }
            return resource;
        }
        catch { resource.Dispose(); throw; }
    }
}
