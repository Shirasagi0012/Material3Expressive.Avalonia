using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Animates the size reported by this control when its content's measured size changes.
/// The child is measured at its natural (target) size while the animated size is reported to
/// the parent, allowing surrounding layout to move continuously.
/// </summary>
/// <remarks>
/// Ordinary Avalonia layout is the default. Explicitly placing this control in an experimental
/// LookaheadRoot opts into cooperative target measurement and its documented tree restrictions.
/// Padding and borders in the template are part of the animated content size. Margin is outside.
/// </remarks>
public class AnimatedContentSize : ContentControl, ILookaheadLayoutNode, ILookaheadPlacementHost, IAnimationScopeHost
{
    public static readonly StyledProperty<IFiniteAnimationSpec<Size>> AnimationSpecProperty =
        AvaloniaProperty.Register<AnimatedContentSize, IFiniteAnimationSpec<Size>>(
            nameof(AnimationSpec), new SpringSpec<Size>(stiffness: Spring.StiffnessMediumLow,
                visibilityThreshold: VisibilityThresholds.Size), validate: value => value is not null);

    public static readonly StyledProperty<VisibilityAlignment> ContentAlignmentProperty =
        AvaloniaProperty.Register<AnimatedContentSize, VisibilityAlignment>(
            nameof(ContentAlignment), VisibilityAlignment.TopStart);

    public IFiniteAnimationSpec<Size> AnimationSpec
    {
        get => GetValue(AnimationSpecProperty);
        set => SetValue(AnimationSpecProperty, value);
    }

    public VisibilityAlignment ContentAlignment
    {
        get => GetValue(ContentAlignmentProperty);
        set => SetValue(ContentAlignmentProperty, value);
    }

    public event EventHandler<AnimatedContentSizeChangedEventArgs>? AnimationCompleted;

    private Animatable<Size>? _animatable;
    private ObservedClock? _clock;
    private IDisposable? _motionRegistration;
    private Size? _value;
    private Size? _requestedTarget;
    private Size? _failedTarget;
    private long _generation;
    private bool _runPending;
    private bool _attached;
    private static readonly IControlTemplate DefaultTemplate;

    static AnimatedContentSize()
    {
        ClipToBoundsProperty.OverrideDefaultValue<AnimatedContentSize>(true);
        AffectsArrange<AnimatedContentSize>(ContentAlignmentProperty, FlowDirectionProperty);
        HorizontalContentAlignmentProperty.OverrideDefaultValue<AnimatedContentSize>(HorizontalAlignment.Stretch);
        VerticalContentAlignmentProperty.OverrideDefaultValue<AnimatedContentSize>(VerticalAlignment.Stretch);
        DefaultTemplate = new FuncControlTemplate<AnimatedContentSize>((host, names) =>
            {
                var presenter = new ApproachContentPresenter { Name = "PART_ContentPresenter" };
                presenter[~ContentPresenter.ContentProperty] = new TemplateBinding(ContentProperty);
                presenter[~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(ContentTemplateProperty);
                presenter[~ContentPresenter.HorizontalContentAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty);
                presenter[~ContentPresenter.VerticalContentAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty);
                presenter[~ContentPresenter.BackgroundProperty] = new TemplateBinding(BackgroundProperty);
                presenter[~ContentPresenter.BackgroundSizingProperty] = new TemplateBinding(BackgroundSizingProperty);
                presenter[~ContentPresenter.BorderBrushProperty] = new TemplateBinding(BorderBrushProperty);
                presenter[~ContentPresenter.BorderThicknessProperty] = new TemplateBinding(BorderThicknessProperty);
                presenter[~ContentPresenter.CornerRadiusProperty] = new TemplateBinding(CornerRadiusProperty);
                presenter[~ContentPresenter.PaddingProperty] = new TemplateBinding(PaddingProperty);
                names.Register(presenter.Name, presenter);
                return presenter;
            });
        TemplateProperty.OverrideDefaultValue<AnimatedContentSize>(DefaultTemplate);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AnimationSpecProperty && _failedTarget is not null)
        {
            _failedTarget = null;
            InvalidateMeasure();
        }
    }

