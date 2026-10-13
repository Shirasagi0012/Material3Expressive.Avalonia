using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Input;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Components;

/// <summary>A wide rail which expands over the current TopLevel, with a dismissible scrim.</summary>
public class ModalWideNavigationRail : WideNavigationRail
{
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec> EffectsAnimationProperty = AvaloniaProperty.Register<ModalWideNavigationRail, Foundation.Motion.SpringSpec>(nameof(EffectsAnimation));
    internal Foundation.Motion.SpringSpec EffectsAnimation { get => GetValue(EffectsAnimationProperty); set => SetValue(EffectsAnimationProperty, value); }
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec> OffsetAnimationProperty = AvaloniaProperty.Register<ModalWideNavigationRail, Foundation.Motion.SpringSpec>(nameof(OffsetAnimation));
    internal Foundation.Motion.SpringSpec OffsetAnimation { get => GetValue(OffsetAnimationProperty); set => SetValue(OffsetAnimationProperty, value); }
    public static readonly StyledProperty<bool> HideOnCollapseProperty = AvaloniaProperty.Register<ModalWideNavigationRail, bool>(nameof(HideOnCollapse));
    public static readonly StyledProperty<object?> ExpandedHeaderProperty = AvaloniaProperty.Register<ModalWideNavigationRail, object?>(nameof(ExpandedHeader));
    public static readonly StyledProperty<IDataTemplate?> ExpandedHeaderTemplateProperty = AvaloniaProperty.Register<ModalWideNavigationRail, IDataTemplate?>(nameof(ExpandedHeaderTemplate));
    public static readonly StyledProperty<double> ExpandedHeaderTopPaddingProperty = AvaloniaProperty.Register<ModalWideNavigationRail, double>(nameof(ExpandedHeaderTopPadding));
    public static readonly StyledProperty<IBrush?> ModalBackgroundProperty = AvaloniaProperty.Register<ModalWideNavigationRail, IBrush?>(nameof(ModalBackground));
    public static readonly StyledProperty<IBrush?> ModalForegroundProperty = AvaloniaProperty.Register<ModalWideNavigationRail, IBrush?>(nameof(ModalForeground));
    public static readonly StyledProperty<IBrush?> ScrimBrushProperty = AvaloniaProperty.Register<ModalWideNavigationRail, IBrush?>(nameof(ScrimBrush));
    public static readonly StyledProperty<CornerRadius> CollapsedCornerRadiusProperty = AvaloniaProperty.Register<ModalWideNavigationRail, CornerRadius>(nameof(CollapsedCornerRadius));
    public static readonly StyledProperty<CornerRadius> ExpandedCornerRadiusProperty = AvaloniaProperty.Register<ModalWideNavigationRail, CornerRadius>(nameof(ExpandedCornerRadius));
    public static readonly StyledProperty<bool> ShouldDismissOnBackPressProperty = AvaloniaProperty.Register<ModalWideNavigationRail, bool>(nameof(ShouldDismissOnBackPress), true);
    public bool HideOnCollapse { get => GetValue(HideOnCollapseProperty); set => SetValue(HideOnCollapseProperty, value); }
    public object? ExpandedHeader { get => GetValue(ExpandedHeaderProperty); set => SetValue(ExpandedHeaderProperty, value); }
    public IDataTemplate? ExpandedHeaderTemplate { get => GetValue(ExpandedHeaderTemplateProperty); set => SetValue(ExpandedHeaderTemplateProperty, value); }
    public double ExpandedHeaderTopPadding { get => GetValue(ExpandedHeaderTopPaddingProperty); set => SetValue(ExpandedHeaderTopPaddingProperty, value); }
    public IBrush? ModalBackground { get => GetValue(ModalBackgroundProperty); set => SetValue(ModalBackgroundProperty, value); }
    public IBrush? ModalForeground { get => GetValue(ModalForegroundProperty); set => SetValue(ModalForegroundProperty, value); }
    public IBrush? ScrimBrush { get => GetValue(ScrimBrushProperty); set => SetValue(ScrimBrushProperty, value); }
    public CornerRadius CollapsedCornerRadius { get => GetValue(CollapsedCornerRadiusProperty); set => SetValue(CollapsedCornerRadiusProperty, value); }
    public CornerRadius ExpandedCornerRadius { get => GetValue(ExpandedCornerRadiusProperty); set => SetValue(ExpandedCornerRadiusProperty, value); }
    public bool ShouldDismissOnBackPress { get => GetValue(ShouldDismissOnBackPressProperty); set => SetValue(ShouldDismissOnBackPressProperty, value); }

