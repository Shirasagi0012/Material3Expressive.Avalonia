using System;
using Avalonia.Controls;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class SpringTests
{
    private static readonly TimeSpan Frame60 = TimeSpan.FromSeconds(1.0 / 60);
    private static readonly TimeSpan Frame144 = TimeSpan.FromSeconds(1.0 / 144);

    /// <summary>Runs an animation to completion, returning how many frames it took.</summary>
    private static int RunToRest(IAnimation animation, TimeSpan frame, int maxFrames = 6000)
    {
        var frames = 0;
        while (animation.IsRunning && frames < maxFrames)
        {
            animation.Tick(frame);
            frames++;
        }

        return frames;
    }

    [Fact]
    public void ASpring_SettlesExactlyOnItsTarget()
    {
        var spring = new SpringAnimation(0, MotionTokens.Expressive.DefaultSpatial);
        spring.AnimateTo(1);

        RunToRest(spring, Frame60);

        Assert.False(spring.IsRunning);
        Assert.Equal(1.0, spring.Value);
        Assert.Equal(0.0, spring.Velocity);
    }

    [Fact]
    public void ASpring_LandsInTheSamePlaceAtAnyFrameRate()
    {
        // The closed-form solution is evaluated from the start of the leg, not integrated frame to
        // frame, so sampling more often must not change the trajectory.
        var slow = new SpringAnimation(0, MotionTokens.Expressive.DefaultSpatial);
        var fast = new SpringAnimation(0, MotionTokens.Expressive.DefaultSpatial);
        slow.AnimateTo(1);
        fast.AnimateTo(1);

        // 20 frames at 60Hz and 48 at 144Hz cover the same third of a second.
        for (var i = 0; i < 20; i++)
            slow.Tick(Frame60);

        for (var i = 0; i < 48; i++)
            fast.Tick(Frame144);

        Assert.Equal(slow.Value, fast.Value, 6);
        Assert.Equal(slow.Velocity, fast.Velocity, 4);
    }

    [Fact]
    public void ADroppedFrame_DoesNotChangeWhereTheSpringIs()
    {
        var steady = new SpringAnimation(0, MotionTokens.Expressive.DefaultSpatial);
        var stuttering = new SpringAnimation(0, MotionTokens.Expressive.DefaultSpatial);
        steady.AnimateTo(1);
        stuttering.AnimateTo(1);

        for (var i = 0; i < 6; i++)
            steady.Tick(Frame60);

        stuttering.Tick(Frame60);
        stuttering.Tick(Frame60 * 4); // four frames' worth arriving at once
        stuttering.Tick(Frame60);

        Assert.Equal(steady.Value, stuttering.Value, 9);
    }

    [Fact]
    public void ARetarget_KeepsTheVelocityItAlreadyHad()
    {
        var spring = new SpringAnimation(0, MotionTokens.Expressive.DefaultSpatial);
        spring.AnimateTo(1);

        for (var i = 0; i < 5; i++)
            spring.Tick(Frame60);

        var velocity = spring.Velocity;
        Assert.True(velocity > 0, "the spring should be moving before it is interrupted");

        spring.AnimateTo(0);

        // Reversing mid-flight must not stall: the first frame after still moves the way it was.
        spring.Tick(TimeSpan.FromMilliseconds(1));
        Assert.True(spring.Velocity > 0, "a reversed spring keeps its momentum for a moment");
        Assert.True(spring.Value > 0);
    }

    [Fact]
    public void ARetargetToTheSamePlace_DoesNotRestartTheAnimation()
    {
        var spring = new SpringAnimation(0, MotionTokens.Expressive.DefaultSpatial);
        spring.AnimateTo(1);

        for (var i = 0; i < 5; i++)
            spring.Tick(Frame60);

        var value = spring.Value;
        spring.AnimateTo(1);

        Assert.Equal(value, spring.Value);
    }

    [Fact]
    public void ACriticallyDampedSpring_NeverOvershoots()
    {
        var spring = new SpringAnimation(0, MotionTokens.Expressive.DefaultEffects);
        spring.AnimateTo(1);

        while (spring.IsRunning)
        {
            spring.Tick(Frame60);
            Assert.True(spring.Value <= 1.0 + 1e-9, $"overshot to {spring.Value}");
        }
    }

    [Fact]
    public void AnUnderDampedSpring_Overshoots()
    {
        // Expressive's fast spatial spring is the bounciest thing in the scheme; bounce is the
        // point of it, so if this stops overshooting the port has gone wrong.
        var spring = new SpringAnimation(0, MotionTokens.Expressive.FastSpatial);
        spring.AnimateTo(1);

        var peak = 0.0;
        while (spring.IsRunning)
        {
            spring.Tick(Frame60);
            peak = Math.Max(peak, spring.Value);
        }

        Assert.True(peak > 1.0, $"expected an overshoot, peaked at {peak}");
    }

    [Fact]
    public void AStifferSpring_SettlesSooner()
    {
        var fast = new SpringAnimation(0, MotionTokens.Expressive.FastEffects);
        var slow = new SpringAnimation(0, MotionTokens.Expressive.SlowEffects);
        fast.AnimateTo(1);
        slow.AnimateTo(1);

        Assert.True(fast.DurationSeconds < slow.DurationSeconds);
        Assert.True(RunToRest(fast, Frame60) < RunToRest(slow, Frame60));
    }

    [Theory]
    [InlineData(0.5)]  // under-damped
    [InlineData(1.0)]  // critically damped
    [InlineData(1.8)]  // over-damped
    public void EveryDampingRegime_ProducesAFiniteDuration(double dampingRatio)
    {
        var spring = new SpringAnimation(0, new SpringSpec(dampingRatio, 400));
        spring.AnimateTo(1);

        Assert.True(spring.DurationSeconds > 0);
        Assert.True(double.IsFinite(spring.DurationSeconds));
        Assert.True(spring.DurationSeconds < 30, $"{dampingRatio} took {spring.DurationSeconds}s");
    }

    [Fact]
    public void AnUndampedSpring_SnapsRatherThanOscillatingForever()
    {
        // Nothing in Material is undamped, but a hand-authored MotionScheme could be, and an
        // animation that never ends would pin the render loop.
        var spring = new SpringAnimation(0, new SpringSpec(0, 400));
        spring.AnimateTo(1);

        Assert.False(spring.IsRunning);
        Assert.Equal(1.0, spring.Value);
    }

    [Fact]
    public void SnapTo_CancelsMotion()
    {
        var spring = new SpringAnimation(0, MotionTokens.Expressive.DefaultSpatial);
        spring.AnimateTo(1);
        spring.Tick(Frame60);
        spring.SnapTo(0.25);

        Assert.False(spring.IsRunning);
        Assert.Equal(0.25, spring.Value);
        Assert.Equal(0.0, spring.Velocity);
    }
}

