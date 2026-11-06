using Avalonia;
using Avalonia.Automation;
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
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

/// <summary>Two content slots shared by the three NavigationDrawer.kt containers.</summary>
public abstract class NavigationDrawer : ContentControl
{
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec> OpenAnimationProperty = AvaloniaProperty.Register<NavigationDrawer, Foundation.Motion.SpringSpec>(nameof(OpenAnimation));
    internal Foundation.Motion.SpringSpec OpenAnimation { get => GetValue(OpenAnimationProperty); set => SetValue(OpenAnimationProperty, value); }
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec> CloseAnimationProperty = AvaloniaProperty.Register<NavigationDrawer, Foundation.Motion.SpringSpec>(nameof(CloseAnimation));
    internal Foundation.Motion.SpringSpec CloseAnimation { get => GetValue(CloseAnimationProperty); set => SetValue(CloseAnimationProperty, value); }
    public static readonly StyledProperty<object?> DrawerContentProperty = AvaloniaProperty.Register<NavigationDrawer, object?>(nameof(DrawerContent));
    public static readonly StyledProperty<IDataTemplate?> DrawerContentTemplateProperty = AvaloniaProperty.Register<NavigationDrawer, IDataTemplate?>(nameof(DrawerContentTemplate));
    public object? DrawerContent { get => GetValue(DrawerContentProperty); set => SetValue(DrawerContentProperty, value); }
    public IDataTemplate? DrawerContentTemplate { get => GetValue(DrawerContentTemplateProperty); set => SetValue(DrawerContentTemplateProperty, value); }
    internal DrawerState? AnimatedState => (this as DismissibleNavigationDrawer)?.State;
    internal bool Gestures => this is DismissibleNavigationDrawer { GesturesEnabled: true };
    private DrawerState? _attachedState;
    private NavigationDrawerLayout? _layout;
    private ContentPresenter? _sheet;
    private IPointer? _pointer;
    private Point _down, _last;
    private bool _dragging;
    private readonly VelocityTracker _velocity = new();
    private int _focusGeneration;
    private AnimationChildScope? _effects;
    private FlowDirection _gestureDirection;
    private int _gestureGeneration;