    private Grid? _templateRoot;
    private Control? _inline;
    private ModalWideNavigationRailOverlay? _overlay;
    private WideNavigationRailLayout? _modalLayout;
    private ItemsPresenter? _presenter;
    private ContentPresenter? _expandedHeader;
    private Border? _expandedHeaderPadding, _scrim;
    private Control? _surface;
    private readonly Popup _popup = new();
    private TopLevel? _topLevel;
    private AnimationChildScope? _attachScope;
    private AnimatedState<float>? _position, _alpha;
    private Animatable<float>? _offset;
    private IDisposable? _offsetSubscription;
    private CancellationTokenSource? _flingCancellation;
    private bool _replacingState, _closingOverlay, _dragging, _moving;
    private float _width;
    private WideNavigationRailValue _anchorTarget;
    private IPointer? _pointer;
    private Point _down, _last;
    private readonly VelocityTracker _velocity = new();
    private Action? _restoreAfterDetach;
    internal bool IsMovingContent => _moving;
    internal bool IsModalVisible => _topLevel is not null;
    internal float PositionProgress => _position?.Value ?? (State.TargetValue == WideNavigationRailValue.Expanded ? 1 : 0);
    internal float ModalOffset => _offset?.Value ?? 0;
    internal float ScrimAlpha => _alpha?.Value ?? 0;
    internal override bool ComputeLayoutExpanded => false;
    internal override IEnumerable<WideNavigationRailItem> NavigationItems =>
        (_topLevel is null ? (Visual)this : _overlay!).GetVisualDescendants().OfType<WideNavigationRailItem>();
    protected override Type StyleKeyOverride => typeof(ModalWideNavigationRail);

    public ModalWideNavigationRail() => LogicalChildren.Add(_popup);

