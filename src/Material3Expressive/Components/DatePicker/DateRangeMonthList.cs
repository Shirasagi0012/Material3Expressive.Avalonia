// AndroidX DateRangePicker.VerticalMonthsList / LazyListMeasure and fixed Core
// DefaultFlingBehavior / Scrollable.desktop.kt, Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

// A vertical lazy list specialized to calendar months. Keep an item index and physical offset,
// rather than an estimated total extent: headings have their actual measured, variable heights.
internal sealed class DateRangeMonthList : Panel
{
    private readonly DateOnly _first;
    private readonly int _count;
    private readonly Func<DateOnly, Control> _create;
    private readonly Action<DateOnly> _changed;
    private readonly Dictionary<int, Control> _realized = [];
    private readonly Dictionary<int, int> _positions = [];
    private readonly VelocityTracker _velocity = new();
    private readonly MouseWheelScroll _wheel = new();
    private int _index, _offset, _viewport, _lastVisible, _arrangedViewport;
    private double _measureWidth;
    private float _pending;
    private bool _canNext, _dragging, _wheelRunning, _notifying;
    private CancellationTokenSource? _cancellation;
    private IPointer? _pointer;
    private Point _down, _last;
    private Control? _bringTarget;
    private Rect? _bringRect;
    private bool _bringing;
    private Control? _focusedChild;
    private bool _lastScrollRemeasured;
    private float Density => (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);

    internal int FirstVisibleIndex => _index;
    internal DateOnly FirstVisibleMonth => _first.AddMonths(_index);
    internal double FirstVisibleOffset => _offset / Density;
    internal bool CanScrollPrevious => _index > 0 || _offset > 0;
    internal bool CanScrollNext => _canNext;
    internal bool IsScrollInProgress => _dragging || _cancellation is not null;

