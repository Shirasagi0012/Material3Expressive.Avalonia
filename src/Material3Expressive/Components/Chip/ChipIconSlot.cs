using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

// AnimatingChipContent's AnimatedVisibility, with rememberRetainedState while exiting.
internal sealed class ChipIconSlot : Decorator
{
    private VisualAnimationScope? _motion;
    private VisualAnimationScope? _visibilityMotion;
    private Transition<EnterExitState>? _transition;
    private Transition<EnterExitState>.TransitionAnimationState<float>? _width;
    private object? _requested;
    private bool _initialized;
    private double _fullWidth;
    private bool _pending;
    private Chip? _owner;
    private bool _animate;
    private bool _transitionPresent;
    private bool _visibilityTarget;
    private bool _loopStarted;
    private ContentPresenter? Presenter => (Child as Border)?.Child as ContentPresenter;
    public ChipIconSlot() { ClipToBounds = true; }
    internal void Initialize(VisualAnimationScope parent) => _motion = parent.CreateChild("Chip icon visibility");
    internal void Stop()
    {
        _motion?.Dispose(); _motion = null; _visibilityMotion = null; _transition = null;
        _width = null; _pending = false; _initialized = false;
        // The icon is a caller-owned Control. Release it from the retired presenter before a
        // replacement template hosts that same object (logical and visual parents included).
        if (Presenter is { } presenter) { presenter.Content = null; presenter.UpdateChild(); }
    }

    private SpringSpec<float> SizeSpec(TransitionSegment<EnterExitState> segment)
    {
        var spec = _owner!.Spring(segment.TargetState == EnterExitState.Visible || _owner.MorphShapes is not null
            ? Sys.Motion.Spring.SpatialFast : Sys.Motion.Spring.EffectsDefault);
        return new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 1f);
    }
    private SpringSpec<float> AlphaSpec(TransitionSegment<EnterExitState> segment)
    {
        var spec = _owner!.Spring(_owner.MorphShapes is not null ? Sys.Motion.Spring.EffectsDefault
            : segment.TargetState == EnterExitState.Visible ? Sys.Motion.Spring.EffectsSlow : Sys.Motion.Spring.EffectsFast);
        return new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 0.01f);
    }
    private float WidthTarget(EnterExitState state) => state == EnterExitState.Visible ? (float)_fullWidth : 0;

    private void CreateTransition(EnterExitState initial)
    {
        _visibilityMotion?.Dispose();
        _loopStarted = false;
        if (_motion is not { IsEnding: false } motion) return;
        var scope = _visibilityMotion = motion.CreateChild("AnimatedVisibility transition");
        var transition = _transition = scope.CreateTransition(initial);
        _width = transition.AnimateValue(new ChipWidthConverter(), WidthTarget, SizeSpec);
        var alpha = transition.AnimateValue(new FloatConverter(), state => state == EnterExitState.Visible ? 1f : 0f, AlphaSpec);
        scope.SubscribeValue(_width, _ => InvalidateMeasure());
        scope.SubscribeValue(alpha, value => Opacity = Math.Clamp(value, 0, 1));
        void Finished(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(Transition.IsRunning) && !transition.IsRunning
                && transition.TargetState == EnterExitState.PostExit && _requested is null)
            {
                if (Presenter is { } presenter) presenter.Content = null;
                _transitionPresent = false; InvalidateMeasure();
            }
        }
        transition.PropertyChanged += Finished;
        scope.Lifetime.Register(() => transition.PropertyChanged -= Finished);
    }

    // IntSize.VectorConverter rounds each value, retaining the vector velocity for reversal.
    private sealed class ChipWidthConverter : ITwoWayConverter<float>
    {
        public int Dimension => 1;
        public void To(float value, Span<double> destination) => destination[0] = value;
        public float From(ReadOnlySpan<double> vector) => MathUtils.RoundToInt((float)vector[0]);
    }

    internal void Update(object? content, IDataTemplate? template, bool animate, bool avatar, double avatarOpacity, IBrush? color, Chip owner)
    {
        _owner = owner; _animate = animate;
        if (Presenter is not { } presenter || Child is not Border border) return;
        var wasVisible = _requested is not null;
        var previousWidth = _fullWidth;
        var changed = wasVisible != (content is not null);
        _requested = content;
        if (content is not null || !animate || !_initialized)
        {
            presenter.Content = content; presenter.ContentTemplate = template;
            presenter.UpdateChild();
            presenter.InvalidateMeasure(); border.InvalidateMeasure();
            border.Measure(Size.Infinity);
            _fullWidth = MathUtils.DipToPx(border.DesiredSize.Width, LayoutHelper.GetLayoutScale(this));
            // Retain the provider and avatar wrapper as well as the content on exit.
            presenter.Foreground = color;
            border.CornerRadius = avatar ? owner.Resolve(Comp.Chip.AvatarShape) : default;
            border.ClipToBounds = avatar;
            border.Opacity = avatar ? avatarOpacity : 1;
        }
        if (!_initialized || !animate || _motion is null)
        {
            _initialized = true;
            _transitionPresent = _visibilityTarget = content is not null;
            Opacity = content is null ? 0 : 1;
            CreateTransition(content is null ? EnterExitState.PreEnter : EnterExitState.Visible);
            InvalidateMeasure();
        }
        else if (changed) QueueVisibility();
        else if (wasVisible && previousWidth != _fullWidth)
        {
            if (_transition is { IsRunning: true }) _width?.Update(WidthTarget, SizeSpec);
            else CreateTransition(EnterExitState.Visible);
        }
    }

    private void QueueVisibility()
    {
        if (_motion is not { IsEnding: false } motion || _pending) return;
        _pending = true;
        // Coalesce setters in one request: avatar takes precedence over leadingIcon.
        Dispatcher.UIThread.Post(() =>
        {
            if (motion.IsEnding) return;
            _ = motion.Launch(async ct =>
            {
                if (!_transitionPresent || _transition is { IsRunning: true })
                {
                    await motion.Clock.NextFrameAsync(ct);
                    if (!_transitionPresent) CreateTransition(EnterExitState.PreEnter);
                }
                _transitionPresent = true; _pending = false;
                var visible = _requested is not null;
                if (visible == _visibilityTarget)
                    CreateTransition(visible ? EnterExitState.Visible : EnterExitState.PostExit);
                else
                {
                    // An idle presentation has no composition-frame waiter. Start its loop
                    // with real work already registered; interruption retains the active loop.
                    if (_transition is not { IsRunning: true })
                        CreateTransition(_visibilityTarget ? EnterExitState.Visible : EnterExitState.PreEnter);
                    _transition?.UpdateTarget(visible ? EnterExitState.Visible : EnterExitState.PostExit);
                    if (!_loopStarted && _visibilityMotion is { } scope && _transition is { } transition)
                    {
                        _loopStarted = true;
                        _ = scope.Launch(token => transition.RunAsync(token), "Chip visibility transition");
                    }
                }
                _visibilityTarget = visible;
            }, "Chip icon composition");
        });
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize);
        var size = Child?.DesiredSize ?? default;
        var scale = LayoutHelper.GetLayoutScale(this);
        var width = _animate && _width is not null ? Math.Max(0, _width.Value) / scale
            : _requested is null ? 0 : size.Width;
        return new Size(width, size.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var fullWidth = _fullWidth / LayoutHelper.GetLayoutScale(this);
        // Avalonia resolves logical Start/End through the inherited subtree reflection.
        Child?.Arrange(new Rect(Name == "PART_TrailingIcon" ? finalSize.Width - fullWidth : 0, 0, fullWidth, finalSize.Height));
        return finalSize;
    }
}