    static ModalWideNavigationRail() => AffectsMeasure<ModalWideNavigationRail>(HideOnCollapseProperty);
    protected override Size MeasureCore(Size availableSize)
    {
        ApplyTemplate(); // A hidden placeholder must still construct its modal host.
        return HideOnCollapse ? default : base.MeasureCore(availableSize);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        CloseOverlay(false);
        _templateRoot?.Children.Remove(_popup);
        if (_overlay is not null) { _overlay.DismissRequested -= OnModalDismissRequested; _overlay.Closed -= OnModalClosed; }
        base.OnApplyTemplate(e);
        _templateRoot = e.NameScope.Find<Grid>("PART_Root");
        _templateRoot?.Children.Add(_popup);
        _inline = e.NameScope.Find<Control>("PART_Inline");
        _overlay = e.NameScope.Find<ModalWideNavigationRailOverlay>("PART_Overlay");
        _modalLayout = e.NameScope.Find<WideNavigationRailLayout>("PART_ModalLayout");
        _presenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
        _expandedHeader = e.NameScope.Find<ContentPresenter>("PART_ExpandedHeader");
        _expandedHeaderPadding = e.NameScope.Find<Border>("PART_ExpandedHeaderPadding");
        _surface = e.NameScope.Find<Control>("PART_ModalSurface");
        _scrim = e.NameScope.Find<Border>("PART_Scrim");
        if (_overlay is not null)
        {
            _overlay.Owner = this;
            _overlay.DismissRequested += OnModalDismissRequested;
            _overlay.Closed += OnModalClosed;
            _overlay.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, true);
            _overlay.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel, true);
            _overlay.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel, true);
            _overlay.PointerCaptureLost += OnPointerCaptureLost;
        }
        if (_scrim is not null)
        {
            _scrim.PointerReleased += OnScrimReleased;
            if (_scrim is ModalWideNavigationRailScrim scrim) scrim.Owner = this;
        }
        InitializeMotion();
        RefreshPresentation();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _restoreAfterDetach?.Invoke(); _restoreAfterDetach = null;
        base.OnAttachedToVisualTree(e);
        _attachScope = AnimationScope.Get(this).CreateChild("Modal wide rail");
        InitializeMotion();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CloseOverlay(false);
        _pendingDragSettle = null;
        _pendingDismiss = false;
        _position?.Dispose(); _alpha?.Dispose();
        _position = _alpha = null; _offset = null; _width = 0;
        _offsetSubscription = null;
        _attachScope?.Dispose(); _attachScope = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        // Base notifies expansion before this method replaces the anchored offset.
        var replacingState = change.Property == StateProperty;
        if (replacingState) _replacingState = true;
        try { base.OnPropertyChanged(change); }
        finally { if (replacingState) _replacingState = false; }
        if (change.Property == HeaderProperty || change.Property == HeaderTemplateProperty || change.Property == ExpandedHeaderProperty
            || change.Property == ExpandedHeaderTemplateProperty || change.Property == ExpandedHeaderTopPaddingProperty)
            UpdateHeader();
        if (change.Property == HideOnCollapseProperty) RefreshPresentation();
        if (change.Property == EffectsAnimationProperty && EffectsAnimation.Stiffness > 0)
        {
            if (_position is { } position && position.TargetValue != (State.TargetValue == WideNavigationRailValue.Expanded ? 1f : 0f)) UpdateExpansion();
            UpdateScrim();
        }
        if (change.Property == OffsetAnimationProperty && OffsetAnimation.Stiffness > 0)
        {
            if (_pendingDismiss) { _pendingDismiss = false; DismissFromScrim(); }
            else if (_pendingDragSettle is { } pending && pending.StateTarget == State.TargetValue && !_dragging)
                SettleDrag(pending.Target, pending.Velocity);
            else if (!_dragging && HideOnCollapse && _offset is { } offset
                && offset.TargetValue != (State.TargetValue == WideNavigationRailValue.Collapsed ? -_width : 0))
            { _pendingDragSettle = null; _ = AnimateOffset(State.TargetValue); }
        }
        if (change.Property == StateProperty)
        {
            _pendingDismiss = false;
            _pendingDragSettle = null;
            // Upstream remember(state) recreates the anchored state for a new hoisted state.
            _flingCancellation?.Cancel();
            if (_offset is not null) _ = _offset.StopAsync();
            _offsetSubscription?.Dispose(); _offsetSubscription = null;
            _offset = null; _width = 0; _anchorTarget = State.TargetValue;
            _overlay?.InvalidateMeasure();
        }
    }
    internal override void UpdateExpansion()
    {
        base.UpdateExpansion();
        // The channel's anchoredDrag mutation replaces the entire fling, including
        // its finally block awaiting the hoisted state animation.
        if (HideOnCollapse && !_dragging) _flingCancellation?.Cancel();
        if (!this.IsAttachedToVisualTree() || _position is null) return;
        var target = State.TargetValue == WideNavigationRailValue.Expanded ? 1f : 0f;
        var effects = EffectsAnimation;
        if (effects.Stiffness <= 0 || _position.TargetValue == target) return;
        _position.UpdateTarget(target, Spring(effects));
        // Replacing State discards the previous offset after this notification.
        if (!_replacingState && IsModalVisible && HideOnCollapse && !_dragging) _ = AnimateOffset(State.TargetValue);
    }
    private static SpringSpec<float> Spring(Foundation.Motion.SpringSpec spec)
    {
        return new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 0.01f);
    }
    private void InitializeMotion()
    {
        if (_position is not null || !this.IsAttachedToVisualTree()) return;
        _anchorTarget = State.TargetValue;
        var initial = State.TargetValue == WideNavigationRailValue.Expanded ? 1f : 0f;
        _position = AnimatedState.Create(this, initial, label: "Modal rail position");
        _position.ValueChanged += _ => RefreshPresentation();
        RefreshPresentation();
    }
    private void UpdateHeader()
    {
        if (_expandedHeader is null) return;
        _expandedHeader.ContentTemplate = ExpandedHeaderTemplate ?? HeaderTemplate;
        _expandedHeader.Content = ExpandedHeader ?? (Header is Control && HeaderTemplate is null ? null : Header);
        if (_expandedHeaderPadding is not null) _expandedHeaderPadding.Padding = new Thickness(0, ExpandedHeaderTopPadding, 0, 0);
    }
    private void RefreshPresentation()
    {
        if (_inline is not null) _inline.IsVisible = !HideOnCollapse;
        if (_overlay is null || _modalLayout is null || !this.IsAttachedToVisualTree()) return;
        var visible = PositionProgress != 0;
        _modalLayout.Expanded = HideOnCollapse || PositionProgress >= 0.3f;
        if (visible && _topLevel is null) OpenOverlay();
        else if (!visible && _topLevel is not null) CloseOverlay(true);
        if (visible) UpdateScrim();
    }
    private void OpenOverlay()
    {
        if (_templateRoot is null || _overlay is null || _presenter is null || RailLayout is null || _modalLayout is null) return;
        if (Header is Control && HeaderTemplate is null && ExpandedHeader is null && ExpandedHeaderTemplate is null)
            throw new InvalidOperationException("A Control header needs a separate ExpandedHeader. Use HeaderTemplate to create both instances from data.");
        if (Header is Control && ReferenceEquals(Header, ExpandedHeader))
            throw new InvalidOperationException("Header and ExpandedHeader must be separate Control instances.");
        UpdateHeader();
        var initialAnchor = _anchorTarget;
        _anchorTarget = State.TargetValue;
        _topLevel = TopLevel.GetTopLevel(this)!;
        _templateRoot.Children.Remove(_overlay);
        _overlay.IsVisible = true;
        _overlay.Open(_popup, this, focusContent: false);
        ((ISetInheritanceParent)_overlay).SetParent(this);
        // Only the retained rail uses movableContentOf upstream. Hidden content is
        // composed afresh: reattachment initializes item motion at its current targets.
        MoveItems(RailLayout, _modalLayout, !HideOnCollapse);
        var initialAlpha = HideOnCollapse ? (initialAnchor == WideNavigationRailValue.Expanded ? 1f : 0f) : (PositionProgress >= .3f ? 1f : 0f);
        _alpha?.Dispose();
        _alpha = AnimatedState.Create(this, initialAlpha, label: "Modal rail scrim");
        _alpha.ValueChanged += value => { if (_scrim is not null) _scrim.Opacity = Math.Clamp(value, 0, 1); };
        if (_scrim is not null) _scrim.Opacity = Math.Clamp(_alpha.Value, 0, 1);
        // ModalWideNavigationRailState remains remembered while hidden content is absent.
        // Keep its anchors/offset across closes so reopening slides from the collapsed anchor.
        if (!HideOnCollapse) { _width = 0; _offset = null; }
        else if (_offset is not null) _ = AnimateOffset(State.TargetValue);
        _overlay.InvalidateMeasure();
    }
    private void MoveItems(WideNavigationRailLayout from, WideNavigationRailLayout to, bool preserveMotion)
    {
        if (_presenter is null) return;
        _moving = preserveMotion;
        try
        {
            from.Children.Remove(_presenter);
            from.Children.Add(new Panel());
            to.Children.RemoveAt(1);
            to.Children.Add(_presenter);
        }
        finally { _moving = false; }
    }
    private void CloseOverlay(bool restoreFocus)
    {
        if (_topLevel is null || _overlay is null) return;
        _flingCancellation?.Cancel();
        var pointer = _pointer;
        _pointer = null; _dragging = false;
        pointer?.Capture(null);
        _closingOverlay = true;
        if (!restoreFocus && TopLevel.GetTopLevel(this) is null)
        {
            // Parent detach notifications precede child notifications. Reattach only
            // after that traversal, so feedback leases never see a half-detached tree.
            _overlay.Close(restoreFocus);
            _overlay.IsVisible = false;
            _restoreAfterDetach = () =>
            {
                if (_modalLayout is not null && RailLayout is not null) MoveItems(_modalLayout, RailLayout, false);
                if (_overlay.Parent is null) _templateRoot?.Children.Add(_overlay);
            };
            Dispatcher.UIThread.Post(() => { _restoreAfterDetach?.Invoke(); _restoreAfterDetach = null; });
        }
        else
        {
            if (_modalLayout is not null && RailLayout is not null) MoveItems(_modalLayout, RailLayout, restoreFocus && !HideOnCollapse);
            _overlay.Close(restoreFocus);
            _overlay.IsVisible = false;
            _templateRoot?.Children.Add(_overlay);
        }
        _topLevel = null; _closingOverlay = false;
        _alpha?.Dispose(); _alpha = null;
        if (!HideOnCollapse)
        {
            if (_offset is not null) _ = _offset.StopAsync();
            _offsetSubscription?.Dispose(); _offsetSubscription = null;
            _offset = null; _width = 0;
        }
    }
    private void UpdateScrim()
    {
        if (!this.IsAttachedToVisualTree() || _alpha is null || EffectsAnimation.Stiffness <= 0) return;
        var visible = HideOnCollapse ? _anchorTarget == WideNavigationRailValue.Expanded : PositionProgress >= .3f;
        var target = visible ? 1f : 0f;
        // Repeated samples share one target. AnimatedState keeps that target until it changes.
        if (_alpha.TargetValue == target) return;
        _alpha.UpdateTarget(target, Spring(EffectsAnimation));
    }
    internal void UpdateAnchors(double measuredWidth)
    {
        if (!this.IsAttachedToVisualTree() || _attachScope is not { } scope
            || scope.Lifetime.IsCancellationRequested) return;
        var width = (float)MathUtils.DipToPx(measuredWidth, LayoutHelper.GetLayoutScale(this));
        if (_width == width) return;
        _width = width;
        if (_offset is null)
        {
            _offset = new Animatable<float>(new FloatConverter(), HideOnCollapse && _anchorTarget == WideNavigationRailValue.Collapsed ? -width : 0, VisibilityThresholds.Float);
            _offsetSubscription = scope.SubscribeValue(_offset, _ => _overlay?.InvalidateArrange());
            if (HideOnCollapse) _ = AnimateOffset(State.TargetValue);
        }
        else if (!_dragging)
        {
            if (_offset.IsRunning) _ = AnimateOffset(_anchorTarget);
            else _ = _offset.SnapToAsync(HideOnCollapse && _anchorTarget == WideNavigationRailValue.Collapsed ? -width : 0);
        }
    }
    private Task AnimateOffset(WideNavigationRailValue target)
    {
        if (!this.IsAttachedToVisualTree() || _offset is null || _attachScope is not { } scope
            || scope.Lifetime.IsCancellationRequested) return Task.CompletedTask;
        _flingCancellation?.Cancel();
        _anchorTarget = target;
        UpdateScrim();
        var offsetAnimation = OffsetAnimation;
        if (offsetAnimation.Stiffness <= 0) return Task.CompletedTask;
        var track = _offset;
        // Programmatic AnchoredDraggable.animateTo deliberately retains overshoot.
        track.UpdateBounds(default);
        return scope.Launch(async ct => await track.AnimateToAsync(scope, HideOnCollapse && target == WideNavigationRailValue.Collapsed ? -_width : 0,
            Spring(offsetAnimation), initialVelocity: track.Velocity, cancellationToken: ct), "Modal rail offset");
    }
    private void OnScrimReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, _scrim) || e.InitialPressMouseButton != MouseButton.Left || ((Vector)(_down - e.GetPosition(_topLevel))).Length > 18) return;
        e.Handled = true;
        DismissFromScrim();
    }
    private bool _pendingDismiss;
    internal void DismissFromScrim()
    {
        if (!this.IsAttachedToVisualTree() || _attachScope is not { } scope
            || scope.Lifetime.IsCancellationRequested) return;
        if (HideOnCollapse && OffsetAnimation.Stiffness <= 0) { _pendingDismiss = true; return; }
        _pendingDismiss = false;
        _ = scope.Launch(async ct =>
        {
            if (HideOnCollapse)
            {
                await AnimateOffset(WideNavigationRailValue.Collapsed);
                await CollapseStateFromAnimationAsync(ct);
            }
            else await State.CollapseAsync(ct);
        }, "Modal rail dismiss");
    }
    private Task CollapseStateFromAnimationAsync(CancellationToken cancellationToken) =>
        State.CollapseAsync(cancellationToken);
    private void OnModalDismissRequested(object? sender, DismissRequestedEventArgs e)
    {
        // Rail's common Escape path remains independent of its platform Back setting.
        if (e.Reason == DismissReason.Escape || e.Reason == DismissReason.Back && ShouldDismissOnBackPress)
            _ = State.CollapseAsync();
    }
    private void OnModalClosed(object? sender, EventArgs e)
    {
        if (!_closingOverlay) CloseOverlay(false);
    }
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_overlay).Properties.IsLeftButtonPressed || _pointer is not null) return;
        _down = _last = e.GetPosition(_topLevel);
        if (!HideOnCollapse || e.Source is not Visual visual || _surface is null || !(visual == _surface || _surface.IsVisualAncestorOf(visual))) return;
        _pointer = e.Pointer;
        _velocity.Reset(); _velocity.Add(e.Timestamp, (float)_down.X);
        if (_offset?.IsRunning == true) StartDrag(e);
    }
    private void StartDrag(PointerEventArgs e)
    {
        _pendingDragSettle = null;
        _flingCancellation?.Cancel();
        _dragging = true;
        if (_offset is not null) _ = _offset.StopAsync();
        e.Pointer.Capture(_surface); e.Handled = true;
    }
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer || _offset is null) return;
        var point = e.GetPosition(_topLevel);
        _velocity.Add(e.Timestamp, (float)point.X);
        var direction = FlowDirection == FlowDirection.RightToLeft ? -1 : 1;
        var slop = e.Pointer.Type == PointerType.Mouse ? .125 : 18;
        var total = point.X - _down.X;
        if (!_dragging && Math.Abs(total) <= slop) return;
        if (!_dragging) { StartDrag(e); _last = new Point(_down.X + Math.Sign(total) * slop, _down.Y); }
        var value = Math.Clamp(ModalOffset + (float)((point.X - _last.X) * direction * LayoutHelper.GetLayoutScale(this)), -_width, 0);
        _ = _offset.SnapToAsync(value);
        _anchorTarget = Math.Abs(value + _width) < Math.Abs(value) ? WideNavigationRailValue.Collapsed : WideNavigationRailValue.Expanded;
        UpdateScrim(); _last = point; e.Handled = true;
    }
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var dragged = _dragging;
        _dragging = false; _pointer = null;
        e.Pointer.Capture(null);
        if (!dragged) return;
        e.Handled = true;
        var density = (float)LayoutHelper.GetLayoutScale(this);
        var velocity = _velocity.Calculate(e.Timestamp) * density * (FlowDirection == FlowDirection.RightToLeft ? -1 : 1);
        var target = velocity == 0 ? (ModalOffset < -_width / 2 ? WideNavigationRailValue.Collapsed : WideNavigationRailValue.Expanded)
            : Math.Abs(velocity) >= 125 * density ? (velocity < 0 ? WideNavigationRailValue.Collapsed : WideNavigationRailValue.Expanded)
            : velocity < 0 ? (Math.Abs(ModalOffset) >= _width * .5f ? WideNavigationRailValue.Collapsed : WideNavigationRailValue.Expanded)
            : (Math.Abs(ModalOffset + _width) >= _width * .5f ? WideNavigationRailValue.Expanded : WideNavigationRailValue.Collapsed);
        SettleDrag(target, velocity);
    }
    private (WideNavigationRailValue Target, float Velocity, WideNavigationRailValue StateTarget)? _pendingDragSettle;
    private void SettleDrag(WideNavigationRailValue target, float velocity)
    {
        if (!this.IsAttachedToVisualTree() || _offset is not { } track || _attachScope is not { } scope
            || scope.Lifetime.IsCancellationRequested) return;
        var offsetAnimation = OffsetAnimation;
        if (offsetAnimation.Stiffness <= 0) { _pendingDragSettle = (target, velocity, State.TargetValue); return; }
        _pendingDragSettle = null;
        _flingCancellation?.Cancel();
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(scope.Lifetime);
        _flingCancellation = cancellation;
        _ = scope.Launch(async ct =>
        {
            try
            {
                // SnapFlingBehavior.animateWithTarget animates consumed deltas. Its
                // anchoredDrag has no explicit target: the closest anchor follows offset.
                var distance = (target == WideNavigationRailValue.Collapsed ? -_width : 0) - track.Value;
                var consumedUpToNow = 0f;
                var animation = new AnimationState<float>(new FloatConverter(), 0f, AnimationVector.From([velocity]));
                try
                {
                    await SuspendAnimation.AnimateToAsync(scope, animation, distance,
                        Spring(offsetAnimation),
                        sequentialAnimation: velocity != 0, block: frame =>
                        {
                            var realValue = distance == 0 ? 0 : distance > 0 ? Math.Min(frame.Value, distance) : Math.Max(frame.Value, distance);
                            var delta = realValue - consumedUpToNow;
                            var value = Math.Clamp(track.Value + delta, -_width, 0);
                            var consumed = value - track.Value;
                            _ = track.SnapToAsync(value);
                            _anchorTarget = Math.Abs(value + _width) < Math.Abs(value) ? WideNavigationRailValue.Collapsed : WideNavigationRailValue.Expanded;
                            UpdateScrim();
                            if (Math.Abs(delta - consumed) > .5f || realValue != frame.Value) frame.CancelAnimation();
                            consumedUpToNow += consumed;
                        }, cancellationToken: cancellation.Token);
                }
                finally
                {
                    if (!cancellation.IsCancellationRequested && _anchorTarget == WideNavigationRailValue.Collapsed)
                        await CollapseStateFromAnimationAsync(cancellation.Token);
                }
            }
            finally
            {
                if (ReferenceEquals(_flingCancellation, cancellation)) _flingCancellation = null;
                cancellation.Dispose();
            }
        }, "Modal rail fling");
    }
    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_pointer != e.Pointer || !_dragging) return;
        _pointer = null; _dragging = false;
        SettleDrag(ModalOffset < -_width / 2 ? WideNavigationRailValue.Collapsed : WideNavigationRailValue.Expanded, 0);
    }
}