public class TweenTests
{
    [Fact]
    public void ALinearTween_IsHalfwayAtHalfTime()
    {
        var tween = new TweenAnimation(0, TimeSpan.FromMilliseconds(100), Easings.Linear);
        tween.AnimateTo(1);
        tween.Tick(TimeSpan.FromMilliseconds(50));

        Assert.Equal(0.5, tween.Value, 6);
    }

    [Fact]
    public void ATween_EndsOnItsTarget()
    {
        var tween = new TweenAnimation(0, TimeSpan.FromMilliseconds(100));
        tween.AnimateTo(1);
        tween.Tick(TimeSpan.FromMilliseconds(150));

        Assert.False(tween.IsRunning);
        Assert.Equal(1.0, tween.Value);
    }

    [Fact]
    public void AZeroLengthTween_Snaps()
    {
        var tween = new TweenAnimation(0, TimeSpan.Zero);
        tween.AnimateTo(1);

        Assert.False(tween.IsRunning);
        Assert.Equal(1.0, tween.Value);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 1.0)]
    public void FastOutSlowIn_PinsItsEndpoints(double fraction, double expected)
    {
        Assert.Equal(expected, Easings.FastOutSlowIn(fraction), 6);
    }

    [Fact]
    public void FastOutSlowIn_AcceleratesThenDecelerates()
    {
        // cubic-bezier(0.4, 0, 0.2, 1) sits below the diagonal while it picks up speed and above it
        // while it settles — the fast part is the middle, not the start.
        Assert.True(Easings.FastOutSlowIn(0.25) < 0.25);
        Assert.True(Easings.FastOutSlowIn(0.75) > 0.75);
    }

    [Fact]
    public void LinearOutSlowIn_LeadsTheDiagonalThroughout()
    {
        // The entering curve has no acceleration phase, so it is ahead of linear from the start.
        Assert.True(Easings.LinearOutSlowIn(0.25) > 0.25);
        Assert.True(Easings.LinearOutSlowIn(0.75) > 0.75);
    }

    [Fact]
    public void FastOutSlowIn_IsMonotonic()
    {
        var previous = 0.0;
        for (var i = 1; i <= 100; i++)
        {
            var value = Easings.FastOutSlowIn(i / 100.0);
            Assert.True(value >= previous, $"went backwards at {i}%");
            previous = value;
        }
    }
}

