using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Foundation.Interaction;

// Owns one indication collector and one animation, independently of the Surface ripple lease.
// Configuration values are supplied by the implicit theme; geometry comes from the component.
internal sealed class InsetFocusRingPresenter : Control
{
    internal static readonly StyledProperty<Color?> DefaultOuterColorProperty =
        AvaloniaProperty.Register<InsetFocusRingPresenter, Color?>("DefaultOuterColor");
    internal static readonly StyledProperty<Color?> DefaultInnerColorProperty =
        AvaloniaProperty.Register<InsetFocusRingPresenter, Color?>("DefaultInnerColor");
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec?> FocusingAnimationProperty =
        AvaloniaProperty.Register<InsetFocusRingPresenter, Foundation.Motion.SpringSpec?>("FocusingAnimation");
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec?> UnfocusingAnimationProperty =
        AvaloniaProperty.Register<InsetFocusRingPresenter, Foundation.Motion.SpringSpec?>("UnfocusingAnimation");
    public Color? DefaultOuterColor { get => GetValue(DefaultOuterColorProperty); set => SetValue(DefaultOuterColorProperty, value); }
    public Color? DefaultInnerColor { get => GetValue(DefaultInnerColorProperty); set => SetValue(DefaultInnerColorProperty, value); }
    public Foundation.Motion.SpringSpec? FocusingAnimation { get => GetValue(FocusingAnimationProperty); set => SetValue(FocusingAnimationProperty, value); }
    public Foundation.Motion.SpringSpec? UnfocusingAnimation { get => GetValue(UnfocusingAnimationProperty); set => SetValue(UnfocusingAnimationProperty, value); }

    private readonly List<MaterialInteractionEvent> _interactions = [];
    private readonly InsetRingBorder _inner = new(), _outer = new();
    private readonly SolidColorBrush _innerBrush = new(), _outerBrush = new();
    private MaterialInteractionEvent? _currentInteraction;
    private int _actualFocusCount;
    private int _sentFocusCount;
    private Control? _owner;
    private StyledElement? _resourceParent;
    private MaterialInteractionController? _controller;
    private AnimationChildScope? _motion;
    private Animatable<float>? _progress;
    private float _lastAnimatedProgress;
    private CornerRadius _corners;
    private Color? _paintedDefaultInnerColor, _paintedDefaultOuterColor;
    private bool _captureDefaultColors = true;
    private (Size Size, float Scale) _paintGeometry;

    internal float Progress => _progress?.Value ?? 0;
    internal CornerRadius Corners
    {
        get => _corners;
        set { if (_corners == value) return; _corners = value; InvalidateRing(); }
    }

