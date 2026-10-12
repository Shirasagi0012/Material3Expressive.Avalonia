using Avalonia;
using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class SharedBoundsRuntimeTest
{
    private static TimeSpan Ms(double value) => TimeSpan.FromMilliseconds(value);
    private static readonly Rect Source = new(45, 125, 220, 180);
    private static readonly Rect Target = new(520, 35, 440, 360);
    private static SpringSpec<Rect> Spring() => new(stiffness: 16,
        visibilityThreshold: new Rect(0.1, 0.1, 0, 0)); // 0.1 DIP per edge.

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContentFade_RetainsOutgoingAfterBoundsFinish_AndHonorsZeroDuration(bool zeroDuration)
    {
        using var root = new Transition<bool>(true);
        root.DurationScale = zeroDuration ? 0 : 1;
        using var lease = root.BeginPreparation();
        root.UpdateTarget(false);
        root.OnFrame(Ms(0));
        root.OnFrame(Ms(1000));
        using var deferred = root.CreateDeferredAnimation(new RectConverter());
        var bounds = deferred.Animate(_ => new TweenSpec<Rect>(Ms(40)), Source, null, _ => Target);
        var fade = root.AnimateDouble(s => s ? 0 : 1,
            _ => new TweenSpec<double>(Ms(200), easing: new LinearEasing()));
        lease.Dispose();
        root.OnFrame(Ms(1100));
        Assert.True(bounds.IsFinished);
        Assert.Equal(Target, bounds.Value);
        if (!zeroDuration)
        {
            Assert.Equal(0.5, fade.Value, 8);
            Assert.True(root.CurrentState);
            Assert.True(root.IsRunning);
            root.OnFrame(Ms(1200));
        }
        Assert.Equal(1, fade.Value);
        Assert.True(fade.IsFinished);
        Assert.False(root.CurrentState);
        Assert.False(root.IsRunning);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LateBounds_RepeatedLayoutRetarget_PreservesTrajectoryValueAndVelocity(bool startWithTween)
    {
        using var root = new Transition<bool>(true);
        var exiting = root.CreateChildTransition(s => s);
        using var lease = exiting.BeginPreparation();
        root.UpdateTarget(false);
        root.OnFrame(Ms(0));
        root.OnFrame(Ms(60000));
        using var deferred = exiting.CreateDeferredAnimation(new RectConverter());
        IFiniteAnimationSpec<Rect> spec = startWithTween
            ? new TweenSpec<Rect>(Ms(4000), easing: new LinearEasing()) : Spring();
        var bounds = deferred.Animate(_ => spec, Source, new AnimationVector(4), s => s ? Source : Target);
        lease.Dispose();
        Assert.False(root.IsPreparing);
        root.OnFrame(Ms(60120));
        Assert.True(root.CurrentState); // Actual animation now owns retention.
        Assert.NotEqual(Source, bounds.Value);

        var clockTime = 60120.0;
        foreach (var target in new[] { new Rect(430, 65, 330, 270), new Rect(80, 145, 242, 198), Target })
        {
            var before = bounds.Value;
            var velocity = bounds.VelocityVector.ToArray();
            Assert.Contains(velocity, v => Math.Abs(v) > 1);
            bounds.Update(_ => target);
            Assert.Equal(before, bounds.Value);
            Assert.Equal(velocity, bounds.VelocityVector.ToArray());
            AssertOriginAndDerivative(bounds, root.PlayTime, before, velocity);
            var trajectory = bounds.Animation;
            bounds.Update(_ => target); // Re-reading an unchanged layout must not restart it.
            Assert.Same(trajectory, bounds.Animation);
            clockTime += 40;
            root.OnFrame(Ms(clockTime));
        }

        root.OnFrame(bounds.Duration);
        Assert.Equal(Target, bounds.Value);
        Assert.True(bounds.IsFinished);
        Assert.False(root.CurrentState);
        Assert.False(root.IsRunning);
    }

    [Fact]
    public void VisibilityReversal_PreservesAllFourComponentsInsteadOfRestartingAtEndpoint()
    {
        using var root = new Transition<bool>(true);
        var bounds = root.AnimateRect(s => s ? Source : Target, _ => Spring());
        root.UpdateTarget(false);
        root.OnFrame(Ms(0));
        root.OnFrame(Ms(240));
        var before = bounds.Value;
        var velocity = bounds.VelocityVector.ToArray();
        root.UpdateTarget(true);
        AssertOriginAndDerivative(bounds, root.PlayTime, before, velocity);
        root.OnFrame(Ms(241));
        // Retargeting backwards does not erase forward momentum at the interruption instant.
        Assert.True(bounds.Value.X > before.X);
        root.OnFrame(bounds.Duration);
        Assert.Equal(Source, bounds.Value);
        Assert.True(root.CurrentState);
        Assert.False(root.IsRunning);
    }

    [Fact]
    public void ZeroDuration_CompletesLateBoundsOnNextFrameAfterPreparationTransfer()
    {
        using var root = new Transition<bool>(true) { DurationScale = 0 };
        using var lease = root.BeginPreparation();
        root.UpdateTarget(false);
        root.OnFrame(Ms(0));
        using var deferred = root.CreateDeferredAnimation(new RectConverter());
        var bounds = deferred.Animate(_ => Spring(), Source, null, _ => Target);
        lease.Dispose();
        Assert.Equal(Source, bounds.Value);
        root.OnFrame(Ms(16));
        Assert.Equal(Target, bounds.Value);
        Assert.True(bounds.IsFinished);
        Assert.False(root.IsRunning);
    }

    private static void AssertOriginAndDerivative(Transition<bool>.TransitionAnimationState<Rect> bounds,
        TimeSpan time, Rect before, double[] velocity)
    {
        var origin = bounds.Animation.GetValue(time);
        var actualVelocity = new double[4];
        bounds.Animation.GetVelocityVector(time, actualVelocity);
        var converter = new RectConverter();
        var initial = new double[4];
        var next = new double[4];
        converter.To(origin, initial);
        const double deltaSeconds = 0.000001;
        converter.To(bounds.Animation.GetValue(time + TimeSpan.FromSeconds(deltaSeconds)), next);
        Assert.Equal(before.X, origin.X, 8);
        Assert.Equal(before.Y, origin.Y, 8);
        Assert.Equal(before.Width, origin.Width, 8);
        Assert.Equal(before.Height, origin.Height, 8);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(velocity[i], actualVelocity[i], 7);
            Assert.InRange(Math.Abs((next[i] - initial[i]) / deltaSeconds - velocity[i]), 0, 0.5);
        }
    }
}
