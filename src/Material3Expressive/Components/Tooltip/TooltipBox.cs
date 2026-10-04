using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Hosts an anchor and presents its tooltip using the fixed Compose desktop behavior.</summary>
public class TooltipBox : ContentControl
{
    public static readonly StyledProperty<object?> TooltipProperty = AvaloniaProperty.Register<TooltipBox, object?>(nameof(Tooltip));
    public static readonly StyledProperty<IDataTemplate?> TooltipTemplateProperty = AvaloniaProperty.Register<TooltipBox, IDataTemplate?>(nameof(TooltipTemplate));
    public static readonly StyledProperty<TooltipState?> StateProperty = AvaloniaProperty.Register<TooltipBox, TooltipState?>(nameof(State));
    public static readonly StyledProperty<TooltipAnchorPosition> AnchorPositionProperty = AvaloniaProperty.Register<TooltipBox, TooltipAnchorPosition>(nameof(AnchorPosition));
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<TooltipBox, double>(nameof(Spacing), TooltipDefaults.Spacing);
    public static readonly StyledProperty<ITooltipPositionProvider?> PositionProviderProperty = AvaloniaProperty.Register<TooltipBox, ITooltipPositionProvider?>(nameof(PositionProvider));
    public static readonly StyledProperty<bool> EnableUserInputProperty = AvaloniaProperty.Register<TooltipBox, bool>(nameof(EnableUserInput), true);
    public static readonly StyledProperty<bool> HasActionProperty = AvaloniaProperty.Register<TooltipBox, bool>(nameof(HasAction));

    public object? Tooltip { get => GetValue(TooltipProperty); set => SetValue(TooltipProperty, value); }
    public IDataTemplate? TooltipTemplate { get => GetValue(TooltipTemplateProperty); set => SetValue(TooltipTemplateProperty, value); }
    public TooltipState State { get => GetValue(StateProperty) ?? _implicitState; set => SetValue(StateProperty, value); }
    public TooltipAnchorPosition AnchorPosition { get => GetValue(AnchorPositionProperty); set => SetValue(AnchorPositionProperty, value); }
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    public ITooltipPositionProvider? PositionProvider { get => GetValue(PositionProviderProperty); set => SetValue(PositionProviderProperty, value); }
    public bool EnableUserInput { get => GetValue(EnableUserInputProperty); set => SetValue(EnableUserInputProperty, value); }
    public bool HasAction { get => GetValue(HasActionProperty); set => SetValue(HasActionProperty, value); }

    /// <summary>An outside click requests dismissal. With a handler, the application owns dismissal.</summary>
    public event EventHandler? DismissRequested;

    private readonly TooltipState _implicitState = new(isPersistent: false);
    private readonly Popup _popup;
    private readonly TooltipPresenter _presenter = new();
    private readonly Border _layer;
    private Grid? _root;
    private TopLevel? _topLevel;
    private VisualAnimationScope? _scope, _stateScope;
    private TooltipState? _activeState;
    private Transition<bool>? _transition;
    private float _scale = .8f, _alpha;
    private bool _receivedKeyboardFocus;
    private IPointer? _heldPointer;
    private CancellationTokenSource? _press;
    private TaskCompletionSource? _release;
    private bool _longPressed;
    private Size _lastWindowSize;

    internal bool IsPresented => _popup.IsOpen;
    internal Control Presentation => _layer;
    internal float AppearanceScale => _scale;
    internal float AppearanceAlpha => _alpha;

