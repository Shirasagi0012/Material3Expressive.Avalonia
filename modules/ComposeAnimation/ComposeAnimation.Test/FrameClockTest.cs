using Avalonia.Animation.Easings;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public sealed class FrameClockTest
{
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

    private static Task Run(Action test) => HeadlessApp.Session.Dispatch(test, CancellationToken.None);

    // Deliver one source frame and run the clock's fixed dispatcher hop.
    private static void Advance(ManualAnimationFrameClock source, TimeSpan? delta = null)
    {
        source.AdvanceBy(delta ?? Frame);
        Dispatcher.UIThread.RunJobs(FrameClock.ResumePriority);
    }

    private static async Task Record(FrameClock clock, List<string> log, string id)
    {
        var at = await clock.WaitAsync(CancellationToken.None);
        log.Add($"{id}@{at.TotalMilliseconds}");
    }

    [Fact]
    public Task Waiters_ShareOneSourceWait_AndResumeInRegistrationOrderAtOneTimestamp() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        var log = new List<string>();
        var first = Record(clock, log, "a");
        var second = Record(clock, log, "b");
        var third = Record(clock, log, "c");
        Assert.Equal(1, source.PendingFrameCount);

        Advance(source);

        Assert.True(first.IsCompleted && second.IsCompleted && third.IsCompleted);
        Assert.Equal(["a@16", "b@16", "c@16"], log);
        Assert.Equal(0, source.PendingFrameCount);
    });

    [Fact]
    public Task WaiterRegisteredDuringBatch_GoesToNextFrame() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        var log = new List<string>();
        Task? late = null;
        async Task First()
        {
            await clock.WaitAsync(default);
            log.Add("first");
            late = Record(clock, log, "late");
        }
        var first = First();

        Advance(source);
        Assert.True(first.IsCompleted);
        Assert.NotNull(late);
        Assert.False(late!.IsCompleted);
        Assert.Equal(1, source.PendingFrameCount);

        Advance(source);
        Assert.Equal(["first", "late@32"], log);
    });

    [Fact]
    public Task Idle_NoSourceWaitWithoutWaiters_AndLastCancelReleasesSourceWait() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        using var hook = clock.RegisterBeforeFrame(() => { });
        Assert.Equal(0, source.PendingFrameCount);

        using var a = new CancellationTokenSource();
        using var b = new CancellationTokenSource();
        var first = clock.WaitAsync(a.Token).AsValueTask().AsTask();
        var second = clock.WaitAsync(b.Token).AsValueTask().AsTask();
        Assert.Equal(1, source.PendingFrameCount);
        a.Cancel();
        Assert.Equal(1, source.PendingFrameCount);
        b.Cancel();
        Assert.Equal(0, source.PendingFrameCount);
        Assert.True(first.IsCanceled && second.IsCanceled);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, source.PendingFrameCount);

        // A new waiter after the canceled wait was observed requests exactly one new wait.
        var third = clock.WaitAsync(default).AsValueTask().AsTask();
        Assert.Equal(1, source.PendingFrameCount);
        Advance(source);
        Assert.Equal(Frame, third.Result);
    });

    [Fact]
    public Task CanceledWaiter_IsNotGivenTheFrame() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        using var cancellation = new CancellationTokenSource();
        var canceled = clock.WaitAsync(cancellation.Token).AsValueTask().AsTask();
        var kept = clock.WaitAsync(default).AsValueTask().AsTask();
        cancellation.Cancel();

        Advance(source);

        Assert.True(canceled.IsCanceled);
        Assert.Equal(Frame, kept.Result);
    });

    [Fact]
    public Task SourceFailure_IsReportedOnce_ThenFailsCurrentAndFutureWaiters() => Run(() =>
    {
        var source = new PendingFailClock();
        var reports = new List<Exception>();
        using var clock = new FrameClock(source, reports.Add);
        var first = clock.WaitAsync(default).AsValueTask().AsTask();
        var second = clock.WaitAsync(default).AsValueTask().AsTask();
        Assert.Equal(1, source.Requests);

        source.Fail();
        Dispatcher.UIThread.RunJobs();

        Assert.Same(source.Failure, Assert.Single(reports));
        Assert.Same(source.Failure, first.Exception!.InnerException);
        Assert.Same(source.Failure, second.Exception!.InnerException);
        var later = clock.WaitAsync(default).AsValueTask().AsTask();
        Assert.Same(source.Failure, later.Exception!.InnerException);
        Assert.Equal(1, source.Requests);
        Assert.Single(reports);
    });

    [Fact]
    public Task SourceFailure_WithThrowingAfterWaiterHook_StillFailsEveryWaiter() => Run(() =>
    {
        var source = new PendingFailClock();
        using var clock = new FrameClock(source);
        using var hook = clock.RegisterAfterWaiter(() => throw new ArgumentException("hook"));
        var waiters = Enumerable.Range(0, 3).Select(_ => clock.WaitAsync(default).AsValueTask().AsTask()).ToArray();
        source.Fail();
        Dispatcher.UIThread.RunJobs();
        Assert.All(waiters, w => Assert.Same(source.Failure, w.Exception!.InnerException));
    });

    [Fact]
    public Task Dispose_CancelsPendingWaitersAndSourceWait() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        var clock = new FrameClock(source);
        var pending = clock.WaitAsync(default).AsValueTask().AsTask();

        clock.Dispose();

        Assert.True(pending.IsCanceled);
        Assert.Equal(0, source.PendingFrameCount);
        Dispatcher.UIThread.RunJobs();
        Assert.Throws<ObjectDisposedException>(() => clock.WaitAsync(default));
    });

    [Fact]
    public Task BeforeFrameWork_JoinsTheArrivingFrame() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        var log = new List<string>();
        Task? joined = null;
        using var hook = clock.RegisterBeforeFrame(() => joined ??= Record(clock, log, "joined"));
        var early = Record(clock, log, "early");

        Advance(source);

        Assert.True(early.IsCompleted && joined!.IsCompleted);
        Assert.Equal(["early@16", "joined@16"], log);
        Assert.Equal(0, source.PendingFrameCount);
    });

    [Fact]
    public Task Hooks_RunInRegistrationOrder_AroundEachWaiter() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        var log = new List<string>();
        using var b1 = clock.RegisterBeforeFrame(() => log.Add("before1"));
        using var b2 = clock.RegisterBeforeFrame(() => log.Add("before2"));
        using var a1 = clock.RegisterAfterWaiter(() => log.Add("after1"));
        using var a2 = clock.RegisterAfterWaiter(() => log.Add("after2"));
        _ = Record(clock, log, "x");
        _ = Record(clock, log, "y");

        Advance(source);

        Assert.Equal(["before1", "before2", "x@16", "after1", "after2", "y@16", "after1", "after2"], log);
    });

    [Fact]
    public Task HookRegistrationChangesDuringFrame_TakeEffectNextFrame() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        var log = new List<string>();
        IDisposable? second = null;
        IDisposable? added = null;
        using var first = clock.RegisterBeforeFrame(() =>
        {
            log.Add("first");
            second?.Dispose();
            added ??= clock.RegisterBeforeFrame(() => log.Add("added"));
        });
        second = clock.RegisterBeforeFrame(() => log.Add("second"));
        _ = clock.WaitAsync(default).AsValueTask().AsTask();
        Advance(source);
        Assert.Equal(["first", "second"], log);

        log.Clear();
        _ = clock.WaitAsync(default).AsValueTask().AsTask();
        Advance(source);
        Assert.Equal(["first", "added"], log);
        added!.Dispose();
    });

    [Fact]
    public Task ThrowingBeforeFrameHook_FailsTheBatch_ClockStaysUsable() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        var boom = new InvalidOperationException("hook");
        var hook = clock.RegisterBeforeFrame(() => throw boom);
        var a = clock.WaitAsync(default).AsValueTask().AsTask();
        var b = clock.WaitAsync(default).AsValueTask().AsTask();

        Advance(source);

        Assert.Same(boom, a.Exception!.InnerException);
        Assert.Same(boom, b.Exception!.InnerException);
        hook.Dispose();
        var next = clock.WaitAsync(default).AsValueTask().AsTask();
        Advance(source);
        Assert.Equal(Frame * 2, next.Result);
    });

    [Fact]
    public Task ThrowingAfterWaiterHook_FailsTheRestOfTheBatch() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        var boom = new InvalidOperationException("hook");
        using var hook = clock.RegisterAfterWaiter(() => throw boom);
        var a = clock.WaitAsync(default).AsValueTask().AsTask();
        var b = clock.WaitAsync(default).AsValueTask().AsTask();

        Advance(source);

        Assert.Equal(Frame, a.Result);
        Assert.Same(boom, b.Exception!.InnerException);
    });

    [Theory]
    [InlineData(0, false)] // Background, awaiting the FrameAwaitable
    [InlineData(1, false)] // Loaded, awaiting the FrameAwaitable
    [InlineData(0, true)]  // Background, through the ValueTask adapter
    [InlineData(1, true)]  // Loaded, through the ValueTask adapter
    public Task Resumption_IsInlineInTheBatch_WhateverContextTheWaiterStartedFrom(int start, bool adapter) => Run(() =>
    {
        var priority = start == 0 ? DispatcherPriority.Background : DispatcherPriority.Loaded;
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        var log = new List<string>();
        SynchronizationContext? batchContext = null;
        using var before = clock.RegisterBeforeFrame(() => batchContext = SynchronizationContext.Current);
        using var after = clock.RegisterAfterWaiter(() => log.Add("after"));
        Task? waiter = null;
        SynchronizationContext? started = null;
        SynchronizationContext? resumedOn = null;
        Dispatcher.UIThread.Post(() =>
        {
            started = SynchronizationContext.Current;
            waiter = Wait();
        }, priority);
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(waiter);
        Assert.Equal(1, source.PendingFrameCount);

        source.AdvanceBy(Frame);
        // Only jobs at the clock's fixed priority (or higher) run: no Background/Loaded hop.
        Dispatcher.UIThread.RunJobs(FrameClock.ResumePriority);

        Assert.True(waiter!.IsCompleted);
        // Inline inside the batch: the continuation ran before the after-waiter hook.
        Assert.Equal(["resumed", "after"], log);
        // The batch's own dispatcher context stays current; nothing was swapped in.
        Assert.IsAssignableFrom<AvaloniaSynchronizationContext>(resumedOn);
        Assert.Same(batchContext, resumedOn);
        Assert.NotSame(started, resumedOn);

        async Task Wait()
        {
            if (adapter) await clock.WaitAsync(default).AsValueTask();
            else await clock.WaitAsync(default);
            resumedOn = SynchronizationContext.Current;
            log.Add("resumed");
        }
    });

    [Fact]
    public Task Resumption_NeedsNoPost_WaitersCompleteDuringTheBatchJob() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        var resumed = new List<int>();
        var completedAtHook = new List<bool>();
        Task a = null!, b = null!;
        async Task Wait(int id) { await clock.WaitAsync(default); resumed.Add(id); }
        a = Wait(1);
        b = Wait(2);
        using var after = clock.RegisterAfterWaiter(() => completedAtHook.Add(resumed.Count == completedAtHook.Count + 1));

        source.AdvanceBy(Frame);
        // Exactly one job (the clock's hop) is run; every continuation finishes inside it.
        var ran = 0;
        Dispatcher.UIThread.Post(() => ran++, DispatcherPriority.Normal); // queued behind the hop
        Assert.Empty(resumed);
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Send); // nothing at Send: no inline-before-hop
        Assert.Empty(resumed);
        Dispatcher.UIThread.RunJobs(FrameClock.ResumePriority);
        Assert.Equal([1, 2], resumed);
        Assert.Equal([true, true], completedAtHook);
        Assert.True(a.IsCompletedSuccessfully && b.IsCompletedSuccessfully);
        Assert.Equal(1, ran);
    });

    [Fact]
    public Task CancelFromOffTheUiThread_IsPostedAndReleasesTheSourceWait() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        using var cancellation = new CancellationTokenSource();
        var uiThread = Environment.CurrentManagedThreadId;
        int? continuationThread = null;
        async Task Wait()
        {
            try { await clock.WaitAsync(cancellation.Token); }
            catch (OperationCanceledException) { continuationThread = Environment.CurrentManagedThreadId; throw; }
        }
        var waiter = Wait();
        Assert.Equal(1, source.PendingFrameCount);

        // A dedicated thread: Task.Run(...).Wait() may inline the cancel onto this UI thread.
        var canceler = new Thread(cancellation.Cancel);
        canceler.Start();
        canceler.Join();
        Assert.False(waiter.IsCompleted); // posted, not completed on the pool thread

        Dispatcher.UIThread.RunJobs(FrameClock.ResumePriority);
        Assert.True(waiter.IsCanceled);
        Assert.Equal(uiThread, continuationThread);
        Assert.Equal(0, source.PendingFrameCount);
    });

    [Fact]
    public Task AnimatableInterrupt_CancelsFirstRunOnFrameClock() => Run(() =>
    {
        var source = new ManualAnimationFrameClock();
        using var clock = new FrameClock(source);
        var animatable = new Animatable<double>(new DoubleConverter(), 0, new Adapter(clock));
        var tween = new TweenSpec<double>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing());

        var first = animatable.AnimateToAsync(100, tween);
        Advance(source);
        Advance(source, TimeSpan.FromMilliseconds(50));
        var mid = animatable.Value;
        Assert.InRange(mid, 40, 60);

        var second = animatable.AnimateToAsync(0, tween);
        Dispatcher.UIThread.RunJobs();
        Assert.True(first.IsCanceled);

        Advance(source);
        Assert.True(animatable.Value <= mid + 0.5, $"Continued toward 100: {mid} -> {animatable.Value}");
        Assert.False(second.IsCompleted);
    });

    private sealed class Adapter(FrameClock clock) : IAnimationFrameClock
    {
        public void VerifyAccess() => clock.VerifyAccess();
        public ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken) => clock.WaitAsync(cancellationToken).AsValueTask();
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
