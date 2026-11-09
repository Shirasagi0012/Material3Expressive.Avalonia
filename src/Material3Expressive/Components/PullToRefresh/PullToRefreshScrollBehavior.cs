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
    private IPullToRefreshScrollHost? _owner;
    private AnimationChildScope? _scope;
    private float _distancePulled, _verticalOffset;
    private bool _updatePending;
    private long _generation;
    public bool IsEnabled { get; set; } = true;
    public event EventHandler? RefreshRequested;
    internal float DistancePulled => _distancePulled;
    internal float VerticalOffset => _verticalOffset;
    internal event Action? DistanceReset;
    private float Density => (float)LayoutHelper.GetLayoutScale(Owner.Control);
    private float ThresholdPx => MathF.Floor((float)Owner.RefreshThreshold * Density + .5f);
    private IPullToRefreshScrollHost Owner => _owner ?? throw new InvalidOperationException("Attach the behavior to a mounted pull-to-refresh host first.");

    internal void Attach(IPullToRefreshScrollHost owner)
    {
        if (_owner is not null) throw new InvalidOperationException("A pull-to-refresh scroll behavior can have only one mounted owner.");
        _owner = owner;
        _scope = AnimationScope.Get(owner.Control).CreateChild("Pull-to-refresh scroll");
        var generation = ++_generation;
        var state = owner.EffectiveState;
        _ = _scope.Launch(async ct =>
        {
            // The modifier's coroutine starts after its content has composed. In
            // particular, the arrow remembers the incoming state's initial alpha.
            await Task.Yield(); if (ct.IsCancellationRequested || !IsCurrent(generation, state)) return;
            await state.SnapToAsync(owner.IsRefreshing ? 1 : 0, ct);
        }, "Initial pull position");
        _verticalOffset = owner.IsRefreshing ? ThresholdPx : 0;
    }
    internal void Detach(IPullToRefreshScrollHost owner)
    {
        if (!ReferenceEquals(_owner, owner)) return;
        ++_generation;
        _owner = null;
        var scope = _scope;
        _scope = null;
        _updatePending = false;
        scope?.Dispose();
    }

    // Element.update replaces the state without reattaching the modifier or snapping it.
    // Keep its numeric pull distance; cancel only operations owned by the old state.
    internal void StateChanged(IPullToRefreshScrollHost owner)
    {
        if (!ReferenceEquals(_owner, owner)) return;
        var pending = _updatePending;
        var generation = ++_generation;
        var previous = _scope;
        _scope = AnimationScope.Get(owner.Control).CreateChild("Pull-to-refresh scroll");
        _updatePending = false;
        previous?.Dispose();
        if (generation == _generation && pending) RefreshingChanged();
    }

    private bool IsCurrent(long generation, IPullToRefreshState state) =>
        generation == _generation && ReferenceEquals(_owner?.EffectiveState, state);
    internal void RefreshingChanged()
    {
        if (_scope is not { } scope || _updatePending) return;
        var generation = _generation;
        _updatePending = true;
        _ = scope.Launch(async ct =>
        {
            // Modifier.update is a composition commit, after the current input callback
            // and the old frame sample. Coalesce multiple changes before that commit.
            if (!await scope.WaitForNextFrameAsync(ct)) return;
            await Task.Yield(); if (ct.IsCancellationRequested || generation != _generation || _owner is not { } owner) return;
            _updatePending = false;
            await AnimateAsync(owner.EffectiveState, generation, owner.IsRefreshing, ct);
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
        var state = Owner.EffectiveState;
        var generation = _generation;
        _ = _scope!.Launch(ct => !IsCurrent(generation, state) || state.IsAnimating ? Task.CompletedTask
            : state.SnapToAsync(_verticalOffset / ThresholdPx, ct), "Pull scroll distance");
        return consumed;
    }
    private float Consume(float availableY)
    {
        if (Owner.IsRefreshing) return 0;
        return PullToRefreshScrollDistance.Consume(availableY, Density, ThresholdPx,
            ref _distancePulled, out _verticalOffset);
    }

    internal void ApplyScrollSnapshot(float distancePulled, float verticalOffset, bool publish)
    {
        _distancePulled = distancePulled;
        _verticalOffset = verticalOffset;
        if (publish && _scope is { } scope && _owner is { } owner)
        {
            var state = owner.EffectiveState;
            var generation = _generation;
            _ = scope.Launch(ct => !IsCurrent(generation, state) || state.IsAnimating ? Task.CompletedTask
                : state.SnapToAsync(_verticalOffset / ThresholdPx, ct), "Physical pull distance");
        }
    }
    /// <summary>Returns consumed vertical velocity. Refresh requires distance strictly above the threshold; upward velocity is never consumed.</summary>
    public async Task<float> OnPreFlingAsync(float availableY, CancellationToken cancellationToken = default)
    {
        Dispatcher.UIThread.VerifyAccess();
        var owner = Owner;
        var state = owner.EffectiveState;
        var scope = _scope!;
        var generation = _generation;
        cancellationToken.ThrowIfCancellationRequested();
        if (owner.IsRefreshing) return 0;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scope.Lifetime);
        // Upstream deliberately does not gate release by enabled or isAnimating.
        if (_distancePulled * .5f > ThresholdPx) RefreshRequested?.Invoke(this, EventArgs.Empty);
        // Refresh callbacks can navigate away or replace State synchronously. The old
        // release must neither dereference a detached owner nor animate the new state.
        if (!IsCurrent(generation, state)) return 0;
        var consumed = _distancePulled == 0 || availableY < 0 ? 0 : availableY;
        await AnimateAsync(state, generation, false, linked.Token);
        if (IsCurrent(generation, state)) _distancePulled = 0;
        return consumed;
    }
    private async Task AnimateAsync(IPullToRefreshState state, long generation, bool refreshing,
        CancellationToken cancellationToken)
    {
        try
        {
            if (refreshing) await state.AnimateToThresholdAsync(cancellationToken);
            else await state.AnimateToHiddenAsync(cancellationToken);
        }
        finally
        {
            if (IsCurrent(generation, state))
            {
                if (!refreshing) _distancePulled = _verticalOffset = 0;
                else _distancePulled = _verticalOffset = ThresholdPx;
                DistanceReset?.Invoke();
            }
        }
    }
}
