using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

public partial class HorizontalCarousel
{
    private readonly List<(Control Target, Rect? Rect)> _bringRequests = [];
    private bool _bringingIntoView;
    private Control? _focusedChild;
    private bool _trackingFocusedChild;

    // ContentInViewNode / BringIntoViewRequestPriorityQueue. Avalonia's routed request
    // has no awaiting caller; containment ordering and superseding requests still apply.
    private void BringChildIntoView(Control child, Rect? rect)
    {
        if (_panel?.Strategy?.IsValid != true || _dragging || _receivingWheel) return;
        var request = (Target: child, Rect: rect);
        if (BoundsOf(request) is not { } bounds || MathF.Abs(BringDistance(bounds)) <= .5f) return;
        if (!_bringingIntoView) CancelScroll();
        var insertion = 0;
        for (var i = _bringRequests.Count - 1; i >= 0; i--)
        {
            if (BoundsOf(_bringRequests[i]) is not { } previous) continue;
            var intersection = bounds.Intersect(previous);
            if (intersection == bounds) { insertion = i + 1; break; }
            if (intersection != previous) _bringRequests.RemoveRange(i, _bringRequests.Count - i);
        }
        _bringRequests.Insert(insertion, request);
        if (_bringingIntoView) return;
        StartBringIntoView();
    }

    // ContentInViewNode.onRemeasured: only keep a previously visible focused child
    // visible when the viewport shrinks. The coordinates still belong to old placement.
    internal void TrackFocusOnResize(double previousWidth, double width)
    {
        if (width >= previousWidth || _bringingIntoView || _trackingFocusedChild || _focusedChild is not { } child ||
            BoundsOf((child, null)) is not { } bounds || _dragging || _receivingWheel) return;
        var adjustment = FlowDirection == FlowDirection.RightToLeft ? previousWidth - width : 0;
        var distance = BringDistance(bounds.Translate(new Vector(-adjustment, 0)), width);
        if (MathF.Abs(BringDistance(bounds, previousWidth)) > .5f || MathF.Abs(distance) <= .5f) return;
        CancelScroll();
        _trackingFocusedChild = true;
        StartBringIntoView(distance);
    }

