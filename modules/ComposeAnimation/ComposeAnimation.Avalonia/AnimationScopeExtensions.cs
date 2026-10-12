using PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs;
using Avalonia;
using Avalonia.Data;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

public static class AnimationScopeExtensions
{
    public static Animatable<T> CreateAnimatable<T>(this AnimationScope scope, T initialValue,
        ITwoWayConverter<T> converter, T? visibilityThreshold = default, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(scope);
        scope.VerifyActive();
        return new Animatable<T>(converter, initialValue, scope.Clock, visibilityThreshold, label);
    }

    public static Animatable<double> CreateAnimatable(this AnimationScope scope, double initialValue, string? label = null)
        => scope.CreateAnimatable(initialValue, new DoubleConverter(), VisibilityThresholds.Double, label);

    public static Animatable<float> CreateAnimatable(this AnimationScope scope, float initialValue, string? label = null)
        => scope.CreateAnimatable(initialValue, new FloatConverter(), VisibilityThresholds.Float, label);

    public static Animatable<int> CreateAnimatable(this AnimationScope scope, int initialValue, string? label = null)
        => scope.CreateAnimatable(initialValue, new Int32Converter(), VisibilityThresholds.Int32, label);

    public static Animatable<Point> CreateAnimatable(this AnimationScope scope, Point initialValue, string? label = null)
        => scope.CreateAnimatable(initialValue, new PointConverter(), VisibilityThresholds.Point, label);

    public static Animatable<Vector> CreateAnimatable(this AnimationScope scope, Vector initialValue, string? label = null)
        => scope.CreateAnimatable(initialValue, new VectorConverter(), VisibilityThresholds.Vector, label);

    public static Animatable<Size> CreateAnimatable(this AnimationScope scope, Size initialValue, string? label = null)
        => scope.CreateAnimatable(initialValue, new SizeConverter(), VisibilityThresholds.Size, label);

    public static Animatable<Rect> CreateAnimatable(this AnimationScope scope, Rect initialValue, string? label = null)
        => scope.CreateAnimatable(initialValue, new RectConverter(), VisibilityThresholds.Rect, label);

    public static Animatable<Thickness> CreateAnimatable(this AnimationScope scope, Thickness initialValue, string? label = null)
        => scope.CreateAnimatable(initialValue, new ThicknessConverter(), VisibilityThresholds.Thickness, label);

    public static Animatable<CornerRadius> CreateAnimatable(this AnimationScope scope, CornerRadius initialValue, string? label = null)
        => scope.CreateAnimatable(initialValue, new CornerRadiusConverter(), VisibilityThresholds.CornerRadius, label);

    public static Animatable<Avalonia.Media.Color> CreateAnimatable(this AnimationScope scope, Avalonia.Media.Color initialValue, string? label = null)
        => scope.CreateAnimatable(initialValue, new ColorConverter(), default, label);

    public static Transition<TState> CreateTransition<TState>(this AnimationScope scope,
        TState initialState, string? label = null)
    {
        scope.VerifyActive();
        return new Transition<TState>(initialState, scope.Clock, label);
    }

    public static Transition<TState> CreateTransition<TState>(this AnimationScope scope,
        TransitionState<TState> state, string? label = null)
    {
        scope.VerifyActive();
        return new Transition<TState>(state, scope.Clock, label);
    }

    public static InfiniteTransition CreateInfiniteTransition(this AnimationScope scope, string? label = null)
    {
        scope.VerifyActive();
        return new InfiniteTransition(scope.Clock, label);
    }

    // Concrete overloads keep inference from selecting TState = SeekableTransitionState<T>, etc.
    public static Transition<TState> CreateTransition<TState>(this AnimationScope scope,
        MutableTransitionState<TState> state, string? label = null)
        => scope.CreateTransition<TState>((TransitionState<TState>)state, label);

    public static Transition<TState> CreateTransition<TState>(this AnimationScope scope,
        SeekableTransitionState<TState> state, string? label = null)
        => scope.CreateTransition<TState>((TransitionState<TState>)state, label);

    public static Transition<TState> CreateTransition<TState>(this AnimationScope scope,
        DeferredTransitionState<TState> state, string? label = null)
        => scope.CreateTransition<TState>((TransitionState<TState>)state, label);

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
