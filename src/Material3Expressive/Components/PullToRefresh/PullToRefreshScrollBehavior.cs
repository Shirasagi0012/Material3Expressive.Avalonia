// PullToRefresh.kt / PullToRefreshModifierNode, pinned AndroidX baseline; Apache-2.0.
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

/// <summary>Explicit nested-scroll adapter for a pull-to-refresh indicator. Distances are DIP and velocities DIP/s.</summary>
/// <remarks>The host must dispatch pre/post input and pre-fling in nested-scroll order. This does not subscribe to ScrollChanged.</remarks>
public sealed class PullToRefreshScrollBehavior
{
    private PullToRefreshIndicatorBox? _owner;
    private AnimationChildScope? _scope;
    private float _distancePulled, _verticalOffset;
    private bool _updatePending;
    public bool IsEnabled { get; set; } = true;
    public event EventHandler? RefreshRequested;
    internal float DistancePulled => _distancePulled;
    internal float VerticalOffset => _verticalOffset;
    private float Density => (float)LayoutHelper.GetLayoutScale(Owner);
    private float ThresholdPx => MathF.Floor((float)Owner.RefreshThreshold * Density + .5f);
    private PullToRefreshIndicatorBox Owner => _owner ?? throw new InvalidOperationException("Attach the behavior to a mounted pull-to-refresh indicator first.");

    internal void Attach(PullToRefreshIndicatorBox owner)
    {
        if (_owner is not null) throw new InvalidOperationException("A pull-to-refresh scroll behavior can have only one mounted owner.");
        _owner = owner;
        _scope = AnimationScope.Get(owner).CreateChild("Pull-to-refresh scroll");
        _ = _scope.Launch(async ct =>
        {
            // The modifier's coroutine starts after its content has composed. In
            // particular, the arrow remembers the incoming state's initial alpha.
            await Task.Yield(); if (ct.IsCancellationRequested) return;
            await owner.EffectiveState.SnapToAsync(owner.IsRefreshing ? 1 : 0, ct);
        }, "Initial pull position");
        _verticalOffset = owner.IsRefreshing ? ThresholdPx : 0;
    }
    internal void Detach(PullToRefreshIndicatorBox owner)
    {
        if (!ReferenceEquals(_owner, owner)) return;
        _owner = null;
        _scope?.Dispose(); _scope = null;
        _updatePending = false;
    }
    internal void RefreshingChanged()
    {
        if (_scope is not { } scope || _updatePending) return;
        _updatePending = true;
        _ = scope.Launch(async ct =>
        {
            // Modifier.update is a composition commit, after the current input callback
            // and the old frame sample. Coalesce multiple changes before that commit.
            if (!await scope.WaitForNextFrameAsync(ct)) return;
            await Task.Yield(); if (ct.IsCancellationRequested) return;
            _updatePending = false;
            await AnimateAsync(Owner.IsRefreshing, ct);
        }, "Pull refreshing composition");
    }

    /// <summary>Consumes upward input before the scroll child. Positive Y means down; programmatic/side-effect input passes false.</summary>
    public float OnPreScroll(float availableY, bool isUserInput = true)
    {
        Dispatcher.UIThread.VerifyAccess();
        return Owner.EffectiveState.IsAnimating || !IsEnabled || !isUserInput || availableY >= 0
            ? 0 : Consume(availableY);
    }
    /// <summary>Consumes the child's remaining vertical input and publishes the resulting distance.</summary>
    public float OnPostScroll(float availableY, bool isUserInput = true)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (Owner.EffectiveState.IsAnimating || !IsEnabled || !isUserInput) return 0;
        var consumed = Consume(availableY);
        _ = _scope!.Launch(ct => Owner.EffectiveState.IsAnimating ? Task.CompletedTask
            : Owner.EffectiveState.SnapToAsync(_verticalOffset / ThresholdPx, ct), "Pull scroll distance");
        return consumed;
    }
    private float Consume(float availableY)
    {
        if (Owner.IsRefreshing) return 0;
        var density = Density;
        var next = Math.Max(_distancePulled + availableY * density, 0);
        var consumed = next - _distancePulled;
        _distancePulled = next;
        var adjusted = _distancePulled * .5f;
        var threshold = ThresholdPx;
        if (adjusted <= threshold) _verticalOffset = adjusted;
        else
        {
            var linear = Math.Clamp(Math.Abs(adjusted / threshold) - 1, 0, 2);
            var tension = linear - linear * linear / 4;
            _verticalOffset = threshold + threshold * tension;
        }
        return consumed / density;
    }
    /// <summary>Returns consumed vertical velocity. Refresh requires distance strictly above the threshold; upward velocity is never consumed.</summary>
    public async Task<float> OnPreFlingAsync(float availableY, CancellationToken cancellationToken = default)
    {
        Dispatcher.UIThread.VerifyAccess();
        var owner = Owner;
        cancellationToken.ThrowIfCancellationRequested();
        if (owner.IsRefreshing) return 0;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _scope!.Lifetime);
        // Upstream deliberately does not gate release by enabled or isAnimating.
        if (_distancePulled * .5f > ThresholdPx) RefreshRequested?.Invoke(this, EventArgs.Empty);
        var consumed = _distancePulled == 0 || availableY < 0 ? 0 : availableY;
        await AnimateAsync(false, linked.Token);
        _distancePulled = 0;
        return consumed;
    }
    private async Task AnimateAsync(bool refreshing, CancellationToken cancellationToken)
    {
        var state = Owner.EffectiveState;
        try
        {
            if (refreshing) await state.AnimateToThresholdAsync(cancellationToken);
            else await state.AnimateToHiddenAsync(cancellationToken);
        }
        finally
        {
            if (!refreshing) _distancePulled = _verticalOffset = 0;
            else if (_owner is not null) _distancePulled = _verticalOffset = ThresholdPx;
        }
    }
}