    // The old scope's end released the animation state; measure again to start on the new scope.
    void IAnimationScopeHost.OnScopeChanged(AnimationScopeChangedEventArgs e) => InvalidateMeasure();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        _clock = null;
        _failedTarget = null;
        InvalidateMeasure();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        ReleaseAnimator();
        _clock = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Match upstream: use target constraints when provided by a lookahead scope,
        // otherwise animate the ordinary measured size. Nested animation samples never
        // replace a scoped target size.
        var lookahead = LookaheadRoot.TargetFor(this);
        var measured = base.MeasureOverride(lookahead?.MeasureConstraints ?? availableSize);
        // Avalonia Measure(Size) has no minimum-constraint pair to pass to the child.
        // Normalize the container endpoint to its own Min/Max/fixed dimensions instead of
        // starting an animation from an unobservable size below its minimum.
        var target = LayoutHelper.ApplyLayoutConstraints(this, lookahead?.MeasuredSize ?? measured);
        // Off-tree probes must not become the initial visible animation state. A previously
        // attached instance keeps its last sample for recovery when it reattaches.
        if (!_attached) return Constrain(target, availableSize);
        if (_value is null)
        {
            _value = target;
            return Constrain(target, availableSize);
        }
        if (_clock?.Faulted == true || _failedTarget == target)
        {
            _value = target; // Render usable content after a failure; never retry every layout.
            return Constrain(target, availableSize);
        }
        var animated = _animatable?.Value ?? _value.Value;
        // Upstream also resumes an interrupted run whose target has not changed. A pending
        // async drive may briefly not be running while its predecessor releases the mutex.
        if ((_animatable is not null && target != _requestedTarget) ||
            (!_runPending && _animatable?.IsRunning != true && animated != target))
            StartAnimation(animated, target);
        // A synchronous provider/spec may complete or fail inside StartAnimation. Its callback
        // can ResetAnimation and clear _value before this measure returns.
        animated = _animatable?.Value ?? _value ?? target;
        return Constrain(animated, availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Arrange the entire template root, including any decoration or nested presenter.
        // No cached template part: replacing/removing a template must not arrange the old tree.
        foreach (var child in this.GetVisualChildren().OfType<Control>())
        {
            var childSize = child.DesiredSize;
            var offset = VisibilityMotion.Align(childSize, finalSize, ContentAlignment, FlowDirection);
            LookaheadPlacement.ArrangeChild(this, child, new Rect(offset, childSize));
        }
        return finalSize;
    }

    private void StartAnimation(Size from, Size to)
    {
        if (!_attached) return;
        if (AnimationScope.IsEnvironmentEnding(this)) { _value = to; return; }
        EnsureClock();
        if (_clock is null) { _value = to; return; }
        _failedTarget = null;
        _animatable ??= new Animatable<Size>(new SizeConverter(), from, _clock,
            VisibilityThresholds.Size, "AnimatedContentSize");
        _animatable.PropertyChanged -= OnAnimationChanged;
        _animatable.PropertyChanged += OnAnimationChanged;
        var generation = ++_generation;
        _requestedTarget = to;
        _runPending = true;
        var motion = AnimationScope.Get(this);
        _ = motion.Launch(ct => AnimateAsync(_animatable, to, AnimationSpec, generation, ct), "Content size");
    }

    private async Task AnimateAsync(Animatable<Size> animatable, Size target,
        IFiniteAnimationSpec<Size> spec, long generation, CancellationToken cancellationToken)
    {
        var initial = animatable.Value;
        AnimationResult<Size> result;
        try
        {
            result = await animatable.AnimateToAsync(target, spec, cancellationToken: cancellationToken);
        }
        catch (Exception error)
        {
            if (!IsCurrent() || cancellationToken.IsCancellationRequested) throw;
            // Our detach/reset/retarget cancellations have already changed the generation.
            // A current-run cancellation is a failure too (a spec can throw OCE), not a reason
            // to restart indefinitely on each layout pass.
            ReleaseAnimator();
            _value = target;
            _failedTarget = target;
            InvalidateMeasure();
            AnimationScope.ReportError(this, error);
            throw;
        }
        if (!IsCurrent()) return;
        _runPending = false;
        _value = animatable.Value;
        InvalidateMeasure();
        if (result.EndReason == AnimationEndReason.Finished)
        {
            // Publish after all run state has settled, allowing handlers to change content,
            // reset the control or detach it synchronously.
            try { AnimationCompleted?.Invoke(this, new AnimatedContentSizeChangedEventArgs(initial, result.EndState.Value)); }
            catch (Exception error) { AnimationScope.ReportError(this, error); }
        }

        bool IsCurrent() => _attached && generation == _generation && ReferenceEquals(_animatable, animatable);
    }

