/* SwipeToDismissBox.kt and desktop Draggable.kt: gestures and dismissal effects.
 * Copyright 2023-2026 The Android Open Source Project; Apache-2.0. */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

public sealed class SwipeDismissedEventArgs(SwipeToDismissBoxValue direction) : RoutedEventArgs(SwipeToDismissBox.DismissedEvent)
{
    public SwipeToDismissBoxValue Direction { get; } = direction;
}

/// <summary>A foreground content slot that slides horizontally over a stationary background.</summary>
public class SwipeToDismissBox : ContentControl
{
    public static readonly StyledProperty<SwipeToDismissBoxState> StateProperty = AvaloniaProperty.Register<SwipeToDismissBox, SwipeToDismissBoxState>(nameof(State));
    public static readonly StyledProperty<object?> BackgroundContentProperty = AvaloniaProperty.Register<SwipeToDismissBox, object?>(nameof(BackgroundContent));
    public static readonly StyledProperty<IDataTemplate?> BackgroundContentTemplateProperty = AvaloniaProperty.Register<SwipeToDismissBox, IDataTemplate?>(nameof(BackgroundContentTemplate));
    public static readonly StyledProperty<bool> EnableDismissFromStartToEndProperty = AvaloniaProperty.Register<SwipeToDismissBox, bool>(nameof(EnableDismissFromStartToEnd), true);
    public static readonly StyledProperty<bool> EnableDismissFromEndToStartProperty = AvaloniaProperty.Register<SwipeToDismissBox, bool>(nameof(EnableDismissFromEndToStart), true);
    public static readonly StyledProperty<bool> GesturesEnabledProperty = AvaloniaProperty.Register<SwipeToDismissBox, bool>(nameof(GesturesEnabled), true);
    public static readonly RoutedEvent<SwipeDismissedEventArgs> DismissedEvent = RoutedEvent.Register<SwipeToDismissBox, SwipeDismissedEventArgs>(nameof(Dismissed), RoutingStrategies.Bubble);

    public SwipeToDismissBoxState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public object? BackgroundContent { get => GetValue(BackgroundContentProperty); set => SetValue(BackgroundContentProperty, value); }
    public IDataTemplate? BackgroundContentTemplate { get => GetValue(BackgroundContentTemplateProperty); set => SetValue(BackgroundContentTemplateProperty, value); }
    public bool EnableDismissFromStartToEnd { get => GetValue(EnableDismissFromStartToEndProperty); set => SetValue(EnableDismissFromStartToEndProperty, value); }
    public bool EnableDismissFromEndToStart { get => GetValue(EnableDismissFromEndToStartProperty); set => SetValue(EnableDismissFromEndToStartProperty, value); }
    public bool GesturesEnabled { get => GetValue(GesturesEnabledProperty); set => SetValue(GesturesEnabledProperty, value); }
    public event EventHandler<SwipeDismissedEventArgs>? Dismissed { add => AddHandler(DismissedEvent, value); remove => RemoveHandler(DismissedEvent, value); }

    private SwipeToDismissBoxState? _attachedState;
    private SwipeToDismissBoxLayout? _layout;
    private IPointer? _pointer;
    private Point _down, _last;
    private bool _dragging;
    private readonly VelocityTracker _velocity = new();
    private int _dismissGeneration;
    private AnimationChildScope? _effects;
    private bool CanDrag => GesturesEnabled && IsEffectivelyEnabled && State.SettledValue == SwipeToDismissBoxValue.Settled;

