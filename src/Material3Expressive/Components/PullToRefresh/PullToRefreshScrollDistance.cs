using Avalonia.Controls;

namespace Material3Expressive.Components;

internal interface IPullToRefreshScrollHost
{
    Control Control { get; }
    IPullToRefreshState EffectiveState { get; }
    bool IsRefreshing { get; }
    double RefreshThreshold { get; }
}

// PullToRefreshModifierNode.consumeAvailableOffset / calculateVerticalOffset.
// Both explicit UI dispatch and physical viewport transactions use this Float
// calculation in physical pixels; neither keeps an independent approximation.
internal static class PullToRefreshScrollDistance
{
    internal static float Consume(float availableDip, float density, float thresholdPx,
        ref float distancePulled, out float verticalOffset)
    {
        var next = Math.Max(distancePulled + availableDip * density, 0);
        var consumed = next - distancePulled;
        distancePulled = next;
        var adjusted = distancePulled * .5f;
        if (adjusted <= thresholdPx) verticalOffset = adjusted;
        else
        {
            var linear = Math.Clamp(Math.Abs(adjusted / thresholdPx) - 1, 0, 2);
            var tension = linear - linear * linear / 4;
            verticalOffset = thresholdPx + thresholdPx * tension;
        }
        return consumed / density;
    }
}
