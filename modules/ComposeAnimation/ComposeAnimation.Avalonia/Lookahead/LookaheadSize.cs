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
public sealed class LookaheadSize : Decorator, ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost, ILookaheadApproach
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

    private Animatable<Size>? _size;
    private IDisposable? _motionRegistration;
    private LookaheadContext? _context;
    private Size _target;
    private Size? _failedTarget;
    private long _generation;

    public LookaheadSize() => _entry = new(this);

    static LookaheadSize() => AffectsMeasure<LookaheadSize>(TargetSizeProperty);

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
            var clock = TopLevel.GetTopLevel(this) is not null && !AnimationScope.IsEnvironmentEnding(this)
                ? AnimationScope.ForHost(this).Clock : null;
            if (clock is not null)
            {
                _target = destination;
                _size = new Animatable<Size>(new SizeConverter(), destination, clock,
                    VisibilityThresholds.Size, "Lookahead size");
                _size.PropertyChanged += OnSizeSampleChanged;
                var motion = AnimationScope.ForHost(this);
                _motionRegistration = motion.RegisterResource(() => { if (motion.IsEnding) Reset(); });
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
        try { await size.AnimateToAsync(target, AnimationSpec); }
        catch (Exception error)
        {
            if (generation != _generation || !ReferenceEquals(_size, size)) return;
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
        var registration = _motionRegistration;
        _motionRegistration = null;
        registration?.Dispose();
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
        _entry.Detach();
        Reset();
        base.OnDetachedFromVisualTree(e);
    }
}
