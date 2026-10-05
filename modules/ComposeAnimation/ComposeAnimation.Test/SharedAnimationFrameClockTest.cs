using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public sealed class SharedAnimationFrameClockTest
{
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

    [Fact]
    public async Task MultipleWaiters_ShareOneInnerPendingAndWakeTogether()
    {
        var inner = new ManualAnimationFrameClock();
        using var shared = new SharedAnimationFrameClock(inner);

        var first = shared.NextFrameAsync(CancellationToken.None).AsTask();
        var second = shared.NextFrameAsync(CancellationToken.None).AsTask();

        Assert.Equal(1, inner.PendingFrameCount);
        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);

        inner.AdvanceBy(Frame);

        Assert.Equal(Frame, await first);
        Assert.Equal(Frame, await second);
        Assert.Equal(0, inner.PendingFrameCount);
    }

    [Fact]
    public async Task Waiters_WakeInRegistrationOrder()
    {
        var inner = new ManualAnimationFrameClock();
        var order = new List<int>();
        using var shared = new SharedAnimationFrameClock(inner, afterEachWaiter: () => { });

        var first = AwaitAndRecord(shared, 1, order);
        var second = AwaitAndRecord(shared, 2, order);

        inner.AdvanceBy(Frame);
        await Task.WhenAll(first, second);

        Assert.Equal([1, 2], order);
    }

    [Fact]
    public async Task InnerFailure_FailsEveryWaiterAndInvokesCallbackOnce()
    {
        var inner = new PendingFailClock();
        var failures = 0;
        Exception? reported = null;
        using var shared = new SharedAnimationFrameClock(inner, onInnerFailure: error =>
        {
            failures++;
            reported = error;
        });

        var first = shared.NextFrameAsync(CancellationToken.None).AsTask();
        var second = shared.NextFrameAsync(CancellationToken.None).AsTask();
        Assert.Equal(1, inner.Requests);

        inner.Fail();

        await Assert.ThrowsAsync<InvalidOperationException>(() => first);
        await Assert.ThrowsAsync<InvalidOperationException>(() => second);
        Assert.Same(inner.Failure, reported);
        Assert.Equal(1, failures);
        Assert.Equal(1, inner.Requests);
    }

    [Fact]
    public async Task CancelingLastWaiter_ReleasesInnerPending()
    {
        var inner = new ManualAnimationFrameClock();
        using var shared = new SharedAnimationFrameClock(inner);
        using var cancellation = new CancellationTokenSource();

        var pending = shared.NextFrameAsync(cancellation.Token).AsTask();
        Assert.Equal(1, inner.PendingFrameCount);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, inner.PendingFrameCount);
    }

    [Fact]
    public async Task CanceledWaiter_IsNotGivenTheFrame()
    {
        var inner = new ManualAnimationFrameClock();
        using var shared = new SharedAnimationFrameClock(inner);
        using var cancellation = new CancellationTokenSource();

        var canceled = shared.NextFrameAsync(cancellation.Token).AsTask();
        var kept = shared.NextFrameAsync(CancellationToken.None).AsTask();
        cancellation.Cancel();

        inner.AdvanceBy(Frame);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.Equal(Frame, await kept);
    }

    [Fact]
    public async Task BeforeEachFrame_CanRegisterWaiterForTheArrivingFrame()
    {
        var inner = new ManualAnimationFrameClock();
        Task<TimeSpan>? late = null;
        SharedAnimationFrameClock? shared = null;
        shared = new SharedAnimationFrameClock(inner, beforeEachFrame: () =>
        {
            late = shared!.NextFrameAsync(CancellationToken.None).AsTask();
        });
        using (shared)
        {
            var early = shared.NextFrameAsync(CancellationToken.None).AsTask();
            inner.AdvanceBy(Frame);
            Assert.Equal(Frame, await early);
            Assert.NotNull(late);
            Assert.Equal(Frame, await late!);
        }
    }

    [Fact]
    public async Task AnimatableInterrupt_CancelsFirstRunOnSharedClock()
    {
        var inner = new ManualAnimationFrameClock();
        using var shared = new SharedAnimationFrameClock(inner);
        var animatable = new Animatable<double>(new DoubleConverter(), 0, shared);
        var tween = new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing());

        var first = animatable.AnimateToAsync(100, tween);
        inner.AdvanceBy(Frame);
        inner.AdvanceBy(TimeSpan.FromMilliseconds(50));
        var mid = animatable.Value;
        Assert.InRange(mid, 40, 60);

        var second = animatable.AnimateToAsync(0, tween);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);

        inner.AdvanceBy(Frame);
        Assert.True(animatable.Value <= mid + 0.5, $"Continued toward 100: {mid} -> {animatable.Value}");
        Assert.False(second.IsCompleted);
    }

    [Fact]
    public async Task Dispose_CancelsPendingWaiters()
    {
        var inner = new ManualAnimationFrameClock();
        var shared = new SharedAnimationFrameClock(inner);
        var pending = shared.NextFrameAsync(CancellationToken.None).AsTask();

        shared.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, inner.PendingFrameCount);
    }

    [Fact]
    public async Task FailurePublicationCallbackFault_CannotStrandOtherWaiters()
    {
        var inner = new PendingFailClock();
        using var shared = new SharedAnimationFrameClock(inner,
            afterEachWaiter: () => throw new ArgumentException("callback"));
        var first = shared.NextFrameAsync(default).AsTask();
        var second = shared.NextFrameAsync(default).AsTask();
        var third = shared.NextFrameAsync(default).AsTask();
        inner.Fail();
        Assert.True(first.IsCompleted && second.IsCompleted && third.IsCompleted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => first);
        await Assert.ThrowsAsync<InvalidOperationException>(() => second);
        await Assert.ThrowsAsync<InvalidOperationException>(() => third);
        Assert.Equal(1, inner.Requests);
    }

    private static async Task AwaitAndRecord(
        SharedAnimationFrameClock clock, int id, List<int> order)
    {
        await clock.NextFrameAsync(CancellationToken.None);
        order.Add(id);
    }

    private sealed class PendingFailClock : IAnimationFrameClock
    {
        private TaskCompletionSource<TimeSpan>? _pending;
        public Exception Failure { get; } = new InvalidOperationException("clock failed");
        public int Requests { get; private set; }
        public void VerifyAccess() { }
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
        {
            Requests++;
            _pending = new TaskCompletionSource<TimeSpan>();
            return new ValueTask<TimeSpan>(_pending.Task);
        }

        public void Fail() => _pending?.TrySetException(Failure);
    }
}
