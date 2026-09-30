using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using ComposeAnimation.TestSupport;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Tokens;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>Material behavior over the packaged Compose runtime, including intermediate frames.</summary>
public class MaterialMotionTests
{
    private static MaterialSurface Surface(Button button) =>
        button.GetVisualDescendants().OfType<MaterialSurface>().First();

    [AvaloniaFact]
    public void InterruptedStateLayer_ContinuesWithTheIncomingTween()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);
        controller.Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(7.5));
        Assert.Equal(0.04, controller.StateLayerOpacity, 6);

        controller.Set(InteractionState.Focused, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(22.5));
        Assert.Equal(0.07, controller.StateLayerOpacity, 6);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(22.5));
        Assert.Equal(0.10, controller.StateLayerOpacity, 6);
        Assert.Null(MotionTestClockScope.Error(button));
        window.Close();
    }

    [AvaloniaFact]
    public void HoverElevation_UsesDifferentEntryAndExitCurves_WithoutOverwritingTargets()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, "MaterialElevatedButtonTheme");
        var surface = Surface(button);
        var controller = MaterialInteraction.GetOrCreate(button);
        Assert.Equal(1, surface.ShadowElevation);
        controller.Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        Assert.Equal(1 + 2 * ComposeEasings.FastOutSlowIn.Ease(0.5), surface.ShadowElevation, 6);
        Assert.Equal(1, MaterialButton.GetElevation(button)!.Default);
        Assert.Equal(3, MaterialButton.GetElevation(button)!.Hovered);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        Assert.Equal(3, surface.ShadowElevation);

        controller.Set(InteractionState.Hovered, false);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        Assert.Equal(2, surface.ShadowElevation, 6);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        Assert.Equal(1, surface.ShadowElevation);
        Assert.Null(MotionTestClockScope.Error(button));
        window.Close();
    }

    [AvaloniaFact]
    public void PressWhileHovered_LowersElevation_ThenReturnsToHover()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, "MaterialElevatedButtonTheme");
        var controller = MaterialInteraction.GetOrCreate(button);
        var surface = Surface(button);
        controller.Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        controller.StartPress(new Point(10, 10));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(1, surface.ShadowElevation);
        controller.EndPress();
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(3, surface.ShadowElevation);
        window.Close();
    }

    [AvaloniaFact]
    public void FocusExit_Uses150Milliseconds_AndArrivalOrderSelectsElevation()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, "MaterialElevatedButtonTheme");
        var controller = MaterialInteraction.GetOrCreate(button);
        var surface = Surface(button);
        MaterialButton.SetElevation(button, MaterialButton.GetElevation(button)! with { Focused = 5 });
        controller.Set(InteractionState.Hovered, true);
        controller.Set(InteractionState.Focused, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(5, surface.ShadowElevation);
        controller.Set(InteractionState.Hovered, false);
        Assert.Equal(5, surface.ShadowElevation);
        controller.Set(InteractionState.Focused, false);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(75));
        Assert.Equal(3, surface.ShadowElevation, 6);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(75));
        Assert.Equal(1, surface.ShadowElevation);
        window.Close();
    }

    [AvaloniaFact]
    public void DisabledDuringMotion_SnapsElevation_AndClearsPress()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, "MaterialElevatedButtonTheme");
        var controller = MaterialInteraction.GetOrCreate(button);
        controller.Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        controller.StartPress(new Point(10, 10));
        button.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, Surface(button).ShadowElevation);
        Assert.Equal(0, controller.RippleCount);
        Assert.Equal(InteractionState.None, controller.Interaction);
        controller.StartPress(new Point(10, 10));
        Assert.Equal(0, controller.RippleCount);
        button.IsEnabled = true;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, Surface(button).ShadowElevation);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Detach_CancelsAllFrameWaiters_AndReattachHasNoStaleInput()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, "MaterialElevatedButtonTheme");
        var controller = MaterialInteraction.GetOrCreate(button);
        controller.Set(InteractionState.Hovered, true);
        controller.StartPress(new Point(8, 8));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(30));
        Assert.True(clock.PendingFrameCount > 0);
        var oldMotion = controller.Motion!;
        window.Content = null;
        Dispatcher.UIThread.RunJobs();
        await oldMotion.DisposeAsync();
        Assert.Equal(0, oldMotion.ActiveTaskCount);
        Assert.Equal(0, oldMotion.ActiveChildCount);
        Assert.Equal(0, oldMotion.SubscriptionCount);
        Assert.Equal(0, oldMotion.BindingCount);
        Assert.Null(oldMotion.LastError);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Equal(0, controller.RippleCount);
        Assert.Equal(InteractionState.None, controller.Interaction);

        window.Content = button;
        window.UpdateLayout();
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromSeconds(2));
        Assert.Equal(1, Surface(button).ShadowElevation);
        Assert.Equal(0, controller.StateLayerOpacity);
        controller.StartPress(new Point(8, 8));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(30));
        Assert.Equal(1, controller.RippleCount);
        Assert.Null(MotionTestClockScope.Error(button));
        window.Close();
    }

    [AvaloniaFact]
    public void TemplateReplacement_CancelsOldShapeAndRipple_AndInitializesNewPresenter()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);
        controller.StartPress(new Point(8, 8));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(25));
        var oldSurface = Surface(button);
        button.Template = new FuncControlTemplate<Button>((_, _) => new MaterialSurface
        {
            CornerRadius = new CornerRadius(20),
            PressedCornerRadius = new CornerRadius(4)
        });
        button.ApplyTemplate();
        window.UpdateLayout();
        var replacement = Surface(button);
        Assert.NotSame(oldSurface, replacement);
        Assert.Equal(0, controller.RippleCount);
        controller.StartPress(new Point(8, 8));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        Assert.Equal(4, replacement.EffectiveCornerRadius.TopLeft, 6);
        Assert.Equal(oldSurface.CornerRadius, oldSurface.EffectiveCornerRadius);
        Assert.Null(MotionTestClockScope.Error(button));
        window.Close();
    }

    [AvaloniaFact]
    public void RepeatedTaps_AllowOldRippleToFinish_AndEventuallyStopRequestingFrames()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);
        controller.StartPress(new Point(8, 8));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(20));
        controller.EndPress();
        controller.StartPress(new Point(16, 8));
        Assert.Equal(2, controller.RippleCount);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(225));
        controller.EndPress();
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(150));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        Assert.Equal(0, controller.RippleCount);
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Null(MotionTestClockScope.Error(button));
        window.Close();
    }

    [AvaloniaFact]
    public async Task LongLivedButton_1500RipplesReturnEveryRegistrationToBaseline()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);
        var motion = controller.Motion!;
        // Initialize persistent shape/elevation without a transient ripple in the baseline.
        controller.Set(InteractionState.Pressed, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(225));
        controller.EndPress();
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(150));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        await AnimationTestDispatcher.WaitUntilAsync(() => motion.ActiveTaskCount == 0);
        var children = motion.ActiveChildCount;
        var subscriptions = motion.SubscriptionCount;
        var bindings = motion.BindingCount;
        for (var i = 0; i < 1500; i++)
        {
            controller.StartPress(new Point(8, 8));
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(20));
            controller.EndPress();
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(225));
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(150));
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
            await AnimationTestDispatcher.WaitUntilAsync(() =>
                motion.ActiveTaskCount == 0 && motion.ActiveChildCount == children);
            Assert.Equal(0, controller.RippleCount);
            Assert.Equal(0, motion.ActiveTaskCount);
            Assert.Equal(0, motion.PendingAnimationCount);
            Assert.Equal(children, motion.ActiveChildCount);
            Assert.Equal(subscriptions, motion.SubscriptionCount);
            Assert.Equal(bindings, motion.BindingCount);
            Assert.Equal(0, clock.PendingFrameCount);
        }
        Assert.Null(MotionTestClockScope.Error(button));
        window.Close();
    }

    [AvaloniaFact]
    public void OwnerMotionTokenOverride_ReachesTheComposeSpring()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        button.Resources[MaterialTokens.Sys.Motion.Spring.EffectsDefault] =
            new Material3Expressive.Foundation.Motion.SpringSpec(1, 100);
        var controller = MaterialInteraction.GetOrCreate(button);
        var surface = Surface(button);
        controller.StartPress(new Point(8, 8));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
        // Critical damping: p(t) = 1 - (1 + sqrt(k)*t) * exp(-sqrt(k)*t).
        var progress = 1 - 2 * Math.Exp(-1);
        var start = Math.Min(surface.Bounds.Width, surface.Bounds.Height) / 2;
        Assert.Equal(start + (surface.PressedCornerRadius!.Value.TopLeft - start) * progress,
            surface.EffectiveCornerRadius.TopLeft, 5);
        window.Close();
    }

    [AvaloniaFact]
    public void ZeroDurationScale_SettlesFiniteMotion_WithoutLeakingRippleWaitsOnDetach()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, "MaterialElevatedButtonTheme");
        var controller = MaterialInteraction.GetOrCreate(button);
        try
        {
            ((MotionTestWindow)window).MotionRoot.DurationScale = 0;
            controller.Set(InteractionState.Hovered, true);
            controller.StartPress(new Point(8, 8));
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.Zero);
            Assert.Equal(0.08, controller.StateLayerOpacity, 6);
            Assert.Equal(Surface(button).PressedCornerRadius, Surface(button).EffectiveCornerRadius);
            Assert.Equal(0, clock.PendingFrameCount); // held ripple waits for release, not frames
            controller.EndPress();
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.Zero);
            Assert.Equal(0, controller.RippleCount);
            Assert.Equal(3, Surface(button).ShadowElevation);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task TopLevelClock_AdvancesRealAsyncFrames_AndSurvivesRapidRetargeting()
    {
        // Deliberately no manual clock: exercise RequestAnimationFrame and dispatcher continuations.
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            var controller = MaterialInteraction.GetOrCreate(button);
            var surface = Surface(button);
            controller.StartPress(new Point(8, 8));
            var start = surface.EffectiveCornerRadius.TopLeft;
            for (var i = 0; i < 60 && surface.EffectiveCornerRadius.TopLeft == start; i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                await Task.Delay(16);
            }
            Assert.True(surface.EffectiveCornerRadius.TopLeft < start);

            controller.EndPress();
            controller.StartPress(new Point(12, 8));
            controller.EndPress();
            for (var i = 0; i < 100 && (controller.RippleCount != 0
                || surface.EffectiveCornerRadius.TopLeft != start); i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                await Task.Delay(16);
            }
            Assert.Equal(start, surface.EffectiveCornerRadius.TopLeft, 6);
            Assert.Equal(0, controller.RippleCount);
            Assert.Null(MotionTestClockScope.Error(button));
            window.Content = null;
            Assert.Null(controller.Motion);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SimultaneousControls_ShareInjectedTime_ButKeepIndependentMotion()
    {
        using var clock = new MotionTestClockScope();
        var firstWindow = ButtonThemeFixture.ShowWindow(out var first);
        var secondWindow = ButtonThemeFixture.ShowWindow(out var second);
        var firstController = MaterialInteraction.GetOrCreate(first);
        var secondController = MaterialInteraction.GetOrCreate(second);
        firstController.Set(InteractionState.Hovered, true);
        secondController.Set(InteractionState.Focused, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(7.5));
        Assert.Equal(0.04, firstController.StateLayerOpacity, 6);
        Assert.Equal(0.1 / 6, secondController.StateLayerOpacity, 6);
        firstWindow.Close();
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(37.5));
        Assert.Equal(0.10, secondController.StateLayerOpacity, 6);
        Assert.Null(MotionTestClockScope.Error(second));
        secondWindow.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }
}
