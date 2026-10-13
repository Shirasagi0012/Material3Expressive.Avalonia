using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Experimental placement-only approach in a cooperative lookahead domain. It creates a local
/// domain when needed. Layout reserves the destination slot while real Bounds follow the trajectory.
/// Size is not animated.
/// Cooperative motion-frame offsets follow immediately. No reference selection or render-transform conversion is provided.
/// </summary>
public sealed class LookaheadPosition : Decorator, ILookaheadConstrainedLayoutNode, ILookaheadPlacementApproach, ILookaheadPlacementHost, IAnimationScopeHost
{
    private readonly LookaheadEntry _entry;
    public static readonly StyledProperty<IFiniteAnimationSpec<Point>> AnimationSpecProperty =
        AvaloniaProperty.Register<LookaheadPosition, IFiniteAnimationSpec<Point>>(nameof(AnimationSpec),
            new SpringSpec<Point>(stiffness: Spring.StiffnessMediumLow, visibilityThreshold: VisibilityThresholds.Point),
            validate: value => value is not null);
    public IFiniteAnimationSpec<Point> AnimationSpec
    {
        get => GetValue(AnimationSpecProperty);
        set => SetValue(AnimationSpecProperty, value);
    }

    // The scope the animatable's runs use; replaced together with it.
    private FrameScope? _runScope;
    private long _generation;
    private Animatable<Point>? _position;
    private readonly ScopeResumeBinding _scopeBinding;
    private bool _attached;
    private LookaheadContext? _context;
    private Point _target;

    public LookaheadPosition()
    {
        _entry = new(this);
        _scopeBinding = new ScopeResumeBinding(this, () => _attached, Reset);
    }

    // D17: a rebuild while attached keeps the animatable (value and velocity where the cancelled
    // run stopped) and the target; re-run toward the target in the replacement scope. A real
    // lookahead context change still resets (it is not a scope rebuild).
    void IAnimationScopeHost.OnScopeChanged(AnimationScopeChangedEventArgs e)
        => _scopeBinding.Handle(e,
            onResume: motion =>
            {
                if (_position is not { } animatable) return false;
                _runScope = motion;
                if (animatable.Value != _target) _ = Animate(animatable, _target);
                return true;
            },
            onLost: Reset,
            // Intentional: a placement-only host also invalidates the domain's approach placement.
            invalidate: _ =>
            {
                _context?.InvalidateApproachPlacement();
                InvalidateMeasure();
            });

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = true;
        base.OnAttachedToVisualTree(e);
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty) _entry?.VisualParentChanged();
    }

    Point ILookaheadPlacementApproach.ApproachPosition(LookaheadContext root, Point target)
    {
        if (_context != root) Reset();
        _context = root;
        if (_position is null)
        {
            var scope = TopLevel.GetTopLevel(this) is not null && !AnimationScope.IsEnvironmentEnding(this)
                ? AnimationScope.Get(this) : null;
            if (scope is null) return target + LookaheadCoordinates.MotionOffset(this, root.CoordinateReference);
            _target = target;
            _position = new Animatable<Point>(new PointConverter(), target,
                VisibilityThresholds.Point, "Lookahead position");
            _runScope = scope;
            _position.PropertyChanged += PositionChanged;
            _scopeBinding.Reset();
            _scopeBinding.Bind(scope);
        }
        if (_target != target)
        {
            _target = target;
            _ = Animate(_position, target);
        }
        return _position.Value + LookaheadCoordinates.MotionOffset(this, root.CoordinateReference);
    }

    private async Task Animate(Animatable<Point> position, Point target)
    {
        var generation = ++_generation;
        var scope = _runScope!;
        try { await position.AnimateToAsync(scope, target, AnimationSpec); }
        // Interrupted by a newer run or a reset, or cancelled because its scope ended (a rebuild
        // resumes it; a release resets it): not a failure.
        catch (OperationCanceledException) when (generation != _generation || !ReferenceEquals(_position, position) ||
                                                  scope.Lifetime.IsCancellationRequested) { }
    }

    private void PositionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Animatable<Point>.Value) or nameof(Animatable<Point>.IsRunning))
            _context?.InvalidateApproachPlacement();
    }

    private void Reset()
    {
        _scopeBinding.Reset();
        if (_position is { } position)
        {
            position.PropertyChanged -= PositionChanged;
            position.Stop();
            _position = null;
        }
        _context = null;
    }

    void ILookaheadApproach.ResetApproach() => Reset();

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // The lookahead entry belongs to the tree position and goes now; the animation is released
        // when the scope ends at the drain (D17), or resumes if re-attached elsewhere.
        _attached = false;
        _entry.Detach();
        base.OnDetachedFromVisualTree(e);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Padding != default) throw new NotSupportedException("LookaheadPosition does not support Padding. Use child Margin.");
        if (Child is { } child) LookaheadPlacement.ArrangeChild(this, child, new Rect(finalSize));
        return finalSize;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (LookaheadScope.FindParticipatingContext(this) is not null &&
            (this.GetVisualParent() is not ILookaheadPlacementHost || LookaheadRoot.TargetFor(this) is null))
            throw new NotSupportedException("LookaheadPosition requires a registered target and an ILookaheadPlacementHost parent that uses LookaheadPlacement.ArrangeChild.");
        return base.MeasureOverride(availableSize);
    }

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Child is { } child ? [child] : [];
    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
    {
        if (Padding != default) throw new NotSupportedException("LookaheadPosition does not support Padding. Use child Margin.");
        return Child is { } child ? builder.Measure(child, available) : default;
    }
    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
    {
        if (Padding != default) throw new NotSupportedException("LookaheadPosition does not support Padding. Use child Margin.");
        return Child is { } child ? builder.MeasureChild(child, constraints) : constraints.MinSize;
    }
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints)
    {
        if (Padding != default) throw new NotSupportedException("LookaheadPosition does not support Padding. Use child Margin.");
        return Child is { } child ? LookaheadMeasure.MeasureChild(child, constraints) : constraints.MinSize;
    }
    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
    {
        if (Child is { } child) builder.Place(child, bounds);
    }

    protected override Size MeasureCore(Size availableSize)
        => _entry.MeasureCore(availableSize, () => base.MeasureCore(availableSize));

    protected override void ArrangeCore(Rect finalRect)
    {
        if (_entry.TryArrangeCore(finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }
}