    public SwipeToDismissBox()
    {
        SetCurrentValue(StateProperty, new SwipeToDismissBoxState());
        AddHandler(PointerPressedEvent, PointerDown, RoutingStrategies.Tunnel);
        // Main-pass dragging gives an interactive descendant the first chance to consume motion.
        AddHandler(PointerMovedEvent, PointerMove, RoutingStrategies.Bubble);
        AddHandler(PointerReleasedEvent, PointerUp, RoutingStrategies.Tunnel);
        AddHandler(PointerCaptureLostEvent, PointerLost);
        // Native Thumb reports ownership through DragStarted, not PointerMoved.Handled.
        AddHandler(Thumb.DragStartedEvent, (_, _) => CancelPointer(), RoutingStrategies.Bubble, true);
    }
    protected override bool RegisterContentPresenter(ContentPresenter presenter) =>
        presenter.Name == "PART_BackgroundPresenter" || base.RegisterContentPresenter(presenter);
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_layout is not null) _layout.Owner = null;
        CancelPointer();
        base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<SwipeToDismissBoxLayout>("PART_Layout");
        if (_layout is not null) _layout.Owner = this;
        AttachState();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); AttachState(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelPointer();
        _dismissGeneration++;
        _effects?.Dispose();
        _effects = null;
        if (_attachedState is { } state) { state.PropertyChanged -= StateChanged; state.Detach(this); }
        _attachedState = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BackgroundContentProperty)
        {
            if (change.OldValue is ILogical oldChild) LogicalChildren.Remove(oldChild);
            if (change.NewValue is ILogical newChild) LogicalChildren.Add(newChild);
        }
        if (change.Property == StateProperty) { CancelPointer(); AttachState(); }
        if (change.Property == GesturesEnabledProperty && _pointer is { } pointer && this.IsAttachedToVisualTree())
        {
            var scope = _effects!;
            _ = scope.Launch(async ct =>
            {
                // The modifier update cancels input in the next composition frame.
                await scope.NextFrameAsync(ct);
                if (_pointer == pointer && !GesturesEnabled) CancelPointer();
            }, "Swipe gesture configuration");
        }
        if (change.Property == IsEffectivelyEnabledProperty || change.Property == FlowDirectionProperty)
            CancelPointer();
        if (change.Property == EnableDismissFromStartToEndProperty || change.Property == EnableDismissFromEndToStartProperty)
            _layout?.InvalidateArrange();
    }
    private void AttachState()
    {
        if (!this.IsAttachedToVisualTree() || _attachedState == State) return;
        _dismissGeneration++;
        if (_attachedState is { } old) { old.PropertyChanged -= StateChanged; old.Detach(this); }
        _effects?.Dispose();
        _effects = AnimationScope.Get(this).CreateChild("Swipe to dismiss effects");
        _attachedState = State;
        State.Attach(this);
        State.PropertyChanged += StateChanged;
        _layout?.InvalidateArrange();
        ScheduleDismissed(initial: true);
    }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SwipeToDismissBoxState.CurrentOffsetProperty) _layout?.InvalidateArrange();
        if (e.Property == SwipeToDismissBoxState.SettledValueProperty) ScheduleDismissed();
    }
    private void ScheduleDismissed(bool initial = false)
    {
        var generation = ++_dismissGeneration;
        var state = State;
        if (state.SettledValue == SwipeToDismissBoxValue.Settled) return;
        // Initial LaunchedEffect runs before the first anchors are measured. In particular,
        // an initially dismissed, unmeasured state reports DismissDirection == Settled.
        if (initial) { RaiseEvent(new SwipeDismissedEventArgs(state.DismissDirection)); return; }
        void Notify()
        {
            if (generation == _dismissGeneration && this.IsAttachedToVisualTree() && State == state)
                RaiseEvent(new SwipeDismissedEventArgs(state.DismissDirection));
        }
        if (state.IsSettlingAnimation)
        {
            // Animation writes precede recomposition in the same frame.
            Dispatcher.UIThread.Post(Notify);
            return;
        }
        var scope = _effects!;
        _ = scope.Launch(async ct =>
        {
            // LaunchedEffect(settledValue) observes the committed state on recomposition.
            await scope.NextFrameAsync(ct);
            Notify();
        }, "Swipe dismissal callback");
    }
    private void PointerDown(object? sender, PointerPressedEventArgs e)
    {
        if (!CanDrag || _pointer is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _pointer = e.Pointer;
        _down = _last = e.GetPosition(TopLevel.GetTopLevel(this));
        _velocity.Reset();
        _velocity.Add(e.Timestamp, (float)(_down.X * LayoutHelper.GetLayoutScale(this)));
        if (State.IsAnimationRunning) StartDrag(e);
    }
    private void StartDrag(PointerEventArgs e)
    {
        State.BeginDrag();
        _dragging = true;
        e.Pointer.Capture(this);
        e.Handled = true;
    }
    private void PointerMove(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer || !CanDrag) return;
        var point = e.GetPosition(TopLevel.GetTopLevel(this));
        var density = LayoutHelper.GetLayoutScale(this);
        _velocity.Add(e.Timestamp, (float)(point.X * density));
        var total = point.X - _down.X;
        // Fixed Core desktop mouse slop; touch uses the standard 18 DIP slop.
        var slop = e.Pointer.Type == PointerType.Mouse ? .125 : 18;
        if (!_dragging && Math.Abs(total) <= slop) return;
        if (!_dragging)
        {
            StartDrag(e);
            _last = new Point(_down.X + Math.Sign(total) * slop, _down.Y);
        }
        State.DragBy((float)((point.X - _last.X) * density * (FlowDirection == FlowDirection.RightToLeft ? -1 : 1)));
        _last = point;
        e.Handled = true;
    }
    private void PointerUp(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var dragged = _dragging;
        _pointer = null;
        _dragging = false;
        if (!dragged) return;
        e.Pointer.Capture(null);
        e.Handled = true;
        _ = State.EndDrag(_velocity.Calculate(e.Timestamp) * (FlowDirection == FlowDirection.RightToLeft ? -1 : 1));
    }
    private void PointerLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (e.Source == this && _pointer == e.Pointer) CancelPointer();
    }
    private void CancelPointer()
    {
        var pointer = _pointer;
        var dragged = _dragging;
        _pointer = null;
        _dragging = false;
        // A pending parent gesture must not cancel a child's independent press.
        if (pointer?.Captured == this) pointer.Capture(null);
        if (dragged && _attachedState is { } state)
        {
            if (this.IsAttachedToVisualTree()) _ = state.EndDrag(0);
            else state.CancelDrag();
        }
    }
}