public class MaterialMotionControllerTests
{
    [Fact]
    public void ATestClockScopeOwnsControllersCreatedAfterActivation()
    {
        using var clock = new MotionTestClockScope();
        var controller = MaterialElementContext.Ensure(new Border()).Motion;
        var tween = controller.AddTween(0, TimeSpan.FromMilliseconds(100), Easings.Linear);
        tween.AnimateTo(1);

        clock.AdvanceBy(TimeSpan.FromMilliseconds(40));

        Assert.Equal(1, clock.ControllerCount);
        Assert.Equal(0.4, tween.Value, 6);
    }

    [Fact]
    public void ATestClockScopeRejectsNestedOwners()
    {
        using var clock = new MotionTestClockScope();

        Assert.Throws<InvalidOperationException>(() => new MotionTestClockScope());
    }

    [Fact]
    public void AManualClockAdvancesByTheExactRequestedDuration()
    {
        var controller = MaterialElementContext.Ensure(new Border()).Motion;
        var tween = controller.AddTween(0, TimeSpan.FromMilliseconds(100), Easings.Linear);
        controller.UseManualClock();
        tween.AnimateTo(1);

        Assert.Equal(0, tween.Value);

        controller.AdvanceBy(TimeSpan.FromMilliseconds(40));
        Assert.Equal(0.4, tween.Value, 6);

        controller.AdvanceBy(TimeSpan.FromMilliseconds(60));
        Assert.Equal(1, tween.Value);
        Assert.False(tween.IsRunning);
    }

    [Fact]
    public void AManualClockRejectsTimeMovingBackwards()
    {
        var controller = MaterialElementContext.Ensure(new Border()).Motion;
        controller.UseManualClock();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => controller.AdvanceBy(TimeSpan.FromMilliseconds(-1)));
    }
}

public class StateLayerTests
{
    [Theory]
    [InlineData(InteractionState.Hovered, InteractionState.Hovered)]
    [InlineData(InteractionState.Hovered | InteractionState.Focused, InteractionState.Focused)]
    [InlineData(InteractionState.Hovered | InteractionState.Focused | InteractionState.Pressed, InteractionState.Pressed)]
    [InlineData(InteractionState.Focused | InteractionState.Dragged, InteractionState.Dragged)]
    [InlineData(InteractionState.None, InteractionState.None)]
    public void OnlyTheTopmostInteraction_IsShown(InteractionState state, InteractionState expected)
    {
        Assert.Equal(expected, StateLayer.Topmost(state));
    }

    [Fact]
    public void TheOpacities_AreTheStateTokens()
    {
        Assert.Equal(StateTokens.HoverStateLayerOpacity, StateLayer.Opacity(InteractionState.Hovered));
        Assert.Equal(StateTokens.FocusStateLayerOpacity, StateLayer.Opacity(InteractionState.Focused));
        Assert.Equal(StateTokens.PressedStateLayerOpacity, StateLayer.Opacity(InteractionState.Pressed));
        Assert.Equal(StateTokens.DraggedStateLayerOpacity, StateLayer.Opacity(InteractionState.Dragged));
        Assert.Equal(0, StateLayer.Opacity(InteractionState.None));
    }

    [Fact]
    public void TheFades_AreAsymmetricWhereUpstreamMakesThem()
    {
        // Focus arrives slowly and leaves quickly; a drag does the opposite.
        Assert.True(StateLayer.FadeInDuration(InteractionState.Focused)
                    > StateLayer.FadeOutDuration(InteractionState.Focused));
        Assert.True(StateLayer.FadeOutDuration(InteractionState.Dragged)
                    > StateLayer.FadeInDuration(InteractionState.Dragged));

        // Hover is symmetric.
        Assert.Equal(
            StateLayer.FadeInDuration(InteractionState.Hovered),
            StateLayer.FadeOutDuration(InteractionState.Hovered));
    }
}

