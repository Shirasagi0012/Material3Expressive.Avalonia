using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class InfiniteTransitionTest
{
    private static Task OnUi(Func<Task> action) => HeadlessApp.Session.Dispatch(action, CancellationToken.None).Unwrap();
    private static TimeSpan Ms(double value) => TimeSpan.FromMilliseconds(value);

    private static TweenSpec<T> Tween<T>(double duration = 1000)
        => new(Ms(duration), easing: new LinearEasing());

    private static InfiniteRepeatableSpec<T> Repeat<T>(
        IDurationBasedAnimationSpec<T> animation,
        RepeatMode mode = RepeatMode.Restart,
        StartOffset offset = default)
        => new(animation, mode, offset);

    private static async Task WaitForPendingFrame(ManualFrameSource clock)
    {
        // Headless dispatcher timers do not advance on their own: drain the queue instead.
        for (var i = 0; i < 100 && clock.PendingFrameCount == 0; i++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
        }
        Assert.True(clock.PendingFrameCount > 0);
    }

    private static async Task WaitForNoPendingFrame(ManualFrameSource clock)
    {
        // Headless dispatcher timers do not advance on their own: drain the queue instead.
        for (var i = 0; i < 100 && clock.PendingFrameCount > 0; i++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
        }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [Fact]
    public void Restart_SamplesAgainstTheSameInfiniteSpec()
    {
        var spec = Repeat(Tween<double>(1000));
        var expected = new TargetBasedAnimation<double>(spec, new DoubleConverter(), 0, 100);
        using var transition = new InfiniteTransition();
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

        using var transition = new InfiniteTransition();
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
        using var transition = new InfiniteTransition();
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
        using var transition = new InfiniteTransition();
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
        using var transition = new InfiniteTransition();
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

        using var transition = new InfiniteTransition();
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
        using var transition = new InfiniteTransition();
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
        using var transition = new InfiniteTransition();
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
    public Task DurationScaleZero_RunAsyncStopsRequestingFrames() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var transition = new InfiniteTransition();
        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        var task = transition.RunAsync(window.AnimationRoot);
        await WaitForPendingFrame(clock);
        clock.PumpTo(Ms(0));
        clock.PumpTo(Ms(200));
        Assert.Equal(20, value.Value, 8);
        Assert.Equal(1, clock.PendingFrameCount);

        transition.DurationScale = 0;
        clock.PumpTo(Ms(216));
        Assert.Equal(100, value.Value);
        Assert.False(transition.IsRunning);
        await WaitForNoPendingFrame(clock);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.False(task.IsCompleted);

        transition.DurationScale = 1;
        await WaitForPendingFrame(clock);
        clock.PumpTo(Ms(232));
        Assert.Equal(0, value.Value, 8);
        clock.PumpTo(Ms(332));
        Assert.Equal(10, value.Value, 8);

        transition.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        window.Close();
    });

    [Fact]
    public void RemovingLastChild_LeavesTransitionIdle()
    {
        var spec = Repeat(Tween<double>(1000));
        using var transition = new InfiniteTransition();
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
    public Task RunAsync_IdleLoopWakesWhenChildIsAdded() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var transition = new InfiniteTransition();
        var task = transition.RunAsync(window.AnimationRoot);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.False(transition.IsRunning);

        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        await WaitForPendingFrame(clock);
        clock.PumpTo(Ms(0));
        clock.PumpTo(Ms(250));
        Assert.Equal(25, value.Value, 8);
        Assert.True(transition.IsRunning);

        value.Dispose();
        clock.PumpTo(Ms(266));
        await WaitForNoPendingFrame(clock);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.False(transition.IsRunning);
        Assert.False(task.IsCompleted);

        var again = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        Assert.Equal(0, again.Value);
        await WaitForPendingFrame(clock);
        clock.PumpTo(Ms(282));
        // RunAsync is still the same loop, so the parent timeline was not reset.
        Assert.Equal(28.2, again.Value, 8);

        transition.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        window.Close();
    });

    [Fact]
    public Task RunAsync_CancellationDoesNotSnapValue() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var transition = new InfiniteTransition();
        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        var first = transition.RunAsync(window.AnimationRoot);
        await WaitForPendingFrame(clock);
        // A second run takes over: the first is cancelled before the second samples.
        using var cts = new CancellationTokenSource();
        var task = transition.RunAsync(window.AnimationRoot, cts.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Dispatcher.UIThread.RunJobs();
        clock.Pump(Ms(0));
        clock.Pump(Ms(250));
        Assert.Equal(25, value.Value, 8);
        Assert.True(transition.IsRunning);

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(transition.IsRunning);
        Assert.Equal(25, value.Value, 8);
        Assert.Equal(TimeSpan.Zero, transition.PlayTime);
        window.Close();
    });

    [Fact]
    public Task RunAsync_ResumeAfterCancelRestartsTimeline() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var transition = new InfiniteTransition();
        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        using var firstCancellation = new CancellationTokenSource();
        var first = transition.RunAsync(window.AnimationRoot, firstCancellation.Token);
        await WaitForPendingFrame(clock);
        clock.Pump(Ms(0));
        clock.Pump(Ms(250));
        firstCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        clock.PumpTo(Ms(1000));

        using var secondCancellation = new CancellationTokenSource();
        var second = transition.RunAsync(window.AnimationRoot, secondCancellation.Token);
        await WaitForPendingFrame(clock);
        clock.Pump(Ms(0));
        Assert.Equal(0, value.Value, 8);
        clock.Pump(Ms(100));
        Assert.Equal(10, value.Value, 8);
        secondCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        window.Close();
    });

    [Fact]
    public Task NotificationException_FaultsLoopAndClearsRunning() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        using var transition = new InfiniteTransition();
        var value = transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        var error = new InvalidOperationException("observer failed");
        value.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(value.Value) && value.Value != 0) throw error;
        };
        var task = transition.RunAsync(window.AnimationRoot);
        await WaitForPendingFrame(clock);
        clock.Pump(Ms(0));
        clock.Pump(Ms(50));
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
        Assert.False(transition.IsRunning);
        window.Close();
    });

    [Fact]
    public Task Dispose_IsIdempotentAndDoesNotSnapValue() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var transition = new InfiniteTransition();
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
        await Assert.ThrowsAsync<ObjectDisposedException>(() => transition.RunAsync(window.AnimationRoot));
        window.Close();
    });

    [Fact]
    public void BackwardsClock_Throws()
    {
        using var transition = new InfiniteTransition();
        transition.AnimateDouble(0, 100, Repeat(Tween<double>(1000)));
        transition.OnFrame(Ms(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => transition.OnFrame(Ms(50)));
    }

    [Fact]
    public void TypedHelpers_AnimateFloatAndInt()
    {
        using var transition = new InfiniteTransition();
        var f = transition.AnimateFloat(0f, 10f, Repeat(Tween<float>(100)));
        var i = transition.AnimateInt(0, 10, Repeat(Tween<int>(100)));
        transition.OnFrame(Ms(0));
        transition.OnFrame(Ms(50));
        Assert.Equal(5f, f.Value, 5);
        Assert.Equal(5, i.Value);
    }
}
