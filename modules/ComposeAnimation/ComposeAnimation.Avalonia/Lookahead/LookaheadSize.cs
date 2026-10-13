using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Experimental size approach: measures real content under the current animated bounding box.
/// Combine with LookaheadPosition for moving, resizing content. This is not animateContentSize:
/// it uses intermediate constraints rather than holding content at the destination constraints.
/// </summary>
/// <remarks>
/// TargetSize optionally supplies a target-only box; Width/Height remain ordinary Avalonia
/// constraints on every frame. Without TargetSize the cooperative child determines the target.
/// It creates a local lookahead domain when no participating parent domain owns it. The complete
/// constraint path preserves minima; the legacy Size path retains Avalonia maximum-only behavior.
/// Padding is not supported.
/// </remarks>
public sealed class LookaheadSize : Decorator, ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost, ILookaheadApproach, IAnimationScopeHost
{
    private readonly LookaheadEntry _entry;
    public static readonly StyledProperty<Size?> TargetSizeProperty =
        AvaloniaProperty.Register<LookaheadSize, Size?>(nameof(TargetSize),
            validate: size => size is null || (double.IsFinite(size.Value.Width) &&
                double.IsFinite(size.Value.Height) && size.Value.Width >= 0 && size.Value.Height >= 0));
    public static readonly StyledProperty<IFiniteAnimationSpec<Size>> AnimationSpecProperty =
        AvaloniaProperty.Register<LookaheadSize, IFiniteAnimationSpec<Size>>(nameof(AnimationSpec),
            new SpringSpec<Size>(stiffness: Spring.StiffnessMediumLow, visibilityThreshold: VisibilityThresholds.Size),
            validate: value => value is not null);

    public Size? TargetSize { get => GetValue(TargetSizeProperty); set => SetValue(TargetSizeProperty, value); }
    public IFiniteAnimationSpec<Size> AnimationSpec
    {
        get => GetValue(AnimationSpecProperty);
        set => SetValue(AnimationSpecProperty, value);
    }

    // The scope the animatable's runs use; replaced together with it.
    private FrameScope? _runScope;
    private Animatable<Size>? _size;
    private readonly ScopeResumeBinding _scopeBinding;
    private bool _attached;
    private LookaheadContext? _context;
    private Size _target;
    private Size? _failedTarget;
    private long _generation;

    public LookaheadSize()
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
                if (_size is not { } animatable) return false;
                _runScope = motion;
                if (animatable.Value != _target) _ = Animate(animatable, _target, ++_generation);
                return true;
            },
            onLost: Reset,
            invalidate: _ => InvalidateMeasure());

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = true;
        base.OnAttachedToVisualTree(e);
    }


    static LookaheadSize()
    {
        AffectsMeasure<LookaheadSize>(TargetSizeProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty) _entry?.VisualParentChanged();
        if (change.Property == AnimationSpecProperty && _failedTarget is not null)
        {
            _failedTarget = null;
            InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        CheckPadding();
        var root = LookaheadScope.FindParticipatingContext(this);
        var target = LookaheadRoot.TargetFor(this);
        if (root is null || target is null)
        {
            Reset();
            return MeasureContent(availableSize, (child, constraints) =>
            {
                child.Measure(constraints);
                return child.DesiredSize;
            });
        }
        if (_context != root) Reset();
        _context = root;
        var destination = target.Bounds.Size;
        if (_size is null && _failedTarget != destination)
        {
            var scope = TopLevel.GetTopLevel(this) is not null && !AnimationScope.IsEnvironmentEnding(this)
                ? AnimationScope.Get(this) : null;
            if (scope is not null)
            {
                _target = destination;
                _size = new Animatable<Size>(new SizeConverter(), destination,
                    VisibilityThresholds.Size, "Lookahead size");
                _runScope = scope;
                _size.PropertyChanged += OnSizeSampleChanged;
                _scopeBinding.Reset();
                _scopeBinding.Bind(scope);
            }
        }
        if (_size is { } size && _target != destination)
        {
            _target = destination;
            _failedTarget = null;
            _ = Animate(size, destination, ++_generation);
        }
        var sample = _size?.Value ?? destination;
        sample = new Size(Math.Max(0, sample.Width), Math.Max(0, sample.Height)).Constrain(availableSize);
        Child?.Measure(sample);
        return sample;
    }

    private async Task Animate(Animatable<Size> size, Size target, long generation)
    {
        var scope = _runScope!;
        try { await size.AnimateToAsync(scope, target, AnimationSpec); }
        catch (Exception error)
        {
            if (generation != _generation || !ReferenceEquals(_size, size)) return;
            // Cancelled because its scope ended (rebuild or release): not a failure.
            if (error is OperationCanceledException && scope.Lifetime.IsCancellationRequested) return;
            var root = _context;
            Reset();
            _context = root;
            _failedTarget = target;
            InvalidateMeasure();
            AnimationScope.ReportError(this, error);
        }
    }

    private void OnSizeSampleChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Animatable<Size>.Value) or nameof(Animatable<Size>.IsRunning))
        {
            if (_size is { } size && (!double.IsFinite(size.Value.Width) || !double.IsFinite(size.Value.Height)))
                throw new InvalidOperationException("LookaheadSize animation produced a non-finite size.");
            InvalidateMeasure();
            // Size/alignment changes can move descendants even after their position run ends.
            _context?.InvalidateApproachPlacement();
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child) LookaheadPlacement.ArrangeChild(this, child, new Rect(finalSize));
        return finalSize;
    }

    private Size MeasureContent(Size available, Func<Control, Size, Size> measure)
    {
        CheckPadding();
        var constraints = TargetSize?.Constrain(available) ?? available;
        var desired = Child is { } child ? measure(child, constraints) : default;
        return TargetSize is not null ? constraints : desired;
    }

    private void CheckPadding()
    {
        if (Padding != default) throw new NotSupportedException("LookaheadSize does not support Padding. Use child Margin.");
    }

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Child is { } child ? [child] : [];
    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties => [TargetSizeProperty];
    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
        => MeasureContent(available, builder.Measure);
    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
    {
        CheckPadding();
        var childConstraints = TargetSize is { } target
            ? constraints.Constrain(LookaheadConstraints.Fixed(target)) : constraints;
        var desired = Child is { } child ? builder.MeasureChild(child, childConstraints) : childConstraints.MinSize;
        return TargetSize is not null ? childConstraints.MaxSize : desired;
    }
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints)
        => MeasureOverride(constraints.MaxSize);
    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
    {
        if (Child is { } child) builder.Place(child, bounds);
    }

    void ILookaheadApproach.ResetApproach() => Reset();

    protected override Size MeasureCore(Size availableSize)
        => _entry.MeasureCore(availableSize, () => base.MeasureCore(availableSize));

    protected override void ArrangeCore(Rect finalRect)
    {
        if (_entry.TryArrangeCore(finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }
    private void Reset()
    {
        _scopeBinding.Reset();
        ++_generation;
        if (_size is { } size)
        {
            _size = null;
            size.PropertyChanged -= OnSizeSampleChanged;
            size.Stop();
        }
        _context = null;
        _failedTarget = null;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // The lookahead entry belongs to the tree position and goes now; the animation is released
        // when the scope ends at the drain (D17), or resumes if re-attached elsewhere.
        _attached = false;
        _entry.Detach();
        base.OnDetachedFromVisualTree(e);
    }
}
