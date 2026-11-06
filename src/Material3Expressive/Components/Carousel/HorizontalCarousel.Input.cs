using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation.Input;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

public partial class HorizontalCarousel
{
    public static readonly StyledProperty<bool> UserScrollEnabledProperty =
        AvaloniaProperty.Register<HorizontalCarousel, bool>(nameof(UserScrollEnabled), true);
    public bool UserScrollEnabled { get => GetValue(UserScrollEnabledProperty); set => SetValue(UserScrollEnabledProperty, value); }
    /// <summary>Paired drag interactions. Programmatic scrolling does not emit drag interactions.</summary>
    public MaterialInteractionSource InteractionSource { get; } = new();
    private readonly VelocityTracker _velocity = new();
    private IPointer? _pointer;
    private Point _down, _last;
    private bool _dragging;
    private MaterialInteractionEvent.DragStart? _dragInteraction;
    private IDisposable? _pendingSettle;
    private bool CanDrag => UserScrollEnabled && IsEffectivelyEnabled && _panel?.Strategy?.IsValid == true;

    public HorizontalCarousel()
    {
        AddHandler(PointerPressedEvent, PointerDown, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, PointerMove, RoutingStrategies.Bubble);
        AddHandler(PointerReleasedEvent, PointerUp, RoutingStrategies.Tunnel);
        AddHandler(PointerCaptureLostEvent, PointerLost);
        AddHandler(Thumb.DragStartedEvent, (_, _) => CancelPointer(false), RoutingStrategies.Bubble, true);
    }

    private void PointerDown(object? sender, PointerPressedEventArgs e)
    {
        // Core AbstractScrollableNode.CanDragCalculation excludes the mouse. Mouse-wheel and
        // trackpad streams are separate platform protocols, not mouse button drags.
        if (!CanDrag || _pointer is not null || e.Pointer.Type == PointerType.Mouse) return;
        _pointer = e.Pointer;
        _down = _last = e.GetPosition(TopLevel.GetTopLevel(this));
        _velocity.Reset();
        _velocity.Add(e.Timestamp, (float)(_down.X * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1)));
        if (IsScrollInProgress) StartDrag(e);
    }

    private void StartDrag(PointerEventArgs e)
    {
        CancelScroll();
        var density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        _scrollPosition.Rebase(Math.Clamp(ScrollOffset, 0, MaximumScrollOffset), density);
        _dragging = true;
        e.Pointer.Capture(this);
        e.Handled = true;
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, true);
        if (!_dragging || _pointer != e.Pointer || !CanDrag) return;
        _dragInteraction = new MaterialInteractionEvent.DragStart();
        InteractionSource.Emit(_dragInteraction);
    }

    private void PointerMove(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer || !CanDrag) return;
        var position = e.GetPosition(TopLevel.GetTopLevel(this));
        var density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        var total = position.X - _down.X;
        // Scrollable locks detection to its main axis; orthogonal motion alone doesn't start it.
        const double slop = 18;
        if (!_dragging && Math.Abs(total) <= slop) return;
        if (!_dragging)
        {
            StartDrag(e);
            if (!_dragging || _pointer != e.Pointer) return; // Interaction observers may detach/disable.
            _last = new(_down.X + Math.Sign(total) * slop, _down.Y);
        }
        // Fixed Core isDraggableVelocityTrackerFixEnabled=false: DOWN and dragging
        // changes are sampled, but moves that have not crossed touch slop are not.
        _velocity.Add(e.Timestamp, (float)position.X * density);
        var delta = (float)(position.X - _last.X) * density * (FlowDirection == FlowDirection.RightToLeft ? 1 : -1);
        _scrollPosition.Consume(delta, (float)MaximumScrollOffset * density);
        SetCurrentValue(ScrollOffsetProperty, _scrollPosition.Value);
        _last = position;
        e.Handled = true;
    }

    private void PointerUp(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var dragging = _dragging;
        var delta = (float)((e.GetPosition(TopLevel.GetTopLevel(this)).X - _down.X) * (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1));
        var density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        var velocity = _velocity.Calculate(e.Timestamp) / density * (FlowDirection == FlowDirection.RightToLeft ? 1 : -1);
        _pointer = null; _dragging = false;
        if (!dragging) return;
        if (e.Pointer.Captured == this) e.Pointer.Capture(null);
        e.Handled = true;
        EndDragInteraction(false);
        if (this.IsAttachedToVisualTree()) Settle(velocity, delta);
    }
    private void PointerLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (e.Source == this && _pointer == e.Pointer) CancelPointer(true);
    }
    private void CancelPointer(bool settle, bool afterConfigurationCommit = false)
    {
        var pointer = _pointer;
        var dragging = _dragging;
        _pointer = null; _dragging = false;
        if (pointer?.Captured == this) pointer.Capture(null);
        if (!dragging) return;
        EndDragInteraction(true);
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false);
        if (settle && this.IsAttachedToVisualTree())
        {
            if (afterConfigurationCommit)
            {
                // Core updates DragGestureNode during composition, after the animation
                // frame's waiter batch. Reuse the existing commit queue for that effect.
                _pendingSettle?.Dispose();
                _pendingSettle = AnimationScope.Get(this).ScheduleFrameCommit(() =>
                {
                    _pendingSettle = null;
                    if (this.IsAttachedToVisualTree()) Settle(0, 0);
                });
            }
            else Settle(0, 0);
        }
    }
    private void EndDragInteraction(bool cancelled)
    {
        var start = _dragInteraction;
        _dragInteraction = null;
        if (start is not null) InteractionSource.Emit(cancelled
            ? new MaterialInteractionEvent.DragCancel(start) : new MaterialInteractionEvent.DragStop(start));
    }
    private void Settle(double velocity, float gestureDelta)
    {
        _ = AnimationScope.Get(this).Launch(async token => { await Fling(velocity, gestureDelta, token); }, "Carousel release");
    }
}
