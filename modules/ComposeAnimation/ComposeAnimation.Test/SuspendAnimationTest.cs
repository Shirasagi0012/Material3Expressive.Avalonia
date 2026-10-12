using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SuspendAnimationTest
{
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

    private static TweenSpec<double> Tween(TimeSpan duration)
        => new(duration: duration, easing: new LinearEasing());

    private static void Drive(Task task, ManualFrameSource clock, int maxFrames = 1000)
    {
        for (var i = 0; i < maxFrames && !task.IsCompleted; i++)
            clock.AdvanceBy(Frame);
    }

    [Fact]
    public async Task OneShotAnimate_ReportsFramesAndCompletes()
    {
        var clock = new ManualFrameSource();
        var samples = new List<double>();

        var task = clock.AnimateAsync(0, 100,
            animationSpec: Tween(TimeSpan.FromMilliseconds(64)),
            block: (value, _) => samples.Add(value));
        Drive(task, clock);
        await task;

        Assert.NotEmpty(samples);
        Assert.Equal(0, samples[0]); // first frame is sampled at play time 0
        Assert.Equal(100, samples[^1]);
    }

    [Fact]
    public async Task OneShotAnimate_GenericConverter_Works()
    {
        var clock = new ManualFrameSource();
        var samples = new List<double>();

        var task = clock.AnimateAsync(new DoubleConverter(), 0.0, 10.0, 0.0,
            Tween(TimeSpan.FromMilliseconds(32)), (value, _) => samples.Add(value));
        Drive(task, clock);
        await task;

        Assert.Equal(10, samples[^1]);
    }

    [Fact]
    public async Task OneShotAnimate_CancellableByCallerToken()
    {
        var clock = new ManualFrameSource();
        using var cts = new CancellationTokenSource();

        var task = clock.AnimateAsync(0, 100,
            animationSpec: Tween(TimeSpan.FromMilliseconds(160)),
            cancellationToken: cts.Token);
        clock.AdvanceBy(Frame);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task OneShotAnimateDecay_ReportsDecayingVelocity()
    {
        var clock = new ManualFrameSource();
        var samples = new List<(double Value, double Velocity)>();

        var task = clock.AnimateDecayAsync(0, 500, DecayAnimationSpec.ExponentialDecay<double>(),
            block: (value, velocity) => samples.Add((value, velocity)));
        Drive(task, clock);
        await task;

        Assert.NotEmpty(samples);
        Assert.Equal(500, samples[0].Velocity, 3);
        Assert.True(samples[^1].Value > 0);
        Assert.True(samples[^1].Velocity < samples[0].Velocity);
    }

    [Fact]
    public async Task AnimationStateAnimateTo_UpdatesState()
    {
        var clock = new ManualFrameSource();
        var state = new AnimationState<double>(new DoubleConverter(), 0);

        var task = state.AnimateToAsync(100, clock, Tween(TimeSpan.FromMilliseconds(64)));

        // The state only starts reporting IsRunning once the first frame is processed (matches
        // upstream, where the scope is created in the first frame callback).
        clock.AdvanceBy(Frame);
        Assert.True(state.IsRunning);

        Drive(task, clock);
        await task;

        Assert.Equal(100, state.Value);
        Assert.False(state.IsRunning);
        Assert.True(state.IsFinished);
    }

    [Fact]
    public async Task AnimationStateSequentialAnimation_StartsImmediatelyOnOldTimebase()
    {
        var clock = new ManualFrameSource();
        var state = new AnimationState<double>(new DoubleConverter(), 0);

        var first = state.AnimateToAsync(100, clock, Tween(TimeSpan.FromMilliseconds(32)));
        Drive(first, clock);
        await first;
        Assert.True(state.IsFinished);

        // sequentialAnimation uses the state's last frame time as the start time, so the first
        // frame is processed synchronously, without advancing the clock.
        var frames = 0;
        var second = state.AnimateToAsync(200, clock, Tween(TimeSpan.FromMilliseconds(64)),
            sequentialAnimation: true, block: _ => frames++);
        Assert.Equal(1, frames);

        Drive(second, clock);
        await second;
        Assert.Equal(200, state.Value);
    }

    [Fact]
    public async Task AnimationStateNonSequential_WaitsForNextFrame()
    {
        var clock = new ManualFrameSource();
        var state = new AnimationState<double>(new DoubleConverter(), 0);

        var first = state.AnimateToAsync(100, clock, Tween(TimeSpan.FromMilliseconds(32)));
        Drive(first, clock);
        await first;

        var frames = 0;
        var second = state.AnimateToAsync(200, clock, Tween(TimeSpan.FromMilliseconds(64)),
            block: _ => frames++);
        Assert.Equal(0, frames); // no start time supplied: waits for the next frame

        Drive(second, clock);
        await second;
        Assert.Equal(200, state.Value);
    }

    [Fact]
    public async Task AnimationStateAnimateDecay_SettlesAndStops()
    {
        var clock = new ManualFrameSource();
        var state = new AnimationState<double>(new DoubleConverter(), 0,
            AnimationVector.From(new[] { 500.0 }));

        var task = state.AnimateDecayAsync(DecayAnimationSpec.ExponentialDecay<double>(), clock);
        clock.AdvanceBy(Frame); // IsRunning starts reporting once the first frame is processed
        Assert.True(state.IsRunning);

        Drive(task, clock);
        await task;

        Assert.True(state.Value > 0);
        Assert.True(Math.Abs(state.Velocity) < 1);
        Assert.False(state.IsRunning);
    }
[Fact]
    public async Task MotionDurationScale_ZeroSnapsToEnd()
    {
        var clock = new ManualFrameSource();
        MotionDurationScale.ScaleFactor = 0;
        try
        {
            var samples = new List<double>();
            var task = clock.AnimateAsync(0, 100,
                animationSpec: Tween(TimeSpan.FromMilliseconds(1000)),
                block: (value, _) => samples.Add(value));
            Drive(task, clock, maxFrames: 5);
            await task;

            Assert.NotEmpty(samples);
            Assert.Equal(100, samples[^1]);
            Assert.True(samples.Count <= 2, "scale 0 should finish almost immediately");
        }
        finally
        {
            MotionDurationScale.Reset();
        }
    }

    [Fact]
    public async Task MotionDurationScale_TwoSlowsPlayTime()
    {
        var clock = new ManualFrameSource();
        MotionDurationScale.ScaleFactor = 2;
        try
        {
            double? mid = null;
            var task = clock.AnimateAsync(0, 100,
                animationSpec: Tween(TimeSpan.FromMilliseconds(64)),
                block: (value, _) =>
                {
                    // Start frame at t=0 (play 0); next frame at t=32ms wall / scale 2 => 16ms play => 25
                    if (clock.Now == TimeSpan.FromMilliseconds(32))
                        mid = value;
                });
            clock.SendFrame(TimeSpan.Zero); // first frame: startTime = 0
            clock.SendFrame(TimeSpan.FromMilliseconds(32));
            for (var i = 0; i < 20 && !task.IsCompleted; i++)
                clock.AdvanceBy(Frame);
            await task;

            Assert.NotNull(mid);
            Assert.Equal(25, mid!.Value, 1);
        }
        finally
        {
            MotionDurationScale.Reset();
        }
    }
}