    internal DateRangeMonthList(DateOnly first, DateOnly last, DateOnly initial,
        Func<DateOnly, Control> create, Action<DateOnly> changed)
    {
        _first = first;
        _count = (last.Year - first.Year) * 12 + last.Month - first.Month + 1;
        if (_count <= 0) throw new ArgumentOutOfRangeException(nameof(last));
        _index = Index(initial); _create = create; _changed = changed;
        ClipToBounds = true; Background = Brushes.Transparent;
        AddHandler(PointerPressedEvent, Down, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, Move, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, Up, RoutingStrategies.Tunnel);
        AddHandler(PointerCaptureLostEvent, (_, e) =>
        {
            if (_pointer != e.Pointer || e.Source != this) return;
            _pointer = null; _dragging = false;
        });
        AddHandler(PointerWheelChangedEvent, Wheel, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        AddHandler(GotFocusEvent, (_, e) =>
        {
            if (e.Source is Control child && child != this)
            {
                _focusedChild = child; BringChildIntoView(child, null);
            }
        });
        AddHandler(LostFocusEvent, (_, e) => { if (ReferenceEquals(_focusedChild, e.Source)) _focusedChild = null; });
        AddHandler(RequestBringIntoViewEvent, (_, e) =>
        {
            if (e.TargetObject is Control child && child != this) BringChildIntoView(child, e.TargetRect);
        });
    }

    private int Index(DateOnly month) => Math.Clamp((month.Year - _first.Year) * 12 + month.Month - _first.Month, 0, _count - 1);
    internal IEnumerable<Control> GetRealizedContainers() => _realized.OrderBy(item => item.Key).Select(item => item.Value);
    internal Control? ContainerFromIndex(int index) => _realized.GetValueOrDefault(index);

    internal void ScrollToMonth(DateOnly month)
    {
        if (_notifying) return;
        StopInput();
        _index = Index(month); _offset = 0; _pending = 0;
        if (_viewport > 0) LayoutItems();
        InvalidateMeasure(); Notify();
    }

    internal void BringMonthIntoView(DateOnly month)
    {
        var index = Index(month);
        if (index < _index || index > _lastVisible) ScrollToMonth(month);
    }

    private Control Realize(int index)
    {
        if (!_realized.TryGetValue(index, out var child))
        {
            child = _create(_first.AddMonths(index));
            _realized.Add(index, child); Children.Add(child);
        }
        child.Measure(new Size(_measureWidth, double.PositiveInfinity));
        return child;
    }
    private int HeightOf(int index) => Math.Max(1, MathUtils.DipToPx(Realize(index).DesiredSize.Height, Density));

    // Move in measured item coordinates. The final viewport is backfilled from preceding
    // months, exactly as LazyListMeasure does; the final month need not start at the viewport top.
    private int MovePixels(int delta)
    {
        var remaining = (long)_offset + delta;
        var consumed = delta;
        while (remaining < 0 && _index > 0) remaining += HeightOf(--_index);
        if (remaining < 0) { consumed -= (int)remaining; remaining = 0; }
        while (_index < _count - 1)
        {
            var height = HeightOf(_index);
            if (remaining < height) break;
            remaining -= height; _index++;
        }
        var lastHeight = HeightOf(_index);
        if (_index == _count - 1 && remaining > lastHeight)
        {
            consumed -= (int)(remaining - lastHeight); remaining = lastHeight;
        }
        _offset = (int)remaining;
        consumed -= LayoutItems();
        return consumed;
    }

    // Returns the backward correction required at the end. Focused date buttons remain
    // attached while offscreen; all other nonintersecting month containers are released.
    private int LayoutItems()
    {
        _positions.Clear();
        var bottom = -_offset;
        var last = _index;
        while (last < _count)
        {
            _positions[last] = bottom;
            bottom += HeightOf(last);
            if (bottom >= _viewport || last == _count - 1) break;
            last++;
        }
        var correction = 0;
        if (bottom < _viewport)
        {
            var missing = _viewport - bottom;
            while (missing > 0)
            {
                var shift = Math.Min(missing, _offset);
                _offset -= shift; missing -= shift; correction += shift;
                if (missing == 0 || _index == 0) break;
                _offset = HeightOf(--_index);
            }
            if (correction > 0)
            {
                _positions.Clear(); bottom = -_offset;
                for (var i = _index; i <= last; i++) { _positions[i] = bottom; bottom += HeightOf(i); }
            }
        }
        _lastVisible = last;
        _canNext = last < _count - 1 || bottom > _viewport;
        foreach (var index in _realized.Keys.Except(_positions.Keys).ToArray())
        {
            if (_realized[index].IsKeyboardFocusWithin) continue;
            Children.Remove(_realized[index]); _realized.Remove(index);
        }
        // LazyListMeasure positions pinned extras contiguously beside the visible items,
        // deliberately omitting uncomposed intermediate months from their coordinates.
        var before = -_offset;
        foreach (var index in _realized.Keys.Where(index => index < _index).OrderDescending())
        {
            before -= HeightOf(index); _positions[index] = before;
        }
        var after = bottom;
        foreach (var index in _realized.Keys.Where(index => index > last).Order())
        {
            _positions[index] = after; after += HeightOf(index);
        }
        return correction;
    }

    internal float Consume(float delta)
    {
        _lastScrollRemeasured = false;
        if (_viewport <= 0 || delta < 0 && !CanScrollPrevious || delta > 0 && !CanScrollNext) return 0;
        _pending += delta;
        if (MathF.Abs(_pending) <= .5f) return delta;
        var requested = MathUtils.RoundToInt(_pending);
        var firstHeight = HeightOf(_index);
        var lastTop = _positions[_lastVisible];
        var lastBottom = lastTop + HeightOf(_lastVisible);
        // LazyListMeasureResult.copyWithScrollDeltaWithoutRemeasure. Its strict boundary
        // test determines when ContentInView.afterFrame can observe the new coordinates.
        var sameFirst = _offset + requested >= 0 && _offset + requested < firstHeight;
        var sameVisible = requested > 0
            ? Math.Min(firstHeight - _offset, lastBottom - _viewport) > requested
            : Math.Min(_offset, _viewport - lastTop) > -requested;
        _lastScrollRemeasured = !sameFirst || !sameVisible;
        _pending -= MovePixels(requested);
        InvalidateMeasure(); Notify();
        if (MathF.Abs(_pending) <= .5f) return delta;
        var result = delta - _pending; _pending = 0;
        return result;
    }

    private void Notify()
    {
        _notifying = true;
        try { _changed(FirstVisibleMonth); }
        finally { _notifying = false; }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _measureWidth = availableSize.Width;
        if (!double.IsFinite(availableSize.Height))
            throw new InvalidOperationException("DateRangePicker requires a bounded height, as does the upstream LazyColumn. Set Height or place it in a bounded Grid row.");
        _viewport = MathUtils.DipToPx(availableSize.Height, Density);
        MovePixels(0);
        var width = _realized.Values.Max(child => child.DesiredSize.Width);
        return new Size(width, _viewport / Density);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var previousViewport = _arrangedViewport;
        var focusedBounds = _focusedChild is { } focused ? BoundsOf(focused, null) : null;
        _measureWidth = finalSize.Width; _viewport = MathUtils.DipToPx(finalSize.Height, Density);
        MovePixels(0);
        foreach (var (index, child) in _realized)
        {
            var y = _positions.TryGetValue(index, out var position) ? position : -HeightOf(index);
            child.Arrange(new Rect(0, y / Density, finalSize.Width, HeightOf(index) / Density));
        }
        _arrangedViewport = _viewport;
        // ContentInViewNode.onRemeasured only rescues a focused child that was previously
        // maximally visible. A user-scrolled-out day must not pull the calendar back on resize.
        if (_viewport < previousViewport && !_bringing && focusedBounds is { } oldBounds &&
            MathF.Abs(BringDistance(oldBounds, previousViewport)) <= .5f && _focusedChild is { } childToKeep)
            BringChildIntoView(childToKeep, null);
        Notify();
        return finalSize;
    }

    internal Task FlingAsync(float velocity)
    {
        CancelAnimation();
        if (_viewport <= 0 || VisualRoot is null || MathF.Abs(velocity) <= 1) return Task.CompletedTask;
        var scope = AnimationScope.Get(this).CreateChild("Date range month fling");
        var cancellation = new CancellationTokenSource(); _cancellation = cancellation;
        return Run();
        async Task Run()
        {
            try
            {
                var decay = new DecayAnimation<float>(DecayAnimationSpec.SplineBasedDecay<float>(Density), new FloatConverter(), 0, velocity);
                TimeSpan? started = null;
                var previous = 0f;
                // DefaultFlingBehavior fixes MotionDurationScale to 1. Raw scope frames retain
                // that policy even when the surrounding visual animations are disabled/scaled.
                await scope.RunFramesAsync(time =>
                {
                    started ??= time;
                    var elapsed = time - started.Value;
                    var value = decay.GetValue(elapsed);
                    var delta = value - previous;
                    var consumed = Consume(delta); previous = value;
                    return MathF.Abs(delta - consumed) <= .5f && !decay.IsFinished(elapsed);
                }, cancellation.Token);
            }
            finally
            {
                if (ReferenceEquals(_cancellation, cancellation)) _cancellation = null;
                cancellation.Dispose(); scope.Dispose();
            }
        }
    }

    // Date keyboard navigation focuses a day, not its entire month. Default Core
    // BringIntoViewSpec chooses the nearest edge; UpdatableAnimationState retargets the
    // remaining distance each frame and preserves velocity if another day gains focus.
    private void BringChildIntoView(Control child, Rect? rect)
    {
        if (_dragging || _wheelRunning || !this.IsVisualAncestorOf(child)) return;
        if (!_bringing)
        {
            if (Distance(child, rect) == 0) return;
            CancelAnimation();
        }
        _bringTarget = child; _bringRect = rect;
        if (_bringing) return;
        _bringing = true;
        var root = AnimationScope.Get(this);
        var scope = root.CreateChild("Date range focused day relocation");
        var cancellation = new CancellationTokenSource(); _cancellation = cancellation;
        _ = Run();
        async Task Run()
        {
            try
            {
                var spec = new SpringSpec<float>().Vectorize(new FloatConverter());
                var value = Distance(child, rect);
                double[] velocity = [0], initial = [value], zero = [0], output = [0], nextVelocity = [0];
                TimeSpan? previous = null;
                var scale = root.DurationScale * AnimationScope.GetDurationScale(root.Host);
                var blocked = false;
                while (MathF.Abs(value) >= .01f && !cancellation.IsCancellationRequested)
                {
                    var time = await scope.NextFrameAsync(cancellation.Token);
                    initial[0] = value;
                    var elapsed = scale == 0 ? spec.GetDuration(initial, zero, velocity)
                        : TimeSpan.FromTicks((long)Math.Round((time - (previous ?? time)).Ticks / scale));
                    spec.GetValue(elapsed, initial, zero, velocity, output);
                    spec.GetVelocity(elapsed, initial, zero, velocity, nextVelocity);
                    velocity[0] = (float)nextVelocity[0]; previous = time;
                    var delta = value - (float)output[0];
                    var consumed = Consume(delta);
                    // Core's incremental scroll only invalidates placement. Its afterFrame
                    // observes the old child coordinates until draw; forceRemeasure updates
                    // them synchronously when an item enters or leaves the visible list.
                    if (_lastScrollRemeasured) UpdateLayout();
                    if (MathF.Abs(consumed) < MathF.Abs(delta) || _bringTarget is null) { blocked = true; break; }
                    value = Distance(_bringTarget, _bringRect);
                    UpdateLayout();
                    if (scale == 0) break;
                }
                if (value != 0 && !blocked && !cancellation.IsCancellationRequested)
                {
                    await scope.NextFrameAsync(cancellation.Token);
                    Consume(value); UpdateLayout();
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested || scope.Lifetime.IsCancellationRequested) { }
            finally
            {
                if (ReferenceEquals(_cancellation, cancellation))
                {
                    _cancellation = null; _bringing = false; _bringTarget = null; _bringRect = null;
                }
                cancellation.Dispose(); scope.Dispose();
            }
        }

        float Distance(Control target, Rect? requested)
        {
            return BoundsOf(target, requested) is { } bounds ? BringDistance(bounds, _viewport) : 0;
        }
    }

    private Rect? BoundsOf(Control target, Rect? requested) => this.IsVisualAncestorOf(target) && target.TransformToVisual(this) is { } transform
        ? (requested ?? new Rect(target.Bounds.Size)).TransformToAABB(transform) : null;

    private float BringDistance(Rect bounds, int viewport)
    {
        var leading = (float)bounds.Top * Density;
        var trailing = (float)bounds.Bottom * Density;
        if (leading >= 0 && trailing <= viewport || leading < 0 && trailing > viewport) return 0;
        return MathF.Abs(leading) < MathF.Abs(trailing - viewport) ? leading : trailing - viewport;
    }

    private void Down(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse || _pointer is not null || !IsEffectivelyEnabled) return;
        _pointer = e.Pointer; _down = _last = e.GetPosition(null);
        _velocity.Reset(); _velocity.Add(e.Timestamp, (float)_down.Y * Density);
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
        var total = point.Y - _down.Y;
        if (!_dragging && Math.Abs(total) <= 18) return;
        if (!_dragging) { StartDrag(e); _last = new Point(_down.X, _down.Y + Math.Sign(total) * 18); }
        _velocity.Add(e.Timestamp, (float)point.Y * Density);
        Consume((float)(_last.Y - point.Y) * Density);
        _last = point; e.Handled = true;
    }
    private void Up(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer) return;
        var dragging = _dragging; _pointer = null; _dragging = false;
        if (!dragging) return;
        if (e.Pointer.Captured == this) e.Pointer.Capture(null);
        e.Handled = true; _ = FlingAsync(-_velocity.Calculate(e.Timestamp));
    }
    private void Wheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.Handled || (e.Route == RoutingStrategies.Tunnel) != _wheelRunning || !IsEffectivelyEnabled || _dragging) return;
        var delta = MouseWheelScroll.Delta(e.Delta, Bounds.Size, Density, true);
        if (delta == 0 || delta < 0 && !CanScrollPrevious || delta > 0 && !CanScrollNext) return;
        e.Handled = true;
        if (_wheelRunning) { _wheel.Enqueue(delta, e.Timestamp); return; }
        CancelAnimation(); _wheel.Enqueue(delta, e.Timestamp); _wheelRunning = true;
        _ = AnimationScope.Get(this).Launch(async lifetime =>
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
            _cancellation = cancellation;
            using var scope = AnimationScope.Get(this).CreateChild("Date range wheel");
            try { await _wheel.ScrollAsync(scope, Density, Consume, cancellation.Token); }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            finally
            {
                if (ReferenceEquals(_cancellation, cancellation))
                {
                    _cancellation = null; _wheelRunning = false; _wheel.Clear();
                }
            }
            // Core's ScrollableDefaultFlingBehavior explicitly excludes mouse-wheel fling.
        }, "Date range wheel input");
    }
    private void CancelAnimation()
    {
        var cancellation = _cancellation; _cancellation = null;
        _bringing = false; _bringTarget = null; _bringRect = null;
        _wheelRunning = false; _wheel.Clear(); cancellation?.Cancel();
    }
    private void StopInput()
    {
        CancelAnimation(); _wheel.Clear();
        var pointer = _pointer; _pointer = null; _dragging = false;
        if (pointer?.Captured == this) pointer.Capture(null);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) StopInput();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        StopInput(); base.OnDetachedFromVisualTree(e);
    }
}
