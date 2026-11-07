using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

public partial class HorizontalCarousel
{
    private readonly MouseWheelScroll _wheel = new();
    private bool _receivingWheel;

    private void PointerWheel(object? sender, PointerWheelEventArgs e)
    {
        // NonTouchScrollingLogic: an active stream takes Initial-pass priority;
        // otherwise descendants receive Main-pass input first.
        if (e.Handled || (e.Route == RoutingStrategies.Tunnel) != _receivingWheel || !CanDrag) return;
        var density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        var delta = MouseWheelScroll.Delta(e.Delta, Bounds.Size, density, vertical: false);
        if (FlowDirection == FlowDirection.RightToLeft) delta = -delta;
        if (delta == 0 || delta < 0 && ScrollOffset <= 0 || delta > 0 && ScrollOffset >= MaximumScrollOffset)
        {
            e.Handled = _receivingWheel;
            return;
        }
        _wheel.Enqueue(delta, e.Timestamp);
        e.Handled = true;
        if (_receivingWheel) return;
        _receivingWheel = true;
        _ = AnimationScope.Get(this).Launch(async lifetime =>
        {
            try
            {
                while (_wheel.HasPending && this.IsAttachedToVisualTree())
                {
                    var velocity = 0f;
                    var completed = false;
                    CancelPointer(false);
                    CancelScroll();
                    _scrollPosition.Rebase(Math.Clamp(ScrollOffset, 0, MaximumScrollOffset), density);
                    var scope = AnimationScope.Get(this).CreateChild("Carousel wheel movement");
                    _animationScope = scope;
                    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
                    _scrollCancellation = cancellation;
                    SetAndRaise(IsScrollInProgressProperty, ref _scrolling, true);
                    try
                    {
                        using var binding = scope.BindAnimatedValue(this, ScrollOffsetProperty, _scrollPosition);
                        velocity = await _wheel.ScrollAsync(scope, density,
                            amount => _scrollPosition.Consume(amount, (float)MaximumScrollOffset * density, _panel), cancellation.Token);
                        completed = !cancellation.IsCancellationRequested;
                    }
                    finally
                    {
                        if (ReferenceEquals(_scrollCancellation, cancellation))
                        {
                            SetCurrentValue(ScrollOffsetProperty, _scrollPosition.Value);
                            _scrollCancellation = null;
                            _animationScope = null;
                            SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false);
                        }
                        scope.Dispose();
                    }
                    // All Carousel fling policies are TargetedFlingBehavior upstream,
                    // including noSnapFlingBehavior, so wheel completion invokes them.
                    if (completed && !lifetime.IsCancellationRequested) Settle(velocity / density, 0);
                }
            }
            finally { _receivingWheel = false; _wheel.Clear(); }
        }, "Carousel wheel receiver");
    }
}