public class AnimatedShapeTests
{
    private static readonly RoundedCornerShape Pill = RoundedCornerShape.Circle;
    private static readonly RoundedCornerShape Squarish = RoundedCornerShape.Uniform(12);
    private static readonly RoundedCornerShape Boxy = RoundedCornerShape.Uniform(4);
    private static readonly SpringSpec Spec = MotionTokens.Expressive.DefaultEffects;

    [Fact]
    public void ItStartsAtRestOnItsInitialShape()
    {
        var shape = new AnimatedShape(Pill, Spec);

        Assert.False(shape.IsRunning);
        Assert.Same(Pill, shape.Current);
    }

    [Fact]
    public void ItMorphsToANewShapeAndArrives()
    {
        var shape = new AnimatedShape(Pill, Spec);
        shape.AnimateTo(Squarish);

        Assert.True(shape.IsRunning);

        while (shape.IsRunning)
            shape.Progress.Tick(TimeSpan.FromSeconds(1.0 / 60));

        Assert.Same(Squarish, shape.Current);
    }

    [Fact]
    public void TheVisibleShape_DoesNotJumpWhenTheMorphIsReversed()
    {
        var shape = new AnimatedShape(Pill, Spec);
        shape.AnimateTo(Squarish);

        for (var i = 0; i < 3; i++)
            shape.Progress.Tick(TimeSpan.FromSeconds(1.0 / 60));

        var before = shape.Current.TopStart.Resolve(new Avalonia.Size(100, 40));
        shape.AnimateTo(Pill);
        var after = shape.Current.TopStart.Resolve(new Avalonia.Size(100, 40));

        Assert.Equal(before, after, 9);
    }

    [Fact]
    public void AReversal_KeepsItsMomentum()
    {
        var shape = new AnimatedShape(Pill, Spec);
        shape.AnimateTo(Squarish);

        for (var i = 0; i < 3; i++)
            shape.Progress.Tick(TimeSpan.FromSeconds(1.0 / 60));

        shape.AnimateTo(Pill);
        shape.Progress.Tick(TimeSpan.FromMilliseconds(1));

        // Flipping progress means the velocity has to flip too, or the morph would stall and
        // restart. One millisecond in, it should still be heading the way it was.
        Assert.True(shape.Progress.Value < 1.0);
        Assert.True(shape.IsRunning);
    }

    [Fact]
    public void RepeatedInterruption_DoesNotNestInterpolationsWithoutBound()
    {
        // Freezing the visible shape naively would wrap a new interpolation around the last one
        // every time, and a shape toggled fast enough would overflow the stack when resolved.
        var shape = new AnimatedShape(Pill, Spec);
        var targets = new[] { Squarish, Boxy, Pill };

        for (var i = 0; i < 2000; i++)
        {
            shape.AnimateTo(targets[i % targets.Length]);
            shape.Progress.Tick(TimeSpan.FromMilliseconds(2));
        }

        // Resolving is what would blow the stack, so the assertion is that this returns at all.
        var radius = shape.ToCornerRadius(new Avalonia.Size(120, 40));
        Assert.True(radius.TopLeft >= 0);
    }

    [Fact]
    public void TheCurrentShape_IsMemoisedBetweenQueries()
    {
        var shape = new AnimatedShape(Pill, Spec);
        shape.AnimateTo(Squarish);
        shape.Progress.Tick(TimeSpan.FromSeconds(1.0 / 60));

        // Layout and render both ask within a frame; each should not allocate a fresh lerp.
        Assert.Same(shape.Current, shape.Current);
    }

    [Fact]
    public void AnimatingToTheCurrentTarget_IsANoOp()
    {
        var shape = new AnimatedShape(Pill, Spec);
        shape.AnimateTo(Squarish);
        shape.Progress.Tick(TimeSpan.FromSeconds(1.0 / 60));

        var progress = shape.Progress.Value;
        shape.AnimateTo(Squarish);

        Assert.Equal(progress, shape.Progress.Value);
    }
}