    // Fixed Ripple.kt caches the node configuration by metrics/local overrides/motion scheme,
    // not by theme palette. A palette-only update changes the color producers but does not
    // invalidate an idle ring's recorded draw. Read the current bound colors on its next draw.
    // Avalonia may call Render again because a parent changed, even if Compose would retain
    // this node's draw. Capture palette colors only at the ring's own draw invalidations.
    private void InvalidateRing()
    {
        _captureDefaultColors = true;
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FocusingAnimationProperty || change.Property == UnfocusingAnimationProperty)
            InvalidateRing();
    }

    public InsetFocusRingPresenter()
    {
        IsHitTestVisible = false;
        Focusable = false;
        ClipToBounds = false;
    }

    internal void Connect(Control owner)
    {
        if (ReferenceEquals(owner, _owner)) return;
        Disconnect();
        _owner = owner;
        // A visual-only child has no logical parent to supply styling/resource inheritance.
        // Keep it out of LogicalChildren, but explicitly bridge that context and its changes.
        _resourceParent = this.GetVisualParent() as StyledElement ?? owner;
        ((ISetInheritanceParent)this).SetParent(_resourceParent);
        _resourceParent.ResourcesChanged += ResourcesChangedAtParent;
        Theme = _resourceParent.FindResource(typeof(InsetFocusRingPresenter)) as ControlTheme;
        ApplyStyling();
        _controller = MaterialInteraction.GetOrCreate(owner);
        _controller.IndicationInteraction += Receive;
        _controller.IndicationReset += Reset;
        owner.PropertyChanged += OwnerChanged;
        AnimationScope.AddScopeChangedHandler(this, ScopeChanged);
    }

    internal void Disconnect()
    {
        AnimationScope.RemoveScopeChangedHandler(this, ScopeChanged);
        if (_resourceParent is { } resourceParent) resourceParent.ResourcesChanged -= ResourcesChangedAtParent;
        _resourceParent = null;
        ((ISetInheritanceParent)this).SetParent(null);
        if (_owner is { } owner) owner.PropertyChanged -= OwnerChanged;
        if (_controller is { } controller)
        {
            controller.IndicationInteraction -= Receive;
            controller.IndicationReset -= Reset;
        }
        _owner = null;
        _controller = null;
        Reset();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Disconnect();
        base.OnDetachedFromVisualTree(e);
    }

    private void ScopeChanged(object? sender, AnimationScopeChangedEventArgs e) => Reset();
    private void ResourcesChangedAtParent(object? sender, ResourcesChangedEventArgs e) =>
        ((ILogical)this).NotifyResourcesChanged(e);
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialInteraction.FocusFeedbackProperty && _controller?.HasSeparateFocusIndication == true)
        {
            // Surface adds/removes the focus-only indication when its mode changes.
            Reset();
        }
        if (e.Property == MaterialInteraction.FocusFeedbackProperty || e.Property == MaterialInteraction.FocusRingMetricsProperty
            || e.Property == MaterialInteraction.FocusRingOuterColorProperty || e.Property == MaterialInteraction.FocusRingInnerColorProperty)
            InvalidateRing();
    }

    private void Reset()
    {
        _motion?.Dispose();
        _motion = null;
        _progress = null;
        _lastAnimatedProgress = 0;
        _interactions.Clear();
        _currentInteraction = null;
        _actualFocusCount = _sentFocusCount = 0;
        _inner.Clear();
        _outer.Clear();
        InvalidateRing();
    }

    private void Receive(MaterialInteractionEvent interaction)
    {
        if (_owner is null) return;
        var insetRing = MaterialInteraction.GetFocusFeedback(_owner) == FocusFeedback.InsetRing;
        if (!insetRing && _controller?.HasSeparateFocusIndication != false) return;
        if (interaction is MaterialInteractionEvent.Focus or MaterialInteractionEvent.Unfocus)
        {
            // Fixed Core InputModeFilterInteractionSource creates these counters per collector.
            // Reset with that collector, including when an unload cancels a deferred Unfocus.
            var send = _actualFocusCount > 0 && _sentFocusCount > 0
                || _actualFocusCount == 0 && (MaterialInputMode.For(_owner)?.IsKeyboard ?? true);
            var delta = interaction is MaterialInteractionEvent.Focus ? 1 : -1;
            _actualFocusCount += delta;
            if (!send) return;
            _sentFocusCount += delta;
        }
        var wasFocused = _interactions.Any(e => e is MaterialInteractionEvent.Focus);
        switch (interaction)
        {
            case MaterialInteractionEvent.Focus or MaterialInteractionEvent.HoverEnter or MaterialInteractionEvent.DragStart:
                _interactions.Add(interaction); break;
            case MaterialInteractionEvent.Unfocus e: _interactions.Remove(e.Start); break;
            case MaterialInteractionEvent.HoverExit e: _interactions.Remove(e.Start); break;
            case MaterialInteractionEvent.DragStop e: _interactions.Remove(e.Start); break;
            case MaterialInteractionEvent.DragCancel e: _interactions.Remove(e.Start); break;
            default: return;
        }
        var current = _interactions.LastOrDefault();
        var focused = _interactions.Any(e => e is MaterialInteractionEvent.Focus);
        // Preserve RippleNode's nested condition, including focus/hover interleavings.
        if (!ReferenceEquals(_currentInteraction, current)
            && interaction is MaterialInteractionEvent.Focus or MaterialInteractionEvent.Unfocus
            && insetRing && wasFocused != focused)
        {
            var candidate = focused ? FocusingAnimation : UnfocusingAnimation;
            if (candidate is { } spec && double.IsFinite(spec.DampingRatio) && spec.DampingRatio >= 0
                && double.IsFinite(spec.Stiffness) && spec.Stiffness > 0)
            {
                _motion ??= AnimationScope.Get(this).CreateChild("Inset focus ring");
                if (_progress is null)
                {
                    _progress = new Animatable<float>(new FloatConverter(), 0f, VisibilityThresholds.Float);
                    _motion.SubscribeValue(_progress, value =>
                    {
                        // Compose's mutableStateOf uses structural equality. The runtime can
                        // publish the unchanged first-frame value; that is not a draw invalidation.
                        if (_lastAnimatedProgress == value) return;
                        _lastAnimatedProgress = value;
                        InvalidateRing();
                    });
                }
                var progress = _progress;
                var motion = _motion;
                _ = motion.Launch(ct => progress.AnimateToAsync(motion, focused ? 1f : 0f,
                    new ComposeAnimation.Runtime.SpringSpec<float>(spec.DampingRatio, spec.Stiffness, VisibilityThresholds.Float),
                    cancellationToken: ct), "Focus ring progress");
            }
        }
        else if (!ReferenceEquals(_currentInteraction, current) && !insetRing
            && interaction is MaterialInteractionEvent.Focus or MaterialInteractionEvent.Unfocus
            && _progress is { } progress && _motion is { } motion)
            _ = motion.Launch(ct => progress.SnapToAsync(0f, ct), "Clear focus ring progress");
        _currentInteraction = current;
    }

    public override void Render(DrawingContext context)
    {
        if (_owner is null || MaterialInteraction.GetFocusFeedback(_owner) != FocusFeedback.InsetRing
            || Progress <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var metrics = MaterialInteraction.GetFocusRingMetrics(_owner);
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        if (_captureDefaultColors || _paintGeometry != (Bounds.Size, scale))
        {
            _paintedDefaultInnerColor = DefaultInnerColor;
            _paintedDefaultOuterColor = DefaultOuterColor;
            _captureDefaultColors = false;
            _paintGeometry = (Bounds.Size, scale);
        }
        if ((MaterialInteraction.GetFocusRingInnerColor(_owner) ?? _paintedDefaultInnerColor) is { } inner)
        {
            _innerBrush.Color = inner;
            _inner.Draw(context, _innerBrush, Bounds.Size, Corners, scale,
                (float)metrics.InnerStrokeWidth * Progress, (float)metrics.InnerStrokeInset * Progress);
        }
        if ((MaterialInteraction.GetFocusRingOuterColor(_owner) ?? _paintedDefaultOuterColor) is { } outer)
        {
            _outerBrush.Color = outer;
            _outer.Draw(context, _outerBrush, Bounds.Size, Corners, scale,
                (float)metrics.OuterStrokeWidth * Progress, (float)metrics.OuterStrokeInset * Progress);
        }
    }
}