    private void StartBringIntoView(float? initialDistance = null)
    {
        var root = AnimationScope.Get(this);
        var scope = root.CreateChild("Carousel bring into view");
        _animationScope = scope;
        var cancellation = new CancellationTokenSource();
        _scrollCancellation = cancellation;
        var density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        _scrollPosition.Rebase(Math.Clamp(ScrollOffset, 0, MaximumScrollOffset), density);
        _bringingIntoView = true;
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, true);
        _ = root.Launch(async lifetime =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token, lifetime);
            try
            {
                using var binding = scope.BindAnimatedValue(this, ScrollOffsetProperty, _scrollPosition);
                // Fixed UpdatableAnimationState: each frame evaluates the latest remaining
                // distance to zero, preserving velocity and measuring the next target after layout.
                var spec = new SpringSpec<float>().Vectorize(new FloatConverter());
                var value = initialDistance ?? Distance();
                double[] velocity = [0], initial = [value], zero = [0], output = [0], nextVelocity = [0];
                TimeSpan? lastFrame = null;
                ((Control Target, Rect? Rect) Request, Rect? Bounds)[]? frameBounds = null;
                var scale = root.DurationScale * AnimationScope.GetDurationScale(root.Host);
                while (MathF.Abs(value) >= .01f && !linked.IsCancellationRequested)
                {
                    var frame = await scope.NextFrameAsync(linked.Token);
                    initial[0] = value;
                    var elapsed = scale == 0 ? spec.GetDuration(initial, zero, velocity)
                        : TimeSpan.FromTicks((long)Math.Round((frame - (lastFrame ?? frame)).Ticks / scale));
                    spec.GetValue(elapsed, initial, zero, velocity, output);
                    spec.GetVelocity(elapsed, initial, zero, velocity, nextVelocity);
                    velocity[0] = (float)nextVelocity[0];
                    lastFrame = frame;
                    var delta = value - (float)output[0];
                    value = (float)output[0];
                    if (!Consume(delta)) { value = 0; break; }
                    await AfterFrame();
                    if (scale == 0) break;
                }
                if (value != 0 && !linked.IsCancellationRequested)
                {
                    await scope.NextFrameAsync(linked.Token);
                    Consume(value);
                    await AfterFrame();
                }

                bool Consume(float delta)
                {
                    var targets = _trackingFocusedChild && _focusedChild is { } focused
                        ? _bringRequests.Append((focused, (Rect?)null)) : _bringRequests;
                    frameBounds = targets.Select(request => (Request: request, Bounds: BoundsOf(request))).ToArray();
                    var previous = _scrollPosition.Value;
                    var direction = FlowDirection == FlowDirection.RightToLeft ? -1 : 1;
                    var consumed = _scrollPosition.Consume(delta * direction, (float)MaximumScrollOffset * density, _panel);
                    // Pager's incremental placement is deferred until draw. A forced measure
                    // updates integer page placement now, but its graphics-layer translation
                    // still belongs to the preceding draw when ContentInView.afterFrame runs.
                    if (MathF.Abs(delta) >= 1e-4f && _panel?.LastScrollRemeasured == true)
                    {
                        var shift = (previous - _scrollPosition.Value) * direction;
                        for (var i = 0; i < frameBounds.Length; i++)
                            if (frameBounds[i].Bounds is { } bounds) frameBounds[i].Bounds = bounds.Translate(new Vector(shift, 0));
                    }
                    return MathF.Abs(consumed) >= MathF.Abs(delta);
                }
                async Task AfterFrame()
                {
                    // Resume outside the sampling batch, matching ContentInView.afterFrame's
                    // placement/layer phase, then allow Avalonia's visual layout to catch up.
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (linked.IsCancellationRequested) return;
                        while (_bringRequests.Count > 0)
                        {
                            if (FrameBounds(_bringRequests[^1]) is { } current && MathF.Abs(BringDistance(current)) > .5f) break;
                            _bringRequests.RemoveAt(_bringRequests.Count - 1);
                        }
                        if (_trackingFocusedChild && (_focusedChild is not { } focused ||
                            FrameBounds((focused, null)) is not { } focusBounds || MathF.Abs(BringDistance(focusBounds)) <= .5f))
                            _trackingFocusedChild = false;
                        value = Distance(FrameBounds);
                        frameBounds = null;
                        UpdateLayout();
                    }, DispatcherPriority.Render);
                }

                Rect? FrameBounds((Control Target, Rect? Rect) request) => _panel?.IsVisualAncestorOf(request.Target) == true
                    ? frameBounds?.FirstOrDefault(entry => entry.Request == request).Bounds ?? BoundsOf(request) : null;
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested || scope.Lifetime.IsCancellationRequested) { }
            finally
            {
                if (ReferenceEquals(_scrollCancellation, cancellation))
                {
                    SetCurrentValue(ScrollOffsetProperty, _scrollPosition.Value);
                    _scrollCancellation = null; _animationScope = null;
                    _bringRequests.Clear();
                    _bringingIntoView = false;
                    _trackingFocusedChild = false;
                    SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false);
                }
                cancellation.Dispose(); scope.Dispose();
            }
        }, "Carousel focus relocation");

        float Distance(Func<(Control Target, Rect? Rect), Rect?>? boundsOf = null)
        {
            Rect? result = null;
            for (var i = _bringRequests.Count - 1; i >= 0; i--)
            {
                if ((boundsOf ?? BoundsOf)(_bringRequests[i]) is not { } current) continue;
                if (current.Width <= Bounds.Width) result = current;
                else return BringDistance(result ?? current);
            }
            if (result is { } target) return BringDistance(target);
            return _trackingFocusedChild && _focusedChild is { } focused &&
                (boundsOf ?? BoundsOf)((focused, null)) is { } focusBounds ? BringDistance(focusBounds) : 0;
        }
    }

    private Rect? BoundsOf((Control Target, Rect? Rect) request)
    {
        // The panel bypasses Avalonia's inherited RTL mirroring and places in physical
        // coordinates, matching Compose localRectOf. The owner itself may be mirrored.
        if (_panel is null || !_panel.IsVisualAncestorOf(request.Target) || request.Target.TransformToVisual(_panel) is not { } transform) return null;
        return (request.Rect ?? new Rect(request.Target.Bounds.Size)).TransformToAABB(transform);
    }

    // Fixed Pager.DefaultPagerBringIntoViewSpec. Its settling grid is pageSizeWithSpacing,
    // not Carousel's focal keylines; this distinction also applies when user scrolling is disabled.
    private float BringDistance(Rect bounds, double? viewportWidth = null)
    {
        if (_panel is not { PageStride: > 0 } panel) return 0;
        var density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        var offset = (float)bounds.Left * density;
        var size = (float)bounds.Width * density;
        var container = (float)(viewportWidth ?? Bounds.Width) * density;
        var trailing = offset + size;
        var proposed = offset >= 0 && trailing <= container || offset < 0 && trailing > container ? 0
            : MathF.Abs(offset) < MathF.Abs(trailing - container) ? offset : trailing - container;
        var outOfView = offset > 0 ? trailing > container : trailing <= 1;
        var stride = panel.PageStride;
        var firstOffset = CarouselStrategy.Round((float)ScrollOffset * density) % stride;
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var awareOffset = rtl ? -firstOffset + stride : firstOffset;
        if (MathF.Abs(proposed) != 0 && outOfView)
        {
            var corrected = -awareOffset;
            while (proposed > 0 && corrected < proposed) corrected += stride;
            while (proposed < 0 && corrected > proposed) corrected -= stride;
            return corrected;
        }
        if (MathF.Abs(firstOffset) < 1e-6f) return 0;
        var forward = _scrollPosition.LastScrolledForward;
        var settling = -awareOffset + ((rtl ? !forward : forward) ? stride : 0);
        return Math.Clamp(settling, -container, container);
    }
}
