using Avalonia;
using Avalonia.Headless.XUnit;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Xunit;
using MaterialCorners = Material3Expressive.Foundation.Interaction.AnimatedCornerRadius;

namespace Material3Expressive.Tests;

public class AnimationLifetimeTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposingAnEffect_PreservesItsParentAndSiblingAnimation(bool shape)
    {
        using var clock = new MotionTestClockScope();
        var parent = clock.Scope;
        var sibling = new Animatable<double>(new DoubleConverter(), 0d, VisibilityThresholds.Double);
        var baseline = parent.Diagnostics.ActiveChildCount;
        IDisposable effect = shape
            ? new MaterialCorners(new CornerRadius(20), MotionScheme.Expressive.DefaultEffects, parent)
            : new RippleAnimation(new Point(), new Size(80, 40), parent);
        Assert.Equal(baseline + 1, parent.Diagnostics.ActiveChildCount);
        effect.Dispose();
        Assert.False(parent.Diagnostics.IsEnding);
        Assert.Equal(baseline, parent.Diagnostics.ActiveChildCount);

        var playing = sibling.AnimateToAsync(parent, 1, new TweenSpec<double>(TimeSpan.FromMilliseconds(100)));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
        await playing;
        Assert.Equal(1, sibling.Value);
    }

    [AvaloniaFact]
    public async Task DisposingRippleWhileWaitingForRelease_CancelsTheWaitWithoutAnotherFrame()
    {
        using var clock = new MotionTestClockScope();
        var parent = clock.Scope;
        using var ripple = new RippleAnimation(new Point(), new Size(80, 40), parent);
        var playing = ripple.AnimateAsync();
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(225));
        Assert.False(playing.IsCompleted);
        Assert.Equal(0, clock.PendingFrameCount);

        ripple.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => playing);
        Assert.False(parent.Diagnostics.IsEnding);
        Assert.Equal(0, parent.Diagnostics.ActiveChildCount);
        Assert.Null(parent.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData(1000)] // reversal
    [InlineData(4)] // third destination
    public void RetargetingShape_PublishesOnlyConsistentEndpointAndProgressSamples(double destination)
    {
        using var clock = new MotionTestClockScope();
        var samples = new List<double>();
        MaterialCorners? shape = null;
        using var ownedShape = shape = new(new CornerRadius(1000), MotionScheme.Expressive.DefaultEffects,
            clock.Scope, () => samples.Add(shape!.Current.TopLeft)) { Size = new Size(40, 40) };
        Assert.Empty(samples); // no user callback while the constructor is still running
        clock.Track(shape.AnimateToAsync(new CornerRadius(8)));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        var before = shape.Current.TopLeft;
        samples.Clear();

        clock.Track(shape.AnimateToAsync(new CornerRadius(destination)));
        Assert.NotEmpty(samples);
        Assert.All(samples, sample => Assert.Equal(before, sample, 8));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(Math.Min(destination, 20), shape.Current.TopLeft, 6);
    }

    [AvaloniaFact]
    public void ShapeCallbackMayRetarget_WithoutTheOldFlowOverwritingTheNewDestination()
    {
        using var clock = new MotionTestClockScope();
        MaterialCorners? shape = null;
        var redirect = false;
        using var ownedShape = shape = new(new CornerRadius(20), MotionScheme.Expressive.DefaultEffects,
            clock.Scope, () =>
            {
                if (!redirect) return;
                redirect = false;
                clock.Track(shape!.AnimateToAsync(new CornerRadius(4)));
            });
        clock.Track(shape.AnimateToAsync(new CornerRadius(8)));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        redirect = true;
        clock.Track(shape.AnimateToAsync(new CornerRadius(20)));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(new CornerRadius(4), shape.Current);
        Assert.Equal(new CornerRadius(4), shape.Target);
    }

    [AvaloniaFact]
    public void TestClock_ReportsTrackedFailures_AndAllowsNormalCancellation()
    {
        using var clock = new MotionTestClockScope();
        clock.Track(Task.FromCanceled(new CancellationToken(true)));
        clock.AdvanceBy(TimeSpan.Zero);

        var failure = new InvalidOperationException("Animation failed");
        clock.Track(Task.FromException(failure));
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => clock.AdvanceBy(TimeSpan.Zero)));

        // Exception type alone does not make a task canceled.
        var faultedCancellation = new OperationCanceledException("Faulted task");
        clock.Track(Task.FromException(faultedCancellation));
        Assert.Same(faultedCancellation, Assert.Throws<OperationCanceledException>(() => clock.SendFrame(clock.Now)));
    }

    [AvaloniaFact]
    public void TestClock_ReportsFailuresThatArriveDuringCleanup()
    {
        var clock = new MotionTestClockScope();
        var completion = new TaskCompletionSource();
        clock.Track(completion.Task);
        var failure = new InvalidOperationException("Late failure");
        using var registration = clock.Scope.Lifetime.Register(() => completion.SetException(failure));
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(clock.Dispose));
        // Dispose still restored the ambient fixture state despite reporting a failure.
        using var next = new MotionTestClockScope();
    }
}
