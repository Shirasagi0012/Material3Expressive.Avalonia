using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Motion;

namespace Material3Expressive.Components;

public enum WideNavigationRailValue { Collapsed, Expanded }

/// <summary>Hoisted expansion state. Animated operations require a mounted rail; SnapToAsync can seed an unmounted state.</summary>
public sealed class WideNavigationRailState : AvaloniaObject
{
    public static readonly DirectProperty<WideNavigationRailState, WideNavigationRailValue> TargetValueProperty =
        AvaloniaProperty.RegisterDirect<WideNavigationRailState, WideNavigationRailValue>(nameof(TargetValue), state => state.TargetValue);
    public static readonly DirectProperty<WideNavigationRailState, WideNavigationRailValue> CurrentValueProperty =
        AvaloniaProperty.RegisterDirect<WideNavigationRailState, WideNavigationRailValue>(nameof(CurrentValue), state => state.CurrentValue);
    public static readonly DirectProperty<WideNavigationRailState, bool> IsAnimatingProperty =
        AvaloniaProperty.RegisterDirect<WideNavigationRailState, bool>(nameof(IsAnimating), state => state.IsAnimating);

    private WideNavigationRailValue _targetValue, _currentValue;
    private bool _isAnimating;
    private readonly HashSet<Control> _owners = [];
    private VisualAnimationScope? _scope;
    private Animatable<float>? _animation;
    private SpringSpec<float>? _spec;

    public WideNavigationRailState() : this(WideNavigationRailValue.Collapsed) { }
    public WideNavigationRailState(WideNavigationRailValue initialValue) => _targetValue = _currentValue = initialValue;
    public WideNavigationRailValue TargetValue => _targetValue;
    public WideNavigationRailValue CurrentValue => _currentValue;
    public new bool IsAnimating => _isAnimating;

    public Task ExpandAsync(CancellationToken cancellationToken = default) => AnimateAsync(WideNavigationRailValue.Expanded, cancellationToken);
    public Task CollapseAsync(CancellationToken cancellationToken = default) => AnimateAsync(WideNavigationRailValue.Collapsed, cancellationToken);
    public Task ToggleAsync(CancellationToken cancellationToken = default) =>
        AnimateAsync(TargetValue == WideNavigationRailValue.Expanded ? WideNavigationRailValue.Collapsed : WideNavigationRailValue.Expanded, cancellationToken);

    public Task SnapToAsync(WideNavigationRailValue targetValue, CancellationToken cancellationToken = default)
    {
        Dispatcher.UIThread.VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        if (_scope is not { IsEnding: false } scope || _animation is null)
        {
            SetAndRaise(TargetValueProperty, ref _targetValue, targetValue);
            SetAndRaise(CurrentValueProperty, ref _currentValue, targetValue);
            return Task.CompletedTask;
        }
        return scope.Launch(async lifetime =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, cancellationToken);
            await _animation.SnapToAsync(targetValue == WideNavigationRailValue.Expanded ? 1f : 0f, linked.Token);
        }, "Wide rail state snap");
    }

    private Task AnimateAsync(WideNavigationRailValue targetValue, CancellationToken cancellationToken)
    {
        Dispatcher.UIThread.VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        var scope = _scope is { IsEnding: false } active ? active
            : throw new InvalidOperationException("Mount a WideNavigationRail before animating its state. Use the constructor or SnapToAsync for an initial value.");
        return scope.Launch(async lifetime =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, cancellationToken);
            await _animation!.AnimateToAsync(targetValue == WideNavigationRailValue.Expanded ? 1f : 0f, _spec!, cancellationToken: linked.Token);
        }, "Wide rail state");
    }

    internal void Attach(Control owner, Foundation.Motion.SpringSpec spec)
    {
        if (!_owners.Add(owner) || _scope is { IsEnding: false }) return;
        // A hoisted state can drive multiple rails. Its animation survives an individual
        // consumer detaching, and ends when the final consumer releases it.
        _scope = VisualAnimationScope.Create(TopLevel.GetTopLevel(owner)!);
        _animation = _scope.CreateAnimatable(TargetValue == WideNavigationRailValue.Expanded ? 1f : 0f);
        _spec = new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 0.01f);
        _animation.PropertyChanged += OnAnimationChanged;
        Refresh();
    }

    internal void Detach(Control owner)
    {
        if (!_owners.Remove(owner) || _owners.Count != 0) return;
        if (_animation is not null) _animation.PropertyChanged -= OnAnimationChanged;
        _scope?.Dispose();
        _scope = null;
        _animation = null;
        SetAndRaise(IsAnimatingProperty, ref _isAnimating, false);
    }

    private void OnAnimationChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        if (_animation is not { } animation) return;
        // WideNavigationRailStateImpl maps every value other than exactly 1 to Collapsed,
        // including an overshooting spatial spring.
        SetAndRaise(TargetValueProperty, ref _targetValue, animation.TargetValue == 1f ? WideNavigationRailValue.Expanded : WideNavigationRailValue.Collapsed);
        SetAndRaise(CurrentValueProperty, ref _currentValue, animation.Value == 1f ? WideNavigationRailValue.Expanded : WideNavigationRailValue.Collapsed);
        SetAndRaise(IsAnimatingProperty, ref _isAnimating, animation.IsRunning);
    }
}

