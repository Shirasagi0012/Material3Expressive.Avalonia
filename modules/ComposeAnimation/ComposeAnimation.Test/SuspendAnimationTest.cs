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
            clock.Pump(Frame);
    }

    [Fact]
    public Task OneShotAnimate_ReportsFramesAndCompletes() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var samples = new List<double>();

        var task = SuspendAnimation.AnimateAsync(scope, 0, 100,
            animationSpec: Tween(TimeSpan.FromMilliseconds(64)),
            block: (value, _) => samples.Add(value));
        Drive(task, clock);
        await task;

        Assert.NotEmpty(samples);
        Assert.Equal(0, samples[0]); // first frame is sampled at play time 0
        Assert.Equal(100, samples[^1]);
        window.Close();
    });

    [Fact]
    public Task OneShotAnimate_GenericConverter_Works() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var samples = new List<double>();

        var task = SuspendAnimation.AnimateAsync(scope, new DoubleConverter(), 0.0, 10.0, 0.0,
            Tween(TimeSpan.FromMilliseconds(32)), (value, _) => samples.Add(value));
        Drive(task, clock);
        await task;

        Assert.Equal(10, samples[^1]);
        window.Close();
    });

    [Fact]
    public Task OneShotAnimate_CancellableByCallerToken() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        using var cts = new CancellationTokenSource();

        var task = SuspendAnimation.AnimateAsync(scope, 0, 100,
            animationSpec: Tween(TimeSpan.FromMilliseconds(160)),
            cancellationToken: cts.Token);
        clock.Pump(Frame);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        window.Close();
    });

    [Fact]
    public Task OneShotAnimateDecay_ReportsDecayingVelocity() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var samples = new List<(double Value, double Velocity)>();

        var task = SuspendAnimation.AnimateDecayAsync(scope, 0, 500, DecayAnimationSpec.ExponentialDecay<double>(),
            block: (value, velocity) => samples.Add((value, velocity)));
        Drive(task, clock);
        await task;

        Assert.NotEmpty(samples);
        Assert.Equal(500, samples[0].Velocity, 3);
        Assert.True(samples[^1].Value > 0);
        Assert.True(samples[^1].Velocity < samples[0].Velocity);
        window.Close();
    });

    [Fact]
    public Task AnimationStateAnimateTo_UpdatesState() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var state = new AnimationState<double>(new DoubleConverter(), 0);

        var task = SuspendAnimation.AnimateToAsync(scope, state, 100, Tween(TimeSpan.FromMilliseconds(64)));

        // The state only starts reporting IsRunning once the first frame is processed (matches
        // upstream, where the scope is created in the first frame callback).
        clock.Pump(Frame);
        Assert.True(state.IsRunning);

        Drive(task, clock);
        await task;

        Assert.Equal(100, state.Value);
        Assert.False(state.IsRunning);
        Assert.True(state.IsFinished);
        window.Close();
    });

    [Fact]
    public Task AnimationStateSequentialAnimation_StartsImmediatelyOnOldTimebase() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var state = new AnimationState<double>(new DoubleConverter(), 0);

        var first = SuspendAnimation.AnimateToAsync(scope, state, 100, Tween(TimeSpan.FromMilliseconds(32)));
        Drive(first, clock);
        await first;
        Assert.True(state.IsFinished);

        // sequentialAnimation uses the state's last frame time as the start time, so the first
        // frame is processed synchronously, without advancing the clock.
        var frames = 0;
        var second = SuspendAnimation.AnimateToAsync(scope, state, 200, Tween(TimeSpan.FromMilliseconds(64)),
            sequentialAnimation: true, block: _ => frames++);
        Assert.Equal(1, frames);

        Drive(second, clock);
        await second;
        Assert.Equal(200, state.Value);
        window.Close();
    });

    [Fact]
    public Task AnimationStateNonSequential_WaitsForNextFrame() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var state = new AnimationState<double>(new DoubleConverter(), 0);

        var first = SuspendAnimation.AnimateToAsync(scope, state, 100, Tween(TimeSpan.FromMilliseconds(32)));
        Drive(first, clock);
        await first;

        var frames = 0;
        var second = SuspendAnimation.AnimateToAsync(scope, state, 200, Tween(TimeSpan.FromMilliseconds(64)),
            block: _ => frames++);
        Assert.Equal(0, frames); // no start time supplied: waits for the next frame

        Drive(second, clock);
        await second;
        Assert.Equal(200, state.Value);
        window.Close();
    });

    [Fact]
    public Task AnimationStateAnimateDecay_SettlesAndStops() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var state = new AnimationState<double>(new DoubleConverter(), 0,
            AnimationVector.From(new[] { 500.0 }));

        var task = SuspendAnimation.AnimateDecayAsync(scope, state, DecayAnimationSpec.ExponentialDecay<double>());
        clock.Pump(Frame); // IsRunning starts reporting once the first frame is processed
        Assert.True(state.IsRunning);

        Drive(task, clock);
        await task;

        Assert.True(state.Value > 0);
        Assert.True(Math.Abs(state.Velocity) < 1);
        Assert.False(state.IsRunning);
        window.Close();
    });

    [Fact]
    public Task MotionDurationScale_ZeroSnapsToEnd() => OnUi(async () =>
    {
        // The root scope reads the ambient scale when it is created (D21).
        MotionDurationScale.ScaleFactor = 0;
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        try
        {
            var samples = new List<double>();
            var task = SuspendAnimation.AnimateAsync(scope, 0, 100,
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
        window.Close();
    });

    [Fact]
    public Task MotionDurationScale_TwoSlowsPlayTime() => OnUi(async () =>
    {
        // The root scope reads the ambient scale when it is created (D21).
        MotionDurationScale.ScaleFactor = 2;
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        try
        {
            double? mid = null;
            var task = SuspendAnimation.AnimateAsync(scope, 0, 100,
                animationSpec: Tween(TimeSpan.FromMilliseconds(64)),
                block: (value, _) =>
                {
                    // Start frame at t=0 (play 0); next frame at t=32ms wall / scale 2 => 16ms play => 25
                    if (clock.Now == TimeSpan.FromMilliseconds(32))
                        mid = value;
                });
            clock.PumpTo(TimeSpan.Zero); // first frame: startTime = 0
            clock.PumpTo(TimeSpan.FromMilliseconds(32));
            for (var i = 0; i < 20 && !task.IsCompleted; i++)
                clock.Pump(Frame);
            await task;

            Assert.NotNull(mid);
            Assert.Equal(25, mid!.Value, 1);
        }
        finally
        {
            MotionDurationScale.Reset();
        }
        window.Close();
    });
}
