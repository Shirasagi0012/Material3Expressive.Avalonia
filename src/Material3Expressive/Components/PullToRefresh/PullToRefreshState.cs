// PullToRefresh.kt / PullToRefreshStateImpl, pinned AndroidX baseline; Apache-2.0.
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

/// <summary>Hoisted pull distance, including overshoot beyond the refresh threshold.</summary>
/// <remarks>Animated operations require a mounted indicator. SnapToAsync also works before mounting.</remarks>
public class PullToRefreshState : AvaloniaObject, IPullToRefreshState
{
    public static readonly DirectProperty<PullToRefreshState, float> DistanceFractionProperty =
        AvaloniaProperty.RegisterDirect<PullToRefreshState, float>(nameof(DistanceFraction), state => state.DistanceFraction);
    public static readonly DirectProperty<PullToRefreshState, bool> IsAnimatingProperty =
        AvaloniaProperty.RegisterDirect<PullToRefreshState, bool>(nameof(IsAnimating), state => state.IsAnimating);

    private readonly Animatable<float> _animation;
    private readonly HashSet<Control> _owners = [];
    private AnimationChildScope? _scope;
    private TopLevel? _topLevel;
    private float _distanceFraction;
    private bool _isAnimating;

    public PullToRefreshState() : this(0) { }
    public PullToRefreshState(float initialDistanceFraction)
    {
        _distanceFraction = initialDistanceFraction;
        // The upstream constructor uses the generic Animatable factory, whose default
        // Float visibility threshold is .01. It does not clamp values or install bounds.
        _animation = new Animatable<float>(new FloatConverter(), initialDistanceFraction, .01f);
        _animation.PropertyChanged += AnimationChanged;
    }

    public float DistanceFraction => _distanceFraction;
    public new bool IsAnimating => _isAnimating;
    public virtual Task AnimateToThresholdAsync(CancellationToken cancellationToken = default) => AnimateToAsync(1, cancellationToken: cancellationToken);
    public virtual Task AnimateToHiddenAsync(CancellationToken cancellationToken = default) => AnimateToAsync(0, cancellationToken: cancellationToken);
    public virtual Task SnapToAsync(float distanceFraction, CancellationToken cancellationToken = default)
    {
        Dispatcher.UIThread.VerifyAccess();
        return _animation.SnapToAsync(distanceFraction, cancellationToken);
    }

    /// <summary>Animates the shared distance using a custom specification, preserving velocity, notifications and mounted-consumer lifetime.</summary>
    /// <remarks>Derived states can override the threshold/hidden operations to select their own motion.
    /// A null specification uses the same default spring as the standard state.</remarks>
    protected Task AnimateToAsync(float target, IFiniteAnimationSpec<float>? animationSpec = null,
        CancellationToken cancellationToken = default)
    {
        Dispatcher.UIThread.VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        var scope = _scope is { } active && !active.Lifetime.IsCancellationRequested ? active
            : throw new InvalidOperationException("Mount a pull-to-refresh indicator before animating its state. Use SnapToAsync to set an initial distance.");
        return _animation.AnimateToAsync(scope, target, animationSpec, cancellationToken: cancellationToken);
    }

    internal void Attach(Control owner)
    {
        var topLevel = TopLevel.GetTopLevel(owner) ?? throw new InvalidOperationException("The indicator must be mounted.");
        if (_topLevel is not null && !ReferenceEquals(_topLevel, topLevel))
            throw new InvalidOperationException("A PullToRefreshState cannot animate in multiple windows at the same time.");
        if (!_owners.Add(owner)) return;
        _topLevel = topLevel;
        _scope ??= AnimationScope.GetRoot(topLevel).CreateChild("Pull-to-refresh state");
    }

    internal void Detach(Control owner)
    {
        if (!_owners.Remove(owner) || _owners.Count != 0) return;
        _scope?.Dispose();
        _scope = null;
        _topLevel = null;
    }

    private void AnimationChanged(object? sender, PropertyChangedEventArgs e)
    {
        SetAndRaise(DistanceFractionProperty, ref _distanceFraction, _animation.Value);
        SetAndRaise(IsAnimatingProperty, ref _isAnimating, _animation.IsRunning);
    }
}