    protected NavigationDrawer()
    {
        AddHandler(PointerPressedEvent, PointerDown, RoutingStrategies.Tunnel);
        // Compose detects slop in the Main pass, after descendants can consume motion.
        AddHandler(PointerMovedEvent, PointerMove, RoutingStrategies.Bubble);
        AddHandler(PointerReleasedEvent, PointerUp, RoutingStrategies.Tunnel);
        AddHandler(PointerCaptureLostEvent, PointerLost);
        // Native Thumb reports ownership through DragStarted, not PointerMoved.Handled.
        AddHandler(Thumb.DragStartedEvent, (_, _) => CancelPointer(), RoutingStrategies.Bubble, true);
        AddHandler(KeyUpEvent, EscapeUp);
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        CancelPointer();
        if (_layout is not null) _layout.Owner = null;
        base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<NavigationDrawerLayout>("PART_Layout");
        _sheet = e.NameScope.Find<ContentPresenter>("PART_Drawer");
        if (_layout is not null) _layout.Owner = this;
        AttachState();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _effects = AnimationScope.Get(this).CreateChild("Drawer effects");
        AttachState();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelPointer();
        if (_attachedState is { } state) { state.PropertyChanged -= StateChanged; state.Detach(this); }
        _attachedState = null;
        _focusGeneration++;
        _effects?.Dispose();
        _effects = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DismissibleNavigationDrawer.StateProperty) AttachState();
        if ((change.Property == DismissibleNavigationDrawer.GesturesEnabledProperty || change.Property == FlowDirectionProperty)
            && _pointer is { } pointer && _effects is { } scope)
        {
            var generation = _gestureGeneration;
            _ = scope.Launch(async ct =>
            {
                // AnchoredDraggable.update resets pointer input when its committed enabled or
                // reverseDirection changes. Preserve the old animation's composition-frame sample.
                if (!await scope.WaitForNextFrameAsync(ct)) return;
                if (generation == _gestureGeneration && _pointer == pointer
                    && (!Gestures || FlowDirection != _gestureDirection)) CancelPointer();
            }, "Drawer gesture configuration");
        }
        if (change.Property == IsEffectivelyEnabledProperty) CancelPointer();
    }
    private void AttachState()
    {
        if (!this.IsAttachedToVisualTree() || _attachedState == AnimatedState) return;
        // Finish the input owned by the previous state before rebinding. A captured
        // move/release must never reach a replacement whose anchors are not laid out yet.
        CancelPointer();
        _focusGeneration++;
        if (_attachedState is { } old) { old.PropertyChanged -= StateChanged; old.Detach(this); }
        _attachedState = AnimatedState;
        if (_attachedState is { } state) { state.Attach(this); state.PropertyChanged += StateChanged; }
        _layout?.InvalidateArrange();
        if (AnimatedState?.IsOpen == true) FocusDrawer();
    }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        _layout?.InvalidateArrange();
        if (e.Property == DrawerState.IsOpenProperty)
        {
            if (AnimatedState?.IsOpen == true) FocusDrawer();
            else _focusGeneration++;
        }
    }
    private void FocusDrawer()
    {
        if (_effects is not { } scope) return;
        var generation = ++_focusGeneration;
        _ = scope.Launch(async ct =>
        {
            // LaunchedEffect(isOpen) runs after the next composition frame, including snapTo.
            if (!await scope.WaitForNextFrameAsync(ct)) return;
            if (generation == _focusGeneration && AnimatedState?.IsOpen == true && _sheet is not null)
                _sheet.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Focusable && c.IsEffectivelyVisible && c.IsEffectivelyEnabled)?.Focus();
        }, "Drawer focus after composition");
    }
    private void EscapeUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && AnimatedState is { IsOpen: true } state && _sheet is not null
            && e.Source is Visual source && (_sheet == source || _sheet.IsVisualAncestorOf(source)))
        { _ = state.CloseAsync(); e.Handled = true; }
    }
    internal void Dismiss(bool fromScrim)
    {
        if (AnimatedState is { IsOpen: true } state && (!fromScrim || Gestures) && state.Confirm(DrawerValue.Closed)) _ = state.CloseAsync();
    }
    private void PointerDown(object? sender, PointerPressedEventArgs e)
    {
        if (!Gestures || _pointer is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        // Positions in the mirrored drawer are already reversed. Track physical window X,
        // then apply anchoredDraggable's RTL direction once below.
        _pointer = e.Pointer; _down = _last = e.GetPosition(TopLevel.GetTopLevel(this));
        _gestureGeneration++;
        _gestureDirection = FlowDirection;
        _velocity.Reset(); _velocity.Add(e.Timestamp, (float)(_down.X * LayoutHelper.GetLayoutScale(this)));
        if (AnimatedState?.IsAnimationRunning == true) StartDrag(e);
    }
    private void StartDrag(PointerEventArgs e)
    {
        AnimatedState!.BeginDrag(); _dragging = true;
        e.Pointer.Capture(this); e.Handled = true;
    }
    private void PointerMove(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer || !Gestures || AnimatedState is not { } state) return;
        var point = e.GetPosition(TopLevel.GetTopLevel(this)); _velocity.Add(e.Timestamp, (float)(point.X * LayoutHelper.GetLayoutScale(this)));
        var total = point.X - _down.X;
        var slop = e.Pointer.Type == PointerType.Mouse ? .125 : 18;
        if (!_dragging && Math.Abs(total) <= slop) return;
        if (!_dragging) { StartDrag(e); _last = new Point(_down.X + Math.Sign(total) * slop, _down.Y); }
        state.DragBy((float)((point.X - _last.X) * LayoutHelper.GetLayoutScale(this) * (FlowDirection == FlowDirection.RightToLeft ? -1 : 1)));
        _last = point; e.Handled = true;
    }
    private void PointerUp(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var dragged = _dragging; _dragging = false; _pointer = null;
        if (!dragged) return;
        e.Pointer.Capture(null);
        e.Handled = true;
        var velocity = Math.Clamp(_velocity.Calculate(e.Timestamp), -float.MaxValue, float.MaxValue);
        _ = AnimatedState!.EndDrag(velocity * (FlowDirection == FlowDirection.RightToLeft ? -1 : 1));
    }
    private void PointerLost(object? sender, PointerCaptureLostEventArgs e) { if (e.Source == this && _pointer == e.Pointer) CancelPointer(); }
    private void CancelPointer()
    {
        var pointer = _pointer; var dragged = _dragging;
        _pointer = null; _dragging = false;
        // A potential parent drag does not own a descendant button's capture.
        if (pointer?.Captured == this) pointer.Capture(null);
        // AndroidX's pinned DragGestureNode.processDragCancel calls onDragStopped with zero
        // velocity. The locked desktop artifact does too; Core's Git source differs here.
        if (dragged && _attachedState is { } state && this.IsAttachedToVisualTree()) _ = state.EndDrag(0);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new DrawerPeer(this);
    private sealed class DrawerPeer(NavigationDrawer owner) : ControlAutomationPeer(owner), ISelectionProvider, IInvokeProvider
    {
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IReadOnlyList<AutomationPeer> GetSelection() => owner.GetVisualDescendants().OfType<NavigationDrawerItem>().Where(i => i.IsSelected)
            .Select(CreatePeerForElement).OfType<AutomationPeer>().ToArray();
        public void Invoke() => owner.Dismiss(false);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string? GetNameCore() => string.IsNullOrEmpty(AutomationProperties.GetName(owner)) ? "Navigation menu" : base.GetNameCore();
    }
}

