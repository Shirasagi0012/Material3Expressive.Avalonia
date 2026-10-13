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
using Material3Expressive.Foundation.Rendering;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

// AnimatingChipContent's AnimatedVisibility, with rememberRetainedState while exiting.
internal sealed class ChipIconSlot : Decorator
{
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<ChipIconSlot, double>(nameof(IconSize));
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public static readonly StyledProperty<double> AvatarSizeProperty = AvaloniaProperty.Register<ChipIconSlot, double>(nameof(AvatarSize));
    public double AvatarSize { get => GetValue(AvatarSizeProperty); set => SetValue(AvatarSizeProperty, value); }
    public static readonly StyledProperty<CornerRadius> AvatarCornerRadiusProperty = AvaloniaProperty.Register<ChipIconSlot, CornerRadius>(nameof(AvatarCornerRadius));
    public CornerRadius AvatarCornerRadius { get => GetValue(AvatarCornerRadiusProperty); set => SetValue(AvatarCornerRadiusProperty, value); }
    public static readonly StyledProperty<MaterialSpring?> SizeEnterAnimationProperty = AvaloniaProperty.Register<ChipIconSlot, MaterialSpring?>(nameof(SizeEnterAnimation));
    public MaterialSpring? SizeEnterAnimation { get => GetValue(SizeEnterAnimationProperty); set => SetValue(SizeEnterAnimationProperty, value); }
    public static readonly StyledProperty<MaterialSpring?> SizeExitAnimationProperty = AvaloniaProperty.Register<ChipIconSlot, MaterialSpring?>(nameof(SizeExitAnimation));
    public MaterialSpring? SizeExitAnimation { get => GetValue(SizeExitAnimationProperty); set => SetValue(SizeExitAnimationProperty, value); }
    public static readonly StyledProperty<MaterialSpring?> AlphaEnterAnimationProperty = AvaloniaProperty.Register<ChipIconSlot, MaterialSpring?>(nameof(AlphaEnterAnimation));
    public MaterialSpring? AlphaEnterAnimation { get => GetValue(AlphaEnterAnimationProperty); set => SetValue(AlphaEnterAnimationProperty, value); }
    public static readonly StyledProperty<MaterialSpring?> AlphaExitAnimationProperty = AvaloniaProperty.Register<ChipIconSlot, MaterialSpring?>(nameof(AlphaExitAnimation));
    public MaterialSpring? AlphaExitAnimation { get => GetValue(AlphaExitAnimationProperty); set => SetValue(AlphaExitAnimationProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconSizeProperty || change.Property == AvatarSizeProperty || change.Property == AvatarCornerRadiusProperty
            || (MotionConfigurationReady && (change.Property == SizeEnterAnimationProperty || change.Property == SizeExitAnimationProperty
                || change.Property == AlphaEnterAnimationProperty || change.Property == AlphaExitAnimationProperty)))
            _owner?.Refresh();
    }

    private bool MotionConfigurationReady => SizeEnterAnimation is not null && SizeExitAnimation is not null
        && AlphaEnterAnimation is not null && AlphaExitAnimation is not null;
    private bool _pendingWidthUpdate;
    private AnimationChildScope? _motion;
    private AnimationChildScope? _visibilityMotion;
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
    private bool RequestedPresent => _requested is not null || _requestedTemplate is not null;
    private IDataTemplate? _requestedTemplate;
    public ChipIconSlot() { ClipToBounds = true; }
    internal void Initialize(AnimationScope parent) => _motion = parent.CreateChild("Chip icon visibility");
    internal void Stop()
    {
        _motion?.Dispose(); _motion = null; _visibilityMotion = null; _transition = null;
        _width = null; _pending = false; _initialized = false; _pendingWidthUpdate = false;
        // The icon is a caller-owned Control. Release it from the retired presenter before a
        // replacement template hosts that same object (logical and visual parents included).
        if (Presenter is { } presenter)
        { presenter.Content = null; presenter.ContentTemplate = null; presenter.UpdateChild(); }
    }

