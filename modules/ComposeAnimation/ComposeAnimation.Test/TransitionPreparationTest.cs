using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class TransitionPreparationTest
{
    private static Task OnUi(Func<Task> action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None).Unwrap();
    private static TimeSpan Ms(int value) => TimeSpan.FromMilliseconds(value);
    private static TweenSpec<double> Tween() => new(Ms(100), easing: new LinearEasing());

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void NestedLeases_HoldAncestorsWithoutAddingDuration(double durationScale)
    {
        var state = new MutableTransitionState<bool>(true);
        using var root = new Transition<bool>(state);
        root.DurationScale = durationScale;
        var child = root.CreateChildTransition(s => s);
        var leaf = child.CreateChildTransition(s => s);
        using var first = leaf.BeginPreparation();
        using var second = leaf.BeginPreparation();
        Assert.False(state.IsIdle);
        Assert.True(root.IsPreparing);
        Assert.Empty(leaf.Animations);
        Assert.Equal(TimeSpan.Zero, root.TotalDuration);
        root.UpdateTarget(false);
        root.OnFrame(Ms(0));
        root.OnFrame(Ms(60000));
        Assert.True(root.CurrentState);
        Assert.True(leaf.CurrentState);
        Assert.False(state.IsIdle);
        first.Dispose();
        first.Dispose();
        root.OnFrame(Ms(60016));
        Assert.True(root.IsPreparing);
        Assert.True(second.IsActive);
        second.Dispose();
        Assert.False(root.IsPreparing);
        Assert.True(root.CurrentState); // Releasing is not a synchronous completion.
        root.OnFrame(Ms(60032));
        Assert.False(leaf.CurrentState);
        Assert.False(root.CurrentState);
        Assert.True(state.IsIdle);
    }

    [Fact]
    public void Preparation_DoesNotPauseExistingAnimation_AndTransfersToLateAnimation()
    {
        using var root = new Transition<bool>(false);
        var value = root.AnimateDouble(s => s ? 100 : 0, _ => Tween());
        using var preparation = root.BeginPreparation();
        root.UpdateTarget(true);
        root.OnFrame(Ms(0));
        root.OnFrame(Ms(50));
        Assert.Equal(50, value.Value, 8);
        root.OnFrame(Ms(100));
        Assert.Equal(100, value.Value);
        Assert.False(root.CurrentState);
        using var deferred = root.CreateDeferredAnimation(new DoubleConverter());
        var late = deferred.Animate(_ => Tween(), s => s ? 200 : 0);
        preparation.Dispose();
        root.OnFrame(Ms(150));
        Assert.Equal(100, late.Value, 8);
        Assert.False(root.CurrentState);
        root.OnFrame(Ms(200));
        Assert.Equal(200, late.Value);
        Assert.True(root.CurrentState);
        Assert.False(root.IsRunning);
    }

    [Fact]
    public void ReverseToOriginalState_StillWaitsForPreparation()
    {
        var state = new MutableTransitionState<bool>(true);
        using var root = new Transition<bool>(state);
        using var preparation = root.BeginPreparation();
        root.UpdateTarget(false);
        root.OnFrame(Ms(0));
        root.UpdateTarget(true);
        root.OnFrame(Ms(16));
        Assert.False(state.IsIdle);
        preparation.Dispose();
        root.OnFrame(Ms(32));
        Assert.True(state.CurrentState);
        Assert.True(state.IsIdle);
    }

    [Fact]
    public void DisposingChild_RevokesItsLeases_WithoutReleasingSibling()
    {
        using var root = new Transition<bool>(true);
        var child = root.CreateChildTransition(s => s);
        var sibling = root.CreateChildTransition(s => s);
        var childLease = child.BeginPreparation();
        var siblingLease = sibling.BeginPreparation();
        root.UpdateTarget(false);
        root.OnFrame(Ms(0));
        child.Dispose();
        childLease.Dispose();
        Assert.False(childLease.IsActive);
        Assert.True(siblingLease.IsActive);
        root.OnFrame(Ms(16));
        Assert.True(root.CurrentState);
        root.Dispose();
        Assert.False(siblingLease.IsActive);
        siblingLease.Dispose();
        Assert.Throws<ObjectDisposedException>(() => root.BeginPreparation());
    }

    [Fact]
    public void ManualSeek_RevokesPendingPreparation_AndSamplesNormally()
    {
        using var root = new Transition<bool>(false);
        var child = root.CreateChildTransition(s => s);
        var value = child.AnimateDouble(s => s ? 100 : 0, _ => Tween());
        using var preparation = child.BeginPreparation();
        root.SetPlaytimeAfterInitialAndTargetStateEstablished(false, true, Ms(50));
        Assert.False(preparation.IsActive);
        Assert.False(root.IsPreparing);
        Assert.Equal(50, value.Value, 8);
        Assert.Throws<InvalidOperationException>(() => child.BeginPreparation());
        root.SetPlaytimeAfterInitialAndTargetStateEstablished(false, true, Ms(25));
        Assert.Equal(25, value.Value, 8);
    }

    [Fact]
    public async Task SeekableSnap_RevokesPendingPreparation()
    {
        var state = new SeekableTransitionState<bool>(false);
        using var root = new Transition<bool>(state);
        using var preparation = root.BeginPreparation();
        await state.SnapToAsync(true);
        Assert.False(preparation.IsActive);
        Assert.True(state.CurrentState);
        Assert.False(root.IsPreparing);
        Assert.False(root.IsRunning);
        using var next = root.BeginPreparation();
        Assert.True(next.IsActive);
        await state.SnapToAsync(true); // Even snapping to the same state cancels preparation.
        Assert.False(next.IsActive);
        Assert.False(root.IsPreparing);
    }

    [Fact]
    public void ReleaseDuringNotification_DoesNotLeakAQueuedReservation()
    {
        using var root = new Transition<bool>(false);
        TransitionPreparationLease? lease = null;
        root.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(root.TargetState)) return;
            lease = root.BeginPreparation();
            lease.Dispose();
        };
        root.UpdateTarget(true);
        root.OnFrame(Ms(0));
        Assert.NotNull(lease);
        Assert.False(lease.IsActive);
        Assert.False(root.IsPreparing);
        Assert.True(root.CurrentState);
        Assert.False(root.IsRunning);
    }

    [Fact]
    public Task FrameLoop_CompletesAfterRelease_AndCancellationDoesNotOwnCallerLease() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var root = new Transition<bool>(true);
        using var lease = root.BeginPreparation();
        using var cancellation = new CancellationTokenSource();
        root.UpdateTarget(false);
        var loop = root.RunAsync(window.AnimationRoot, cancellation.Token);
        clock.Pump(Ms(16));
        Assert.True(root.CurrentState);
        Assert.Equal(1, clock.PendingFrameCount);
        lease.Dispose();
        clock.Pump(Ms(16));
        Assert.False(root.CurrentState);
        Assert.False(root.IsRunning);
        Assert.Equal(0, clock.PendingFrameCount);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loop);

        // A loop cancellation is not disposal of the presentation. Its owner must release
        // outstanding preparation (usually via using/finally), or dispose the transition.
        using var pending = root.BeginPreparation();
        using var secondCancellation = new CancellationTokenSource();
        var secondLoop = root.RunAsync(window.AnimationRoot, secondCancellation.Token);
        secondCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => secondLoop);
        Assert.True(pending.IsActive);
        Assert.True(root.IsPreparing);
        Assert.True(root.IsRunning);
        pending.Dispose();
        root.OnFrame(Ms(48));
        Assert.False(root.IsPreparing);
        Assert.False(root.IsRunning);
        window.Close();
    });

    [Fact]
    // On the UI thread: the transition is owned by its constructing thread, so the lease's
    // release after the awaited delay must resume there.
    public Task CancellationFinally_ReleasesReservationWithoutDisposingTransition() => OnUi(async () =>
    {
        using var root = new Transition<bool>(true);
        using var cancellation = new CancellationTokenSource();
        TransitionPreparationLease? lease = null;
        async Task Prepare()
        {
            using var reservation = root.BeginPreparation();
            lease = reservation;
            root.UpdateTarget(false);
            await Task.Delay(Timeout.Infinite, cancellation.Token);
        }
        var pending = Prepare();
        root.OnFrame(Ms(0));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.False(lease!.IsActive);
        root.OnFrame(Ms(16));
        Assert.False(root.CurrentState);
        root.UpdateTarget(true);
        root.OnFrame(Ms(32));
        Assert.True(root.CurrentState);
    });

}
