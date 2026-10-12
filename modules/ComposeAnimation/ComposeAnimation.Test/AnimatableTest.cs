using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class AnimatableTest
{

    [Fact]
    public Task CompletedAnimation_AfterIdle_StartsOnFreshFrame() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();
        await DriveToEnd(clock, animatable.AnimateToAsync(scope, 100, Tween(TimeSpan.FromMilliseconds(100))));
        clock.Pump(TimeSpan.FromSeconds(10));

        var second = animatable.AnimateToAsync(scope, 200, Tween(TimeSpan.FromMilliseconds(100)));
        clock.Pump(Frame);
        Assert.Equal(100, animatable.Value);
        Assert.False(second.IsCompleted);
        clock.Pump(TimeSpan.FromMilliseconds(50));
        Assert.Equal(150, animatable.Value, 6);
        await DriveToEnd(clock, second);
        window.Close();
    });

    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(16);

    private static Animatable<double> CreateDouble(double initial = 0)
        => new(new DoubleConverter(), initial);

    private static TweenSpec<double> Tween(TimeSpan duration)
        => new(duration: duration, easing: new LinearEasing());

    private static async Task<AnimationResult<double>> DriveToEnd(
        ManualFrameSource clock,
        Task<AnimationResult<double>> task,
        int maxFrames = 1000)
    {
        for (var i = 0; i < maxFrames && !task.IsCompleted; i++)
            clock.Pump(Frame);
        return await task;
    }

    [Fact]
    public Task AnimateTo_CompletesWithFinishedAndReachesTarget() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();
        var values = new List<double>();

        var task = animatable.AnimateToAsync(
            scope, 100, Tween(TimeSpan.FromMilliseconds(64)), block: a => values.Add(a.Value));

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
        window.Close();
    });

    [Fact]
    public Task AnimateTo_Interrupted_RestartsFromCurrentValueOnOldTimebase() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();

        var first = animatable.AnimateToAsync(scope, 100, Tween(TimeSpan.FromMilliseconds(100)));
        clock.Pump(Frame);
        clock.Pump(Frame);
        var valueAtInterruption = animatable.Value;
        Assert.True(valueAtInterruption is > 0 and < 100);

        var frames = 0;
        var second = animatable.AnimateToAsync(scope, 200, Tween(TimeSpan.FromMilliseconds(100)),
            block: _ => frames++);

        // The interrupting animation re-anchors onto the interrupted one's last frame time, so
        // its first frame is processed synchronously, without advancing the clock.
        Assert.Equal(1, frames);
        Assert.Equal(valueAtInterruption, animatable.Value);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);

        var result = await DriveToEnd(clock, second);
        Assert.Equal(AnimationEndReason.Finished, result.EndReason);
        Assert.Equal(200, animatable.Value);
        window.Close();
    });

    [Fact]
    public Task AnimateTo_WithLowerPriority_IsRejected() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();

        var running = animatable.AnimateToAsync(scope, 100, Tween(TimeSpan.FromMilliseconds(100)),
            priority: MutatePriority.PreventUserInput);
        var rejected = animatable.AnimateToAsync(scope, 200, Tween(TimeSpan.FromMilliseconds(100)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rejected);
        Assert.Equal(100, animatable.TargetValue);

        var result = await DriveToEnd(clock, running);
        Assert.Equal(AnimationEndReason.Finished, result.EndReason);
        Assert.Equal(100, animatable.Value);
        window.Close();
    });

    [Fact]
    public Task AnimateDecay_RunsUntilItSettles() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();

        var task = animatable.AnimateDecayAsync(scope, 500, DecayAnimationSpec.ExponentialDecay<double>());
        var result = await DriveToEnd(clock, task);

        Assert.Equal(AnimationEndReason.Finished, result.EndReason);
        Assert.True(animatable.Value > 0);
        Assert.False(animatable.IsRunning);
        window.Close();
    });

    [Fact]
    public Task UpdateBounds_WhileRunning_AppliesFromNextFrame() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();

        var task = animatable.AnimateToAsync(scope, 100, Tween(TimeSpan.FromMilliseconds(160)));
        animatable.UpdateBounds(new()
        {
            Upper = 50.0,
        });

        var result = await DriveToEnd(clock, task);

        Assert.Equal(AnimationEndReason.BoundReached, result.EndReason);
        Assert.Equal(50, animatable.Value);
        Assert.Equal(50, result.EndState.Value);
        Assert.Equal(50.0, animatable.UpperBound);
        window.Close();
    });

    [Fact]
    public Task UpdateBounds_WhenIdle_ClampsImmediately() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var animatable = CreateDouble(initial: 80);

        animatable.UpdateBounds(new()
        {
            Upper = 50.0
        });

        Assert.Equal(50, animatable.Value);
        window.Close();
    });

    [Fact]
    public Task UpdateBounds_LowerAboveUpper_Throws() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var animatable = CreateDouble();

        Assert.Throws<ArgumentException>(() => animatable.UpdateBounds(new(60,50)));
        window.Close();
    });

    [Fact]
    public Task SnapTo_CancelsAnimationAndSetsValue() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();

        var task = animatable.AnimateToAsync(scope, 100, Tween(TimeSpan.FromMilliseconds(160)));
        clock.Pump(Frame);

        await animatable.SnapToAsync(30);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(30, animatable.Value);
        Assert.Equal(30, animatable.TargetValue);
        Assert.False(animatable.IsRunning);
        Assert.Equal(0, animatable.Velocity);
        window.Close();
    });

    [Fact]
    public Task SnapTo_ClampsToBounds() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var animatable = CreateDouble();
        animatable.UpdateBounds(new(0.0, 50.0));

        await animatable.SnapToAsync(80);

        Assert.Equal(50, animatable.Value);
        window.Close();
    });

    [Fact]
    public Task Stop_CancelsAnimationInPlace() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();

        var task = animatable.AnimateToAsync(scope, 100, Tween(TimeSpan.FromMilliseconds(100)));
        clock.Pump(Frame);
        clock.Pump(Frame);

        animatable.Stop();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.False(animatable.IsRunning);
        Assert.True(animatable.Value is > 0 and < 100); // stopped in its tracks
        window.Close();
    });

    [Fact]
    public Task StopAsync_WaitsForUnwind() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();

        var task = animatable.AnimateToAsync(scope, 100, Tween(TimeSpan.FromMilliseconds(100)));
        clock.Pump(Frame);

        await animatable.StopAsync();

        Assert.True(task.IsCanceled);
        Assert.False(animatable.IsRunning);
        window.Close();
    });

    [Fact]
    public Task AnimateTo_RaisesPropertyChanged() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();
        var raised = new List<string?>();
        animatable.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        var task = animatable.AnimateToAsync(scope, 100, Tween(TimeSpan.FromMilliseconds(64)));

        Assert.Contains(nameof(Animatable<double>.IsRunning), raised);
        Assert.Contains(nameof(Animatable<double>.TargetValue), raised);

        clock.Pump(Frame);
        Assert.Contains(nameof(Animatable<double>.Value), raised);

        await DriveToEnd(clock, task);
        window.Close();
    });

    [Fact]
    public Task SnapTo_SameValueStillNotifies() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var animatable = CreateDouble();
        var valueNotifications = 0;
        animatable.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Animatable<double>.Value)) valueNotifications++;
        };

        await animatable.SnapToAsync(0);

        Assert.Equal(1, valueNotifications);
        Assert.Equal(0, animatable.Value);
        window.Close();
    });

    [Fact]
    public Task AnimateTo_NotifiesValueOnHoldFrames() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = CreateDouble();
        var valueNotifications = 0;
        animatable.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Animatable<double>.Value)) valueNotifications++;
        };

        var task = animatable.AnimateToAsync(scope, 100,
            new TweenSpec<double>(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50),
                new LinearEasing()));
        clock.Pump(Frame);
        clock.Pump(Frame);
        Assert.Equal(0, animatable.Value);
        Assert.Equal(2, valueNotifications);

        await animatable.StopAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        window.Close();
    });

    [Fact]
    public Task AnimateTo_NotifiesVelocityWhenConvertedValueHolds() => OnUi(async () =>
    {
        var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot;
        var animatable = new Animatable<int>(new Int32Converter(), 0);
        var velocityNotifications = 0;
        animatable.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Animatable<int>.Velocity)) velocityNotifications++;
        };

        var task = animatable.AnimateToAsync(scope, 10,
            new TweenSpec<int>(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50),
                new LinearEasing()));
        clock.Pump(Frame);
        clock.Pump(Frame);
        Assert.Equal(0, animatable.Value);
        Assert.Equal(0, animatable.Velocity);
        Assert.Equal(2, velocityNotifications);

        await animatable.StopAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        window.Close();
    });
}