    private SpringSpec<float> SizeSpec(TransitionSegment<EnterExitState> segment)
    {
        var spec = (segment.TargetState == EnterExitState.Visible ? SizeEnterAnimation : SizeExitAnimation)
            ?? throw new InvalidOperationException("Chip icon width motion configuration is not ready.");
        return new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 1f);
    }
    private SpringSpec<float> AlphaSpec(TransitionSegment<EnterExitState> segment)
    {
        var spec = (segment.TargetState == EnterExitState.Visible ? AlphaEnterAnimation : AlphaExitAnimation)
            ?? throw new InvalidOperationException("Chip icon alpha motion configuration is not ready.");
        return new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 0.01f);
    }
    private float WidthTarget(EnterExitState state) => state == EnterExitState.Visible ? (float)_fullWidth : 0;

    private void CreateTransition(EnterExitState initial)
    {
        if (!MotionConfigurationReady) return;
        _visibilityMotion?.Dispose();
        _loopStarted = false;
        if (_motion is not { } motion || motion.Lifetime.IsCancellationRequested) return;
        var scope = _visibilityMotion = motion.CreateChild("AnimatedVisibility transition");
        var transition = _transition = new Transition<EnterExitState>(initial);
        _width = transition.AnimateValue(new ChipWidthConverter(), WidthTarget, SizeSpec);
        var alpha = transition.AnimateValue(new FloatConverter(), state => state == EnterExitState.Visible ? 1f : 0f, AlphaSpec);
        scope.SubscribeValue(_width, _ => InvalidateMeasure());
        scope.SubscribeValue(alpha, value => Opacity = Math.Clamp(value, 0, 1));
        void Finished(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(Transition.IsRunning) && !transition.IsRunning
                && transition.TargetState == EnterExitState.PostExit && !RequestedPresent)
            {
                if (Presenter is { } presenter)
                { presenter.Content = null; presenter.ContentTemplate = null; presenter.UpdateChild(); }
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
        var wasVisible = RequestedPresent;
        var previousWidth = _fullWidth;
        var requestedPresent = content is not null || template is not null;
        var changed = wasVisible != requestedPresent;
        _requested = content;
        _requestedTemplate = template;
        if (requestedPresent || !animate || !_initialized)
        {
            presenter.Content = content; presenter.ContentTemplate = template;
            presenter.UpdateChild();
            presenter.InvalidateMeasure(); border.InvalidateMeasure();
            MaterialIcon.SetDefaultSize(presenter, avatar ? AvatarSize : IconSize);
            border.Measure(Size.Infinity);
            _fullWidth = MathUtils.DipToPx(border.DesiredSize.Width, LayoutHelper.GetLayoutScale(this));
            // Retain the provider and avatar wrapper as well as the content on exit.
            presenter.Foreground = color;
            border.CornerRadius = avatar ? AvatarCornerRadius : default;
            border.ClipToBounds = avatar;
            border.Opacity = avatar ? avatarOpacity : 1;
        }
        if (!MotionConfigurationReady && animate && _motion is not null)
        {
            _pendingWidthUpdate |= previousWidth != _fullWidth;
            return;
        }
        var updateWidth = previousWidth != _fullWidth || _pendingWidthUpdate;
        _pendingWidthUpdate = false;
        if (!_initialized || !animate || _motion is null || _transition is null)
        {
            _initialized = true;
            _transitionPresent = _visibilityTarget = requestedPresent;
            Opacity = requestedPresent ? 1 : 0;
            CreateTransition(requestedPresent ? EnterExitState.Visible : EnterExitState.PreEnter);
            InvalidateMeasure();
        }
        else if (changed || requestedPresent != _visibilityTarget) QueueVisibility();
        else if (wasVisible && updateWidth)
        {
            if (_transition is { IsRunning: true }) _width?.Update(WidthTarget, SizeSpec);
            else CreateTransition(EnterExitState.Visible);
        }
    }

    private void QueueVisibility()
    {
        if (_motion is not { } motion || motion.Lifetime.IsCancellationRequested || _pending || !MotionConfigurationReady) return;
        _pending = true;
        // Coalesce setters in one request: avatar takes precedence over leadingIcon.
        Dispatcher.UIThread.Post(() =>
        {
            if (motion.Lifetime.IsCancellationRequested) return;
            if (!MotionConfigurationReady) { _pending = false; return; }
            _ = motion.Launch(async ct =>
            {
                if (!_transitionPresent || _transition is { IsRunning: true })
                {
                    await motion.NextFrameAsync(ct);
                    if (!MotionConfigurationReady) { _pending = false; return; }
                    if (!_transitionPresent) CreateTransition(EnterExitState.PreEnter);
                }
                _transitionPresent = true; _pending = false;
                var visible = RequestedPresent;
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
                        _ = scope.Launch(token => transition.RunAsync(scope, token), "Chip visibility transition");
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
            : !RequestedPresent ? 0 : size.Width;
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