    private void OnAnimationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Animatable<Size>.Value) or nameof(Animatable<Size>.IsRunning))
        {
            if (_animatable is { } animation &&
                (!double.IsFinite(animation.Value.Width) || !double.IsFinite(animation.Value.Height)))
                throw new InvalidOperationException("AnimatedContentSize animation produced a non-finite size.");
            _value = _animatable?.Value ?? _value;
            InvalidateMeasure();
            InvalidateArrange();
        }
    }

    private void EnsureClock()
    {
        if (_clock is not null) return;
        if (TopLevel.GetTopLevel(this) is null) return;
        var motion = AnimationScope.Get(this);
        _motionRegistration?.Dispose();
        _motionRegistration = motion.RegisterResource(() =>
        {
            if (motion.IsEnding) ReleaseAnimator();
        });
        _clock = new ObservedClock(motion.Clock);
    }

    /// <summary>
    /// Cancels motion and forgets the previous size, for example when recycling this instance
    /// for another item. The next measure initializes directly to its target without completion.
    /// Ordinary detach/reattach preserves the current size instead. Also permits retry after errors.
    /// </summary>
    public void ResetAnimation()
    {
        VerifyAccess();
        ReleaseAnimator();
        _value = null;
        _clock = null;
        _failedTarget = null;
        InvalidateMeasure();
    }

    private void ReleaseAnimator()
    {
        ++_generation;
        _runPending = false;
        _requestedTarget = null;
        if (_animatable is not { } animatable) return;
        _value = animatable.Value;
        _animatable = null;
        animatable.PropertyChanged -= OnAnimationChanged;
        animatable.Stop();
    }

    // Distinguish provider failures (including unexpected cancellation) from spec failures.
    // The root environment does not retry a failed provider.
    private sealed class ObservedClock(IFrameSource provider) : IFrameSource, IAnimationLifetimeClock, IFrameWaitProvider
    {
        public AnimationLifetime? AnimationLifetime => (provider as IAnimationLifetimeClock)?.AnimationLifetime;
        public double DurationScale => (provider as IAnimationLifetimeClock)?.DurationScale ?? 1;
        public bool Faulted { get; private set; }
        public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken) => NextFrameAsync(cancellationToken, null);
        // Pass-through, not async: keeps the FrameClock's inline resumption (see FrameWait).
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken, Action<Exception>? onFault)
            => FrameWait.Observe(provider, cancellationToken, error =>
            {
                if (error is not OperationCanceledException || !cancellationToken.IsCancellationRequested) Faulted = true;
                onFault?.Invoke(error);
            });
    }

    private static Size Constrain(Size size, Size available)
        => new(Math.Min(Math.Max(0, size.Width), available.Width),
            Math.Min(Math.Max(0, size.Height), available.Height));

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren
        => Content is Control child ? new[] { child } : Array.Empty<Control>();
    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties => [ContentAlignmentProperty];

    // The experimental target policy permits only this undecorated, stretch presenter.
    // Ordinary/custom-template layout keeps the native ContentPresenter behavior.
    private sealed class ApproachContentPresenter : ContentPresenter, ILookaheadPlacementHost
    {
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (TemplatedParent is AnimatedContentSize host && LookaheadRoot.TargetFor(host) is not null)
            {
                if (Child is { } child) LookaheadPlacement.ArrangeChild(this, child, new Rect(finalSize));
                return finalSize;
            }
            return base.ArrangeOverride(finalSize);
        }
    }

    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
    {
        if (!ReferenceEquals(Template, DefaultTemplate) || ContentTemplate is not null || Content is not null and not Control ||
            Padding != default || BorderThickness != default ||
            HorizontalContentAlignment != HorizontalAlignment.Stretch ||
            VerticalContentAlignment != VerticalAlignment.Stretch)
            throw new NotSupportedException("Experimental AnimatedContentSize lookahead requires the default template, direct Control content, " +
                "stretch content alignment, and no ContentTemplate/Padding/BorderThickness. Put decoration inside a sized leaf.");
        return Content is Control child ? builder.Measure(child, available) : default;
    }

    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
    {
        if (Content is not Control child) return;
        var size = builder.Desired(child);
        var offset = VisibilityMotion.Align(size, bounds.Size, ContentAlignment, FlowDirection);
        builder.Place(child, new Rect(bounds.TopLeft + new Vector(offset.X, offset.Y), size));
    }
}

public sealed class AnimatedContentSizeChangedEventArgs(Size initialSize, Size targetSize) : EventArgs
{
    public Size InitialSize { get; } = initialSize;
    public Size TargetSize { get; } = targetSize;
}