    public TooltipBox()
    {
        _layer = new Border { Child = _presenter, ClipToBounds = false, RenderTransformOrigin = RelativePoint.Center };
        // graphicsLayer alpha applies once to the whole surface, including overlapping children.
        var presentationRoot = new Border { Child = _layer };
        RenderOptions.SetRequiresFullOpacityHandling(presentationRoot, true);
        _popup = new Popup { Child = presentationRoot, ShouldUseOverlayLayer = true, IsLightDismissEnabled = false,
            TakesFocusFromNativeControl = false, Placement = PlacementMode.Custom,
            CustomPopupPlacementCallback = PlacePopup };
        _popup.Styles.Add(new Style(selector => selector.OfType<OverlayPopupHost>())
            { Setters = { new Setter(ClipToBoundsProperty, false) } });
        _popup.Styles.Add(new Style(selector => selector.OfType<OverlayPopupHost>().Template().OfType<LayoutTransformControl>())
            { Setters = { new Setter(ClipToBoundsProperty, false) } });
        LogicalChildren.Add(_popup);
        _presenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(TooltipProperty));
        _presenter.Bind(ContentPresenter.ContentTemplateProperty, this.GetObservable(TooltipTemplateProperty));
        AddHandler(PointerPressedEvent, OnAnchorPressed, RoutingStrategies.Tunnel, true);
        AddHandler(PointerReleasedEvent, OnAnchorReleased, RoutingStrategies.Tunnel, true);
        AddHandler(PointerCaptureLostEvent, OnAnchorCaptureLost, RoutingStrategies.Bubble, true);
        AddHandler(GotFocusEvent, OnAnchorFocused, RoutingStrategies.Bubble, true);
        AddHandler(LostFocusEvent, OnAnchorLostFocus, RoutingStrategies.Bubble, true);
        AddHandler(KeyDownEvent, OnAnchorKey, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnAnchorKey, RoutingStrategies.Tunnel);
        LayoutUpdated += (_, _) => UpdatePosition();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _popup.Close();
        _root?.Children.Remove(_popup);
        base.OnApplyTemplate(e);
        _root = e.NameScope.Find<Grid>("PART_Root");
        _root?.Children.Add(_popup);
        UpdatePresentation();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(PointerPressedEvent, OnOutsidePressed, RoutingStrategies.Tunnel, true);
        _scope = VisualAnimationScope.Create(this);
        AttachState();
        // The owner's Window template establishes the popup layer during its first layout.
        Dispatcher.UIThread.Post(() => { if (this.IsAttachedToVisualTree()) { ApplyTemplate(); UpdatePresentation(); } });
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(PointerPressedEvent, OnOutsidePressed); _topLevel = null;
        ReleasePointer();
        DetachState();
        _popup.Close();
        _scope?.Dispose(); _scope = null;
        _receivedKeyboardFocus = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TooltipProperty)
        {
            if (change.OldValue is ILogical old) LogicalChildren.Remove(old);
            if (change.NewValue is ILogical child) LogicalChildren.Add(child);
        }
        if (change.Property == StateProperty && _scope is not null)
        {
            ReleasePointer(); DetachState(); AttachState();
        }
        if (change.Property == EnableUserInputProperty && !EnableUserInput) ReleasePointer();
        if (change.Property == AnchorPositionProperty || change.Property == SpacingProperty || change.Property == PositionProviderProperty
            || change.Property == FlowDirectionProperty || change.Property == RenderTransformProperty
            || change.Property == RenderTransformOriginProperty || change.Property == HasMirrorTransformProperty) UpdatePosition(force: true);
    }

    private void AttachState()
    {
        _activeState = State;
        _activeState.PropertyChanged += OnStateChanged;
        var scope = _stateScope = _scope!.CreateChild("Tooltip state");
        var transition = _transition = scope.CreateTransition(_activeState.Transition, "Tooltip appearance");
        var scale = _transition.AnimateFloat(visible => visible ? 1f : .8f,
            _ => Spec(Sys.Motion.Spring.SpatialFast, MotionScheme.Expressive.FastSpatial), "Tooltip scale");
        var alpha = _transition.AnimateFloat(visible => visible ? 1f : 0f,
            _ => Spec(Sys.Motion.Spring.EffectsFast, MotionScheme.Expressive.FastEffects), "Tooltip alpha");
        scope.SubscribeValue(scale, value => { _scale = value; UpdateAppearance(); });
        scope.SubscribeValue(alpha, value => { _alpha = value; UpdateAppearance(); });
        _ = scope.Launch(ct => transition.RunAsync(ct), "Tooltip transition");
        UpdatePresentation();
    }

    private SpringSpec<float> Spec(MaterialTokenKey<Foundation.Motion.SpringSpec> key, Foundation.Motion.SpringSpec fallback)
    {
        var spec = TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is Foundation.Motion.SpringSpec resolved ? resolved : fallback;
        return new(spec.DampingRatio, spec.Stiffness, .01f);
    }

    private void DetachState()
    {
        if (_activeState is not null) { _activeState.PropertyChanged -= OnStateChanged; _activeState.OnDispose(); }
        _stateScope?.Dispose(); _stateScope = null;
        _transition?.Dispose(); _transition = null;
        _activeState = null;
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TooltipState.IsVisible)) UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        if (_root is null || _topLevel is null || _activeState is null) return;
        if (_activeState.IsVisible)
        {
            _popup.PlacementTarget = this;
            if (!_popup.IsOpen) _popup.Open();
            UpdatePosition();
        }
        else _popup.Close();
    }

    private void UpdateAppearance()
    {
        _layer.Opacity = _alpha;
        _layer.RenderTransform = new ScaleTransform(_scale, _scale);
    }

    private Rect AnchorBounds()
    {
        // Core Popup.onPlaced uses positionInWindow() plus the parent's local size,
        // not the axis-aligned bounds of its transformed corners.
        // Avalonia implements RTL with automatic mirror transforms, while Core's
        // layout coordinates still start at the physical left of the node.
        var mirrored = false;
        for (Visual? visual = this; visual is not null && visual != _topLevel; visual = visual.GetVisualParent())
            mirrored ^= visual.HasMirrorTransform;
        var origin = mirrored ? new Point(Bounds.Width, 0) : default;
        return new Rect(this.TranslatePoint(origin, _topLevel!)!.Value, Bounds.Size);
    }

    private void UpdatePosition(bool force = false)
    {
        if (!_popup.IsOpen || _topLevel is null) return;
        var bounds = AnchorBounds();
        if (_lastWindowSize != _topLevel.ClientSize) force = true;
        _lastWindowSize = _topLevel.ClientSize;
        if (force) _popup.PlacementRect = null;
        _popup.PlacementRect = bounds;
    }

    private void PlacePopup(CustomPopupPlacement placement)
    {
        var topLevel = _topLevel!;
        var density = topLevel.RenderScaling;
        int Pixel(double dip) => (int)MathF.Floor((float)(dip * density) + .5f);
        var bounds = AnchorBounds();
        var anchor = new PixelRect(Pixel(bounds.Left), Pixel(bounds.Top), Pixel(bounds.Width), Pixel(bounds.Height));
        var provider = PositionProvider ?? TooltipDefaults.CreatePositionProvider(AnchorPosition, Spacing, density);
        var point = provider.CalculatePosition(anchor, new(Pixel(topLevel.ClientSize.Width), Pixel(topLevel.ClientSize.Height)),
            FlowDirection == FlowDirection.RightToLeft, new(Pixel(placement.PopupSize.Width), Pixel(placement.PopupSize.Height)));
        // Avalonia mirrors anchor/gravity after the callback. Undo only that positioning mirror;
        // tooltip content still inherits the requested layout direction.
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        placement.AnchorRectangle = new Rect(point.X / density, point.Y / density, 0, 0);
        placement.Anchor = rtl ? PopupAnchor.TopRight : PopupAnchor.TopLeft;
        placement.Gravity = rtl ? PopupGravity.BottomLeft : PopupGravity.BottomRight;
        placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.None;
    }

    private void OnOutsidePressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_popup.IsOpen || !_activeState!.IsVisible || _layer.Bounds.Contains(e.GetPosition(_layer))) return;
        if (DismissRequested is { } callback) callback(this, EventArgs.Empty);
        else _activeState.Dismiss();
        // Core's nonfocusable Popup passes the same press on to the underlying anchor/content.
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (EnableUserInput && e.Pointer.Type == PointerType.Mouse) ShowFromInput(MutatePriority.UserInput);
    }
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (EnableUserInput && e.Pointer.Type == PointerType.Mouse && !State.IsPersistent) State.Dismiss();
    }
    private void ShowFromInput(MutatePriority priority)
    {
        if (_stateScope is { IsEnding: false } scope)
            _ = scope.Launch(ct => State.ShowAsync(priority, ct), "Tooltip input");
    }

    private void OnAnchorFocused(object? sender, RoutedEventArgs e)
    {
        if (!EnableUserInput) return;
        _receivedKeyboardFocus = true; ShowFromInput(MutatePriority.PreventUserInput);
    }
    private void OnAnchorLostFocus(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (EnableUserInput && !IsKeyboardFocusWithin && _receivedKeyboardFocus && State.IsVisible)
            { _receivedKeyboardFocus = false; State.Dismiss(); }
        });
    }
    private void OnAnchorKey(object? sender, KeyEventArgs e)
    {
        if (!EnableUserInput || !State.IsVisible) return;
        // Pinned Core consumes Tab for hasAction, but its Popup is still nonfocusable.
        if (HasAction && e.Key == Key.Tab) e.Handled = true;
        if (e.Key == Key.Escape && e.RoutedEvent == KeyDownEvent)
        { _receivedKeyboardFocus = false; State.Dismiss(); e.Handled = true; }
    }

    private void OnAnchorPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!EnableUserInput || e.Pointer.Type is not (PointerType.Touch or PointerType.Pen) || _heldPointer is not null
            || _stateScope is not { IsEnding: false } scope) return;
        _heldPointer = e.Pointer;
        var press = _press = new CancellationTokenSource();
        var release = _release = new TaskCompletionSource();
        var state = State;
        _ = scope.Launch(async ct =>
        {
            using var delay = CancellationTokenSource.CreateLinkedTokenSource(ct, press.Token);
            await Task.Delay(this.GetPlatformSettings()?.HoldWaitDuration ?? TimeSpan.FromMilliseconds(500), delay.Token);
            _longPressed = true;
            try { await state.ShowAsync(MutatePriority.PreventUserInput, ct); }
            finally
            {
                if (state.IsVisible)
                {
                    await release.Task.WaitAsync(ct);
                    state.Dismiss();
                }
            }
        }, "Tooltip long press");
    }
    private void OnAnchorReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _heldPointer) return;
        if (_longPressed) e.Handled = true;
        ReleasePointer();
    }
    private void OnAnchorCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (e.Pointer == _heldPointer) ReleasePointer();
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Pointer == _heldPointer && !new Rect(Bounds.Size).Contains(e.GetPosition(this))) ReleasePointer();
    }
    private void ReleasePointer()
    {
        _heldPointer = null; _longPressed = false;
        _press?.Cancel(); _press?.Dispose(); _press = null;
        _release?.TrySetResult(); _release = null;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new TooltipBoxPeer(this);
    private sealed class TooltipBoxPeer(TooltipBox owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        public void Invoke() { if (owner.EnableUserInput) owner.ShowFromInput(MutatePriority.Default); }
    }
}

internal sealed class TooltipPresenter : ContentPresenter
{
    public TooltipPresenter()
    {
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Assertive);
        AutomationProperties.SetName(this, "tooltip");
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer(TooltipPresenter owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolTip;
    }
}
