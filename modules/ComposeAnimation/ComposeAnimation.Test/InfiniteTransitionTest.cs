using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class InfiniteTransitionTest
{
    private static TimeSpan Ms(double value) => TimeSpan.FromMilliseconds(value);

    private static TweenSpec<T> Tween<T>(double duration = 1000)
        => new(Ms(duration), easing: new LinearEasing());

    private static InfiniteRepeatableSpec<T> Repeat<T>(
        IDurationBasedAnimationSpec<T> animation,
        RepeatMode mode = RepeatMode.Restart,
        StartOffset offset = default)
        => new(animation, mode, offset);

    private static async Task WaitForPendingFrame(ManualAnimationFrameClock clock)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (clock.PendingFrameCount == 0 && !timeout.IsCancellationRequested)
        {
            Dispatcher.UIThread.RunJobs();
            try { await Task.Delay(1, timeout.Token); }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested) { }
        }
        Assert.True(clock.PendingFrameCount > 0);
    }

    private static async Task WaitForNoPendingFrame(ManualAnimationFrameClock clock)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (clock.PendingFrameCount > 0 && !timeout.IsCancellationRequested)
        {
            Dispatcher.UIThread.RunJobs();
            try { await Task.Delay(1, timeout.Token); }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested) { }
        }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [Fact]
    public void Restart_SamplesAgainstTheSameInfiniteSpec()
    {
        var spec = Repeat(Tween<double>(1000));
        var expected = new TargetBasedAnimation<double>(spec, new DoubleConverter(), 0, 100);
        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        var value = transition.AnimateDouble(0, 100, spec);

        Assert.Equal(0, value.Value);
        Assert.False(transition.IsRunning);
        transition.OnFrame(Ms(0));
        Assert.Equal(0, value.Value);
        Assert.True(transition.IsRunning);
        Assert.Equal(TimeSpan.Zero, transition.PlayTime);

        transition.OnFrame(Ms(250));
        Assert.Equal(expected.GetValue(Ms(250)), value.Value, 8);
        Assert.Equal(Ms(250), transition.PlayTime);

        transition.OnFrame(Ms(1000));
        Assert.Equal(expected.GetValue(Ms(1000)), value.Value, 8);

        transition.OnFrame(Ms(1250));
        Assert.Equal(expected.GetValue(Ms(1250)), value.Value, 8);
        Assert.Equal(25, value.Value, 8);
        Assert.False(value.IsFinished);
        Assert.True(transition.IsRunning);
    }

    [Fact]
    public void ReverseAndColor_ShareParentPlayTime()
    {
        var keyframes = new KeyframesSpecConfig<double> { Duration = Ms(1000) };
        keyframes.At(0, 0);
        keyframes.At(400, 200);
        keyframes.At(1000, 1000);
        var keyframeSpec = new KeyframesSpec<double>(keyframes);
        var floatSpec = Repeat(keyframeSpec, RepeatMode.Reverse);
        var colorSpec = Repeat(Tween<Color>(1000));

        var expectedFloat = new TargetBasedAnimation<double>(floatSpec, new DoubleConverter(), 0, 0);
        var expectedColor = new TargetBasedAnimation<Color>(colorSpec, new ColorConverter(), Colors.Red, Colors.Green);

        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        var animFloat = transition.AnimateDouble(0, 0, floatSpec);
        var animColor = transition.AnimateColor(Colors.Red, Colors.Green, colorSpec);

        transition.OnFrame(Ms(0));
        foreach (var playTime in new double[] { 400, 1000, 1400, 2000, 2100 }.Select(Ms))
        {
            transition.OnFrame(playTime);
            Assert.Equal(expectedFloat.GetValue(playTime), animFloat.Value, 8);
            Assert.Equal(expectedColor.GetValue(playTime), animColor.Value);
            Assert.Equal(playTime, transition.PlayTime);
        }
    }

    [Fact]
    public void LateRegistration_JoinsExistingParentPhase()
    {
        var spec = Repeat(Tween<double>(1000));
        var expected = new TargetBasedAnimation<double>(spec, new DoubleConverter(), 0, 100);
        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        var first = transition.AnimateDouble(0, 100, spec);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(250));
        Assert.Equal(25, first.Value, 8);

        var late = transition.AnimateDouble(0, 100, spec);
        Assert.Equal(0, late.Value);
        transition.OnFrame(Ms(250));
        Assert.Equal(expected.GetValue(Ms(250)), late.Value, 8);
        Assert.Equal(first.Value, late.Value, 8);
        Assert.Equal(2, transition.Animations.Count);
    }

    [Fact]
    public void Update_RestartsWithoutContinuityOnNextFrame()
    {
        var spec = Repeat(Tween<double>(1000));
        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        var value = transition.AnimateDouble(0, 100, spec);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(400));
        Assert.Equal(40, value.Value, 8);
        var original = value.Animation;

        value.Update(0, 200);
        Assert.Equal(40, value.Value, 8);
        Assert.Equal(200, value.TargetValue);
        Assert.NotSame(original, value.Animation);

        transition.OnFrame(Ms(416));
        var restarted = new TargetBasedAnimation<double>(spec, new DoubleConverter(), 0, 200);
        Assert.Equal(restarted.GetValue(TimeSpan.Zero), value.Value, 8);
        Assert.Equal(0, value.Value, 8);

        transition.OnFrame(Ms(516));
        Assert.Equal(restarted.GetValue(Ms(100)), value.Value, 8);
        Assert.Equal(20, value.Value, 8);
    }

    [Fact]
    public void Update_SameValuesIsNoOpAndSpecChangeRestarts()
    {
        var spec = Repeat(Tween<double>(1000));
        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        var value = transition.AnimateDouble(0, 100, spec);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(200));
        var original = value.Animation;
        value.Update(0, 100);
        Assert.Same(original, value.Animation);

        var slower = Repeat(Tween<double>(2000));
        value.Update(0, 100, slower);
        Assert.NotSame(original, value.Animation);
        transition.OnFrame(Ms(216));
        Assert.Equal(0, value.Value, 8);
        transition.OnFrame(Ms(1216));
        Assert.Equal(50, value.Value, 8);
    }

    [Fact]
    public void StartOffset_DelayAndFastForwardAreAppliedByTheSpec()
    {
        var tween = Tween<double>(1000);
        var delayed = Repeat(tween, offset: new StartOffset(250));
        var fastForwarded = Repeat(tween, offset: new StartOffset(250, StartOffsetType.FastForward));
        var expectedDelay = new TargetBasedAnimation<double>(delayed, new DoubleConverter(), 0, 100);
        var expectedFast = new TargetBasedAnimation<double>(fastForwarded, new DoubleConverter(), 0, 100);

        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        var delayedValue = transition.AnimateDouble(0, 100, delayed);
        var fastValue = transition.AnimateDouble(0, 100, fastForwarded);
        transition.OnFrame(Ms(0));
        Assert.Equal(expectedDelay.GetValue(TimeSpan.Zero), delayedValue.Value, 8);
        Assert.Equal(expectedFast.GetValue(TimeSpan.Zero), fastValue.Value, 8);
        Assert.Equal(0, delayedValue.Value, 8);
        Assert.Equal(25, fastValue.Value, 8);

        transition.OnFrame(Ms(250));
        Assert.Equal(expectedDelay.GetValue(Ms(250)), delayedValue.Value, 8);
        Assert.Equal(expectedFast.GetValue(Ms(250)), fastValue.Value, 8);
        Assert.Equal(0, delayedValue.Value, 8);
        Assert.Equal(50, fastValue.Value, 8);

        transition.OnFrame(Ms(500));
        Assert.Equal(25, delayedValue.Value, 8);
        Assert.Equal(75, fastValue.Value, 8);
    }

    [Fact]
    public void DurationScale_SlowsThenRestartOnChange()
    {
        var spec = Repeat(Tween<double>(1000));
        var expected = new TargetBasedAnimation<double>(spec, new DoubleConverter(), 0, 100);
        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        var value = transition.AnimateDouble(0, 100, spec);
        transition.DurationScale = 2;
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(200));
        Assert.Equal(Ms(100), transition.PlayTime);
        Assert.Equal(expected.GetValue(Ms(100)), value.Value, 8);
        Assert.Equal(10, value.Value, 8);

        // Unlike Transition, a scale change restarts every child from the initial value.
        transition.DurationScale = 1;
        transition.OnFrame(Ms(300));
        Assert.Equal(TimeSpan.Zero, transition.PlayTime);
        Assert.Equal(0, value.Value, 8);
        transition.OnFrame(Ms(400));
        Assert.Equal(expected.GetValue(Ms(100)), value.Value, 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => transition.DurationScale = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => transition.DurationScale = -1);
    }

    [Fact]
    public void DurationScaleZero_SnapsToTargetAndResumeRestarts()
    {
        var spec = Repeat(Tween<double>(1000));
        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        var value = transition.AnimateDouble(0, 100, spec);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(250));
        Assert.Equal(25, value.Value, 8);

        transition.DurationScale = 0;
        transition.OnFrame(Ms(266));
        Assert.Equal(100, value.Value);
        Assert.False(transition.IsRunning);
        Assert.False(value.IsFinished);

        transition.DurationScale = 1;
        transition.OnFrame(Ms(282));
        Assert.Equal(0, value.Value, 8);
        Assert.True(transition.IsRunning);
        transition.OnFrame(Ms(382));
        Assert.Equal(10, value.Value, 8);
    }

    [Fact]
    public async Task DurationScaleZero_RunAsyncStopsRequestingFrames()
    {
        var clock = new ManualAnimationFrameClock();
        using var transition = new InfiniteTransition(clock);
        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        var task = transition.RunAsync();
        await WaitForPendingFrame(clock);
        clock.SendFrame(Ms(0));
        clock.SendFrame(Ms(200));
        Assert.Equal(20, value.Value, 8);
        Assert.Equal(1, clock.PendingFrameCount);

        transition.DurationScale = 0;
        clock.SendFrame(Ms(216));
        Assert.Equal(100, value.Value);
        Assert.False(transition.IsRunning);
        await WaitForNoPendingFrame(clock);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.False(task.IsCompleted);

        transition.DurationScale = 1;
        await WaitForPendingFrame(clock);
        clock.SendFrame(Ms(232));
        Assert.Equal(0, value.Value, 8);
        clock.SendFrame(Ms(332));
        Assert.Equal(10, value.Value, 8);

        transition.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public void RemovingLastChild_LeavesTransitionIdle()
    {
        var spec = Repeat(Tween<double>(1000));
        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        var first = transition.AnimateDouble(0, 100, spec);
        var second = transition.AnimateDouble(0, 200, spec);
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(250));
        Assert.True(transition.IsRunning);

        first.Dispose();
        transition.OnFrame(Ms(266));
        Assert.True(transition.IsRunning);
        Assert.Single(transition.Animations);
        Assert.Equal(53.2, second.Value, 8);

        second.Dispose();
        transition.OnFrame(Ms(282));
        Assert.False(transition.IsRunning);
        Assert.Empty(transition.Animations);
        Assert.Equal(53.2, second.Value, 8);
    }

    [Fact]
    public async Task RunAsync_IdleLoopWakesWhenChildIsAdded()
    {
        var clock = new ManualAnimationFrameClock();
        using var transition = new InfiniteTransition(clock);
        var task = transition.RunAsync();
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.False(transition.IsRunning);

        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        await WaitForPendingFrame(clock);
        clock.SendFrame(Ms(0));
        clock.SendFrame(Ms(250));
        Assert.Equal(25, value.Value, 8);
        Assert.True(transition.IsRunning);

        value.Dispose();
        clock.SendFrame(Ms(266));
        await WaitForNoPendingFrame(clock);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.False(transition.IsRunning);
        Assert.False(task.IsCompleted);

        var again = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        Assert.Equal(0, again.Value);
        await WaitForPendingFrame(clock);
        clock.SendFrame(Ms(282));
        // RunAsync is still the same loop, so the parent timeline was not reset.
        Assert.Equal(28.2, again.Value, 8);

        transition.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task RunAsync_CancellationDoesNotSnapValue()
    {
        var clock = new ManualAnimationFrameClock();
        using var transition = new InfiniteTransition(clock);
        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        using var cts = new CancellationTokenSource();
        var task = transition.RunAsync(cts.Token);
        await WaitForPendingFrame(clock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => transition.RunAsync());
        clock.AdvanceBy(Ms(0));
        clock.AdvanceBy(Ms(250));
        Assert.Equal(25, value.Value, 8);
        Assert.True(transition.IsRunning);

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(transition.IsRunning);
        Assert.Equal(25, value.Value, 8);
        Assert.Equal(TimeSpan.Zero, transition.PlayTime);
    }

    [Fact]
    public async Task RunAsync_ResumeAfterCancelRestartsTimeline()
    {
        var clock = new ManualAnimationFrameClock();
        using var transition = new InfiniteTransition(clock);
        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        using var firstCancellation = new CancellationTokenSource();
        var first = transition.RunAsync(firstCancellation.Token);
        await WaitForPendingFrame(clock);
        clock.AdvanceBy(Ms(0));
        clock.AdvanceBy(Ms(250));
        firstCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        clock.SendFrame(Ms(1000));

        using var secondCancellation = new CancellationTokenSource();
        var second = transition.RunAsync(secondCancellation.Token);
        await WaitForPendingFrame(clock);
        clock.AdvanceBy(Ms(0));
        Assert.Equal(0, value.Value, 8);
        clock.AdvanceBy(Ms(100));
        Assert.Equal(10, value.Value, 8);
        secondCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
    }

    [Fact]
    public async Task NotificationException_FaultsLoopAndClearsRunning()
    {
        var clock = new ManualAnimationFrameClock();
        using var transition = new InfiniteTransition(clock);
        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        var error = new InvalidOperationException("observer failed");
        value.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(value.Value) && value.Value != 0) throw error;
        };
        var task = transition.RunAsync();
        await WaitForPendingFrame(clock);
        clock.AdvanceBy(Ms(0));
        clock.AdvanceBy(Ms(50));
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
        Assert.False(transition.IsRunning);
    }

    [Fact]
    public async Task Dispose_IsIdempotentAndDoesNotSnapValue()
    {
        var clock = new ManualAnimationFrameClock();
        var transition = new InfiniteTransition(clock);
        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(250));
        transition.Dispose();
        transition.Dispose();
        value.Dispose();
        Assert.Equal(25, value.Value, 8);
        Assert.False(transition.IsRunning);
        Assert.Empty(transition.Animations);
        Assert.Throws<ObjectDisposedException>(() => transition.AnimateDouble(0, 1, Repeat(Tween<double>(1000))));
        Assert.Throws<ObjectDisposedException>(() => value.Update(0, 1));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => transition.RunAsync());
    }

    [Fact]
    public void BackwardsClock_Throws()
    {
        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        transition.OnFrame(Ms(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => transition.OnFrame(Ms(50)));
    }

    [Fact]
    public void TypedHelpers_AnimateFloatAndInt()
    {
        using var transition = new InfiniteTransition(new ManualAnimationFrameClock());
        var f = transition.AnimateFloat(0f, 10f, Repeat(Tween<float>(100)));
        var i = transition.AnimateInt(0, 10, Repeat(Tween<int>(100)));
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(50));
        Assert.Equal(5f, f.Value, 5);
        Assert.Equal(5, i.Value);
    }
}
