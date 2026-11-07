// Fixed Core MouseWheelScrollingLogic.kt / DesktopScrollable.desktop.kt, Apache-2.0.
using Avalonia.Input;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

internal sealed class ScrollFieldWheel(ScrollField owner)
{
    private readonly MouseWheelScroll _scroll = new();
    internal bool IsReceiving { get; private set; }

    internal bool Receive(PointerWheelEventArgs e)
    {
        if (owner.State is not { PageSize: > 0 } state || !owner.IsEffectivelyEnabled) return false;
        var y = MouseWheelScroll.Delta(e.Delta, owner.Bounds.Size, state.Density, vertical: true);
        if (y == 0) return IsReceiving;
        if (y < 0 && state.CurrentPage == 0 && state.OffsetFraction <= 0 ||
            y > 0 && state.CurrentPage == ScrollFieldState.PageCount - 1 && state.OffsetFraction >= 0) return IsReceiving;
        _scroll.Enqueue(y, e.Timestamp);
        if (!IsReceiving)
        {
            IsReceiving = true;
            _ = AnimationScope.Get(owner).Launch(async lifetime =>
            {
                try
                {
                    while (_scroll.HasPending && owner.IsAttachedToVisualTree() && owner.State is { } current)
                    {
                        try
                        {
                            var velocity = 0f;
                            var completed = false;
                            await current.WheelScrollAsync(async (scope, token) =>
                            { velocity = await _scroll.ScrollAsync(scope, current.Density, current.Consume, token); completed = !token.IsCancellationRequested; }, lifetime);
                            // The callback launches its fling; a queued event may interrupt it.
                            if (completed && !lifetime.IsCancellationRequested) owner.SettleWheel(velocity);
                        }
                        // Core's receiver survives cancellation of the userScroll child. Events
                        // accepted immediately after a state request must not be discarded.
                        catch (OperationCanceledException) when (!lifetime.IsCancellationRequested) { }
                    }
                }
                catch (OperationCanceledException) { }
                finally { IsReceiving = false; _scroll.Clear(); }
            }, "Scroll field wheel");
        }
        return true;
    }

}