public class PermanentNavigationDrawer : NavigationDrawer { }

public class DismissibleNavigationDrawer : NavigationDrawer
{
    public static readonly StyledProperty<DrawerState> StateProperty = AvaloniaProperty.Register<DismissibleNavigationDrawer, DrawerState>(nameof(State));
    public static readonly StyledProperty<bool> GesturesEnabledProperty = AvaloniaProperty.Register<DismissibleNavigationDrawer, bool>(nameof(GesturesEnabled), true);
    public DrawerState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public bool GesturesEnabled { get => GetValue(GesturesEnabledProperty); set => SetValue(GesturesEnabledProperty, value); }
    public DismissibleNavigationDrawer() => SetCurrentValue(StateProperty, new DrawerState());
}

public class ModalNavigationDrawer : DismissibleNavigationDrawer
{
    public static readonly StyledProperty<IBrush?> ScrimBrushProperty = AvaloniaProperty.Register<ModalNavigationDrawer, IBrush?>(nameof(ScrimBrush));
    public IBrush? ScrimBrush { get => GetValue(ScrimBrushProperty); set => SetValue(ScrimBrushProperty, value); }
}

internal sealed class DrawerScrim : Border
{
    private bool _pressed;
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) { _pressed = true; e.Pointer.Capture(this); e.Handled = true; }
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var invoke = _pressed && new Rect(Bounds.Size).Contains(e.GetPosition(this));
        _pressed = false; e.Pointer.Capture(null); e.Handled = true;
        if (invoke) this.FindAncestorOfType<NavigationDrawer>()?.Dismiss(true);
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { _pressed = false; base.OnPointerCaptureLost(e); }
    protected override AutomationPeer OnCreateAutomationPeer() => new ScrimPeer(this);
    private sealed class ScrimPeer(DrawerScrim owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        public void Invoke() => owner.FindAncestorOfType<NavigationDrawer>()?.Dismiss(true);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
        protected override string? GetNameCore() => string.IsNullOrEmpty(AutomationProperties.GetName(owner)) ? "Close drawer" : base.GetNameCore();
    }
}