internal sealed class ModalWideNavigationRailOverlay : ModalPresenter
{
    internal ModalWideNavigationRail? Owner;
    public ModalWideNavigationRailOverlay() { Focusable = true; FocusAdorner = null; }
    protected override AutomationPeer OnCreateAutomationPeer() => new PanePeer(this);
    private sealed class PanePeer(Control owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
        protected override string GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : "Navigation rail";
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(availableSize);
        Owner?.UpdateAnchors(Children[1].DesiredSize.Width);
        // Canvas may measure a newly inserted child once with unbounded constraints.
        // The overlay always occupies the current client area, independent of its rail width.
        return new Size(double.IsFinite(availableSize.Width) ? availableSize.Width : TopLevel.GetTopLevel(this)?.ClientSize.Width ?? 0,
            double.IsFinite(availableSize.Height) ? availableSize.Height : TopLevel.GetTopLevel(this)?.ClientSize.Height ?? 0);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Children[0].Arrange(new Rect(finalSize));
        var surface = Children[1];
        var density = (float)LayoutHelper.GetLayoutScale(this);
        // draggableAnchors reverses RTL before roundToInt; Avalonia mirrors afterwards.
        var direction = FlowDirection == FlowDirection.RightToLeft ? -1 : 1;
        var x = direction * (double)MathUtils.RoundToInt(direction * (Owner?.ModalOffset ?? 0));
        surface.Arrange(new Rect(x / density, 0, surface.DesiredSize.Width, finalSize.Height));
        return finalSize;
    }
}

internal sealed class ModalWideNavigationRailScrim : Border
{
    internal ModalWideNavigationRail? Owner;
    protected override AutomationPeer OnCreateAutomationPeer() => new ClosePeer(this);
    private sealed class ClosePeer(ModalWideNavigationRailScrim owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        public void Invoke() => owner.Owner?.DismissFromScrim();
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
        protected override string GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : "Close rail";
    }
}
