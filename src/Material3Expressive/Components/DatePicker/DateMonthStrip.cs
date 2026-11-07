// AndroidX DatePicker.HorizontalMonthsList / LazyListSnapLayoutInfoProvider, Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

// Equal-width months need only the visible month and its neighbour. Keep the first item's
// index and physical offset separately so even the full DateOnly range retains pixel precision.
internal sealed class DateMonthStrip : Panel
{
    protected override bool BypassFlowDirectionPolicies => true;
    private readonly DateOnly _first;
    private readonly int _count;
    private readonly Func<DateOnly, Control> _create;
    private readonly Action<DateOnly> _changed;
    private readonly Func<SpringSpec<float>> _snap;
    private readonly Dictionary<int, Control> _realized = [];
    private readonly VelocityTracker _velocity = new();
    private readonly MouseWheelScroll _wheel = new();
    private int _index, _offset, _width;
    private float _pending;
    private CancellationTokenSource? _cancellation;
    private IPointer? _pointer;
    private Point _down, _last;
    private bool _dragging, _wheelRunning, _notifying;
    internal bool IsScrollInProgress => _dragging || _cancellation is not null;
    internal DateOnly FirstVisibleMonth => _first.AddMonths(_index);
    internal bool CanScrollPrevious => _index > 0 || _offset > 0;
    internal bool CanScrollNext => _index < _count - 1;
    internal double ScrollOffset => _offset / Density;
    private float Density => (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
    private int Direction => FlowDirection == FlowDirection.RightToLeft ? -1 : 1;

    internal DateMonthStrip(DateOnly first, DateOnly last, DateOnly initial,
        Func<DateOnly, Control> create, Action<DateOnly> changed, Func<SpringSpec<float>> snap)
    {
        _first = first;
        _count = (last.Year - first.Year) * 12 + last.Month - first.Month + 1;
        _index = Index(initial); _create = create; _changed = changed; _snap = snap;
        ClipToBounds = true;
        Background = Brushes.Transparent;
        AddHandler(PointerPressedEvent, Down, RoutingStrategies.Tunnel);
        // Day buttons may consume moves while pressed. Claim the horizontal drag after slop
        // in the tunnel so they receive capture loss and cancel their click/ripple.
        AddHandler(PointerMovedEvent, Move, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, Up, RoutingStrategies.Tunnel);
        AddHandler(PointerCaptureLostEvent, (_, e) =>
        {
            if (_pointer != e.Pointer || e.Source != this) return;
            var drag = _dragging; _pointer = null; _dragging = false;
            if (drag) _ = RunAnimation(null, 0);
        });
        AddHandler(PointerWheelChangedEvent, Wheel, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }

    private int Index(DateOnly month) => Math.Clamp((month.Year - _first.Year) * 12 + month.Month - _first.Month, 0, _count - 1);

    internal void ShowMonth(DateOnly month)
    {
        if (_notifying || IsScrollInProgress) return; // DatePicker's LaunchedEffect(monthIndex).
        _index = Index(month); _offset = 0; _pending = 0;
        InvalidateMeasure();
    }

    internal Task AnimateToAsync(DateOnly month) => RunAnimation(Index(month), 0);
    internal Task SettleAsync(float velocity) => RunAnimation(null, velocity);
    internal void JumpToMonth(DateOnly month)
    {
        CancelAnimation();
        Place(Index(month));
    }

    private Task RunAnimation(int? target, float velocity)
    {
        CancelAnimation();
        if (_width <= 0 || VisualRoot is null) return Task.CompletedTask;
        var scope = AnimationScope.Get(this).CreateChild("Date picker month scroll");
        var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        return Run();
        async Task Run()
        {
            try
            {
                if (target is { } index)
                    await AnimateToMonth(scope, index, cancellation.Token);
                else
                    await SnapFling.RunAsync(scope, velocity, DecayAnimationSpec.ExponentialDecay<float>(),
                        _snap(), Consume, (_, _) => 0, SnapOffset, cancellation.Token);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested || scope.Lifetime.IsCancellationRequested) { }
            finally
            {
                if (ReferenceEquals(_cancellation, cancellation)) _cancellation = null;
                cancellation.Dispose(); scope.Dispose();
            }
        }
    }

    private async Task AnimateToMonth(AnimationChildScope scope, int index, CancellationToken token)
    {
        // LazyLayoutScrollScope.animateScrollToItem has two phases. As soon as the target
        // enters the visible list, it restarts from that item's integer offset, carrying
        // velocity and frame time. A single spring to the original distance is not equivalent.
        var state = new AnimationState<float>(new FloatConverter(), 0);
        var forward = index > _index;
        bool Visible() => index == _index || _offset > 0 && index == _index + 1;
        float Distance() => (index - _index) * (float)_width - _offset;
        AnimationState<float> Restart() => new(new FloatConverter(), 0,
            AnimationVector.From(state.VelocityVector), state.LastFrameTime, state.FinishedTime);
        static float Clamp(float value, float target) => target > 0 ? MathF.Min(value, target) : target < 0 ? MathF.Max(value, target) : 0;
        while (!Visible() && !token.IsCancellationRequested)
        {
            var distance = Distance();
            var target = MathF.Abs(distance) < 2500 * Density
                ? MathF.Max(MathF.Abs(distance), 50 * Density) * (forward ? 1 : -1)
                : 2500 * Density * (forward ? 1 : -1);
            state = Restart();
            var previous = 0f;
            var stopped = false;
            await SuspendAnimation.AnimateToAsync(scope, state, target, sequentialAnimation: state.Velocity != 0,
                block: frame =>
                {
                    var value = Clamp(frame.Value, target);
                    var delta = value - previous;
                    var consumed = Consume(delta);
                    if (forward ? _index > index || _index == index && _offset > 0 : _index < index)
                    {
                        Place(index); stopped = true; frame.CancelAnimation();
                    }
                    else if (Visible()) frame.CancelAnimation();
                    else
                    {
                        previous += delta;
                        if (delta != consumed) { stopped = true; frame.CancelAnimation(); }
                        else if (MathF.Abs(frame.Value) > 1500 * Density) frame.CancelAnimation();
                    }
                }, cancellationToken: token);
            if (stopped) return;
        }
        if (token.IsCancellationRequested) return;
        state = Restart();
        var seek = Distance();
        var prev = 0f;
        await SuspendAnimation.AnimateToAsync(scope, state, seek, sequentialAnimation: state.Velocity != 0,
            block: frame =>
            {
                var value = Clamp(frame.Value, seek);
                var delta = value - prev;
                var consumed = Consume(delta);
                if (delta != consumed || value != frame.Value) frame.CancelAnimation();
                prev += delta;
            }, cancellationToken: token);
        if (!token.IsCancellationRequested) Place(index);
    }

    private void Place(int index)
    {
        _index = index; _offset = 0; _pending = 0;
        InvalidateMeasure();
        _notifying = true;
        try { _changed(FirstVisibleMonth); }
        finally { _notifying = false; }
    }

    // LazyList's integer placement plus fractional scrollToBeConsumed. Unlike Pager, a list
    // waits until accumulated movement exceeds half a pixel before requesting remeasurement.
    internal float Consume(float delta)
    {
        if (_width <= 0) return 0;
        if (delta < 0 && !CanScrollPrevious || delta > 0 && !CanScrollNext) return 0;
        _pending += delta;
        if (MathF.Abs(_pending) <= .5f) return delta;
        var requested = MathUtils.RoundToInt(_pending);
        var position = (long)_index * _width + _offset;
        var updated = Math.Clamp(position + requested, 0, (long)(_count - 1) * _width);
        var consumed = (int)(updated - position);
        _pending -= consumed;
        _index = (int)(updated / _width); _offset = (int)(updated % _width);
        InvalidateMeasure();
        _notifying = true;
        try { _changed(FirstVisibleMonth); }
        finally { _notifying = false; }
        if (MathF.Abs(_pending) <= .5f) return delta;
        var result = delta - _pending; _pending = 0;
        return result;
    }

    internal float SnapOffset(float velocity)
    {
        // Full viewport items make Center equal Start. Only intersecting items participate.
        if (_offset == 0) return 0;
        var lower = -_offset;
        var upper = _index + 1 < _count ? _width - _offset : float.PositiveInfinity;
        var distance = MathF.Abs(velocity) < 400 * Density
            ? MathF.Abs(upper) <= MathF.Abs(lower) ? upper : lower
            : velocity > 0 ? upper : lower;
        return float.IsFinite(distance) ? distance : 0;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var visible = _offset > 0 && _index + 1 < _count ? new[] { _index, _index + 1 } : new[] { _index };
        foreach (var index in _realized.Keys.Except(visible).ToArray())
        {
            Children.Remove(_realized[index]); _realized.Remove(index);
        }
        foreach (var index in visible)
            if (!_realized.ContainsKey(index))
            {
                var month = _create(_first.AddMonths(index));
                month.Margin = default; // Padding belongs to the viewport, as in HorizontalMonthsList.
                _realized.Add(index, month); Children.Add(month);
            }
        foreach (var child in Children) child.Measure(availableSize);
        return new Size(Children.Max(child => child.DesiredSize.Width), Children.Max(child => child.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _width = MathUtils.DipToPx(finalSize.Width, Density);
        foreach (var (index, child) in _realized)
            child.Arrange(new Rect(((index - _index) * _width - _offset) * Direction / Density, 0,
                finalSize.Width, finalSize.Height));
        return finalSize;
    }

    private void Down(object? sender, PointerPressedEventArgs e)
    {
        // Core Scrollable excludes mouse-button dragging. Trackpads enter through wheel events.
        if (e.Pointer.Type == PointerType.Mouse || _pointer is not null || !IsEffectivelyEnabled) return;
        _pointer = e.Pointer; _down = _last = e.GetPosition(null);
        _velocity.Reset(); _velocity.Add(e.Timestamp, (float)_down.X * Density);
        if (IsScrollInProgress) StartDrag(e);
    }
    private void StartDrag(PointerEventArgs e)
    {
        CancelAnimation(); _wheel.Clear();
        _dragging = true; e.Pointer.Capture(this); e.Handled = true;
    }
    private void Move(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer || !IsEffectivelyEnabled) return;
        var point = e.GetPosition(null);
        var total = point.X - _down.X;
        if (!_dragging && Math.Abs(total) <= 18) return;
        if (!_dragging)
        {
            StartDrag(e); _last = new Point(_down.X + Math.Sign(total) * 18, _down.Y);
        }
        _velocity.Add(e.Timestamp, (float)point.X * Density);
        Consume((float)(_last.X - point.X) * Density * Direction);
        _last = point; e.Handled = true;
    }
    private void Up(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var dragging = _dragging; _pointer = null; _dragging = false;
        if (!dragging) return;
        if (e.Pointer.Captured == this) e.Pointer.Capture(null);
        e.Handled = true;
        _ = RunAnimation(null, -_velocity.Calculate(e.Timestamp) * Direction);
    }
    private void Wheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.Handled || (e.Route == RoutingStrategies.Tunnel) != _wheelRunning || !IsEffectivelyEnabled || _dragging) return;
        var delta = MouseWheelScroll.Delta(e.Delta, Bounds.Size, Density, false) * Direction;
        if (delta == 0 || delta < 0 && _index == 0 && _offset == 0 || delta > 0 && _index == _count - 1) return;
        _wheel.Enqueue(delta, e.Timestamp); e.Handled = true;
        if (_wheelRunning) return;
        CancelAnimation(); _wheelRunning = true;
        _ = AnimationScope.Get(this).Launch(async lifetime =>
        {
            var velocity = 0f;
            var completed = false;
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
            _cancellation = cancellation;
            using var scope = AnimationScope.Get(this).CreateChild("Date picker wheel");
            try
            {
                velocity = await _wheel.ScrollAsync(scope, Density, Consume, cancellation.Token);
                completed = !cancellation.IsCancellationRequested;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            finally
            {
                if (ReferenceEquals(_cancellation, cancellation)) _cancellation = null;
                _wheelRunning = false; _wheel.Clear();
            }
            // The wheel receiver ends before snapping. A new wheel event can now interrupt
            // the snap instead of queuing a delta with nobody left to consume it.
            if (completed && !lifetime.IsCancellationRequested) _ = SettleAsync(velocity);
        }, "Date picker wheel input");
    }

    private void CancelAnimation()
    {
        var cancellation = _cancellation; _cancellation = null; cancellation?.Cancel();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled || change.Property == FlowDirectionProperty)
        {
            CancelAnimation(); _wheel.Clear();
            var pointer = _pointer; _pointer = null; _dragging = false;
            if (pointer?.Captured == this) pointer.Capture(null);
            if (VisualRoot is not null && _offset != 0) _ = SettleAsync(0);
        }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelAnimation(); _wheel.Clear();
        var pointer = _pointer; _pointer = null; _dragging = false;
        if (pointer?.Captured == this) pointer.Capture(null);
        base.OnDetachedFromVisualTree(e);
    }
}
