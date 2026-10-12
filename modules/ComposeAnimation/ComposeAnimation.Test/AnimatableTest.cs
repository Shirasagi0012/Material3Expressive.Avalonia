using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimatableTest
{
    [Fact]
    public async Task CompletedAnimation_AfterIdle_StartsOnFreshFrame()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);
        await DriveToEnd(clock, animatable.AnimateToAsync(100, Tween(TimeSpan.FromMilliseconds(100))));
        clock.AdvanceBy(TimeSpan.FromSeconds(10));

        var second = animatable.AnimateToAsync(200, Tween(TimeSpan.FromMilliseconds(100)));
        clock.AdvanceBy(Frame);
        Assert.Equal(100, animatable.Value);
        Assert.False(second.IsCompleted);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50));
        Assert.Equal(150, animatable.Value, 6);
        await DriveToEnd(clock, second);
    }

    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

    private static Animatable<double> CreateDouble(ManualFrameSource clock, double initial = 0)
        => new(new DoubleConverter(), initial, clock);

    private static TweenSpec<double> Tween(TimeSpan duration)
        => new(duration: duration, easing: new LinearEasing());

    private static async Task<AnimationResult<double>> DriveToEnd(
        ManualFrameSource clock,
        Task<AnimationResult<double>> task,
        int maxFrames = 1000)
    {
        for (var i = 0; i < maxFrames && !task.IsCompleted; i++)
            clock.AdvanceBy(Frame);
        return await task;
    }

    [Fact]
    public async Task AnimateTo_CompletesWithFinishedAndReachesTarget()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);
        var values = new List<double>();

        var task = animatable.AnimateToAsync(
            100, Tween(TimeSpan.FromMilliseconds(64)), block: a => values.Add(a.Value));

        Assert.True(animatable.IsRunning);
        Assert.Equal(100, animatable.TargetValue);

        var result = await DriveToEnd(clock, task);

        Assert.Equal(AnimationEndReason.Finished, result.EndReason);
        Assert.Equal(100, animatable.Value);
        Assert.Equal(100, result.EndState.Value);
        Assert.False(animatable.IsRunning);
        Assert.Equal(0, animatable.Velocity);
        Assert.NotEmpty(values);
        Assert.Equal(0, values[0]); // first frame is sampled at play time 0
    }

    [Fact]
    public async Task AnimateTo_Interrupted_RestartsFromCurrentValueOnOldTimebase()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);

        var first = animatable.AnimateToAsync(100, Tween(TimeSpan.FromMilliseconds(100)));
        clock.AdvanceBy(Frame);
        clock.AdvanceBy(Frame);
        var valueAtInterruption = animatable.Value;
        Assert.True(valueAtInterruption is > 0 and < 100);

        var frames = 0;
        var second = animatable.AnimateToAsync(200, Tween(TimeSpan.FromMilliseconds(100)),
            block: _ => frames++);

        // The interrupting animation re-anchors onto the interrupted one's last frame time, so
        // its first frame is processed synchronously, without advancing the clock.
        Assert.Equal(1, frames);
        Assert.Equal(valueAtInterruption, animatable.Value);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);

        var result = await DriveToEnd(clock, second);
        Assert.Equal(AnimationEndReason.Finished, result.EndReason);
        Assert.Equal(200, animatable.Value);
    }

    [Fact]
    public async Task AnimateTo_WithLowerPriority_IsRejected()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);

        var running = animatable.AnimateToAsync(100, Tween(TimeSpan.FromMilliseconds(100)),
            priority: MutatePriority.PreventUserInput);
        var rejected = animatable.AnimateToAsync(200, Tween(TimeSpan.FromMilliseconds(100)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rejected);
        Assert.Equal(100, animatable.TargetValue);

        var result = await DriveToEnd(clock, running);
        Assert.Equal(AnimationEndReason.Finished, result.EndReason);
        Assert.Equal(100, animatable.Value);
    }

    [Fact]
    public async Task AnimateDecay_RunsUntilItSettles()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);

        var task = animatable.AnimateDecayAsync(500, DecayAnimationSpec.ExponentialDecay<double>());
        var result = await DriveToEnd(clock, task);

        Assert.Equal(AnimationEndReason.Finished, result.EndReason);
        Assert.True(animatable.Value > 0);
        Assert.False(animatable.IsRunning);
    }

    [Fact]
    public async Task UpdateBounds_WhileRunning_AppliesFromNextFrame()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);

        var task = animatable.AnimateToAsync(100, Tween(TimeSpan.FromMilliseconds(160)));
        animatable.UpdateBounds(new()
        {
            Upper = 50.0,
        });

        var result = await DriveToEnd(clock, task);

        Assert.Equal(AnimationEndReason.BoundReached, result.EndReason);
        Assert.Equal(50, animatable.Value);
        Assert.Equal(50, result.EndState.Value);
        Assert.Equal(50.0, animatable.UpperBound);
    }

    [Fact]
    public void UpdateBounds_WhenIdle_ClampsImmediately()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock, initial: 80);

        animatable.UpdateBounds(new()
        {
            Upper = 50.0
        });

        Assert.Equal(50, animatable.Value);
    }

    [Fact]
    public void UpdateBounds_LowerAboveUpper_Throws()
    {
        var animatable = CreateDouble(new ManualFrameSource());

        Assert.Throws<ArgumentException>(() => animatable.UpdateBounds(new(60,50)));
    }

    [Fact]
    public async Task SnapTo_CancelsAnimationAndSetsValue()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);

        var task = animatable.AnimateToAsync(100, Tween(TimeSpan.FromMilliseconds(160)));
        clock.AdvanceBy(Frame);

        await animatable.SnapToAsync(30);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(30, animatable.Value);
        Assert.Equal(30, animatable.TargetValue);
        Assert.False(animatable.IsRunning);
        Assert.Equal(0, animatable.Velocity);
    }

    [Fact]
    public async Task SnapTo_ClampsToBounds()
    {
        var animatable = CreateDouble(new ManualFrameSource());
        animatable.UpdateBounds(new(0.0, 50.0));

        await animatable.SnapToAsync(80);

        Assert.Equal(50, animatable.Value);
    }

    [Fact]
    public async Task Stop_CancelsAnimationInPlace()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);

        var task = animatable.AnimateToAsync(100, Tween(TimeSpan.FromMilliseconds(100)));
        clock.AdvanceBy(Frame);
        clock.AdvanceBy(Frame);

        animatable.Stop();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(animatable.IsRunning);
        Assert.True(animatable.Value is > 0 and < 100); // stopped in its tracks
    }

    [Fact]
    public async Task StopAsync_WaitsForUnwind()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);

        var task = animatable.AnimateToAsync(100, Tween(TimeSpan.FromMilliseconds(100)));
        clock.AdvanceBy(Frame);

        await animatable.StopAsync();

        Assert.True(task.IsCanceled);
        Assert.False(animatable.IsRunning);
    }

    [Fact]
    public async Task AnimateTo_RaisesPropertyChanged()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);
        var raised = new List<string?>();
        animatable.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        var task = animatable.AnimateToAsync(100, Tween(TimeSpan.FromMilliseconds(64)));

        Assert.Contains(nameof(Animatable<double>.IsRunning), raised);
        Assert.Contains(nameof(Animatable<double>.TargetValue), raised);

        clock.AdvanceBy(Frame);
        Assert.Contains(nameof(Animatable<double>.Value), raised);

        await DriveToEnd(clock, task);
    }

    [Fact]
    public async Task SnapTo_SameValueStillNotifies()
    {
        var animatable = CreateDouble(new ManualFrameSource());
        var valueNotifications = 0;
        animatable.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Animatable<double>.Value)) valueNotifications++;
        };

        await animatable.SnapToAsync(0);

        Assert.Equal(1, valueNotifications);
        Assert.Equal(0, animatable.Value);
    }

    [Fact]
    public async Task AnimateTo_NotifiesValueOnHoldFrames()
    {
        var clock = new ManualFrameSource();
        var animatable = CreateDouble(clock);
        var valueNotifications = 0;
        animatable.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Animatable<double>.Value)) valueNotifications++;
        };

        var task = animatable.AnimateToAsync(100,
            new TweenSpec<double>(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50),
                new LinearEasing()));
        clock.AdvanceBy(Frame);
        clock.AdvanceBy(Frame);
        Assert.Equal(0, animatable.Value);
        Assert.Equal(2, valueNotifications);

        await animatable.StopAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task AnimateTo_NotifiesVelocityWhenConvertedValueHolds()
    {
        var clock = new ManualFrameSource();
        var animatable = new Animatable<int>(new Int32Converter(), 0, clock);
        var velocityNotifications = 0;
        animatable.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Animatable<int>.Velocity)) velocityNotifications++;
        };

        var task = animatable.AnimateToAsync(10,
            new TweenSpec<int>(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50),
                new LinearEasing()));
        clock.AdvanceBy(Frame);
        clock.AdvanceBy(Frame);
        Assert.Equal(0, animatable.Value);
        Assert.Equal(0, animatable.Velocity);
        Assert.Equal(2, velocityNotifications);

        await animatable.StopAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }
}
