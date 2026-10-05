using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class SurfaceConfigurationReadinessTests
{
    private static TokenRef MissingSpring() => new(new MaterialTokenKey<SpringSpec>("test.surface.missing-spring"));

    [AvaloniaFact]
    public void MissingShapeSpec_WaitsAndRecoversTheCurrentPressedOrCheckedTarget()
    {
        using var clock = new MotionTestClockScope();
        var button = CreateButton();
        button.Resources[Sys.Motion.Spring.EffectsDefault] = MissingSpring();
        var window = Show(button);
        try
        {
            var surface = Surface(button);
            var controller = MaterialInteraction.GetOrCreate(button);
            Assert.Null(surface.ShapeAnimation);
            controller.StartPress(new Point(20, 20));
            ButtonSurfaceState.SetCheckedCornerRadius(surface, new CornerRadius(12));
            ButtonSurfaceState.SetIsChecked(surface, true);
            Advance(clock, window, 4); Assert.Null(MotionTestClockScope.Error(button));
            controller.EndPress(); Advance(clock, window, 4);
            button.Resources.Remove(Sys.Motion.Spring.EffectsDefault);
            Advance(clock, window, 100);
            Assert.NotNull(surface.ShapeAnimation); Assert.Equal(12, surface.EffectiveCornerRadius.TopLeft, 6);
            ButtonSurfaceState.SetIsChecked(surface, false);
            Advance(clock, window, 100); Assert.Equal(20, surface.EffectiveCornerRadius.TopLeft, 6);
            Assert.Null(MotionTestClockScope.Error(button));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void WithdrawnShapeSpec_DoesNotReplaceTheRunningEffectsCapturedSpec()
    {
        using var clock = new MotionTestClockScope();
        var button = CreateButton(); var window = Show(button);
        try
        {
            var surface = Surface(button);
            var controller = MaterialInteraction.GetOrCreate(button);
            controller.StartPress(new Point(20, 20)); Advance(clock, window, 3);
            button.Resources[Sys.Motion.Spring.EffectsDefault] = MissingSpring(); Flush(window);
            Assert.Null(surface.ShapeAnimation);
            Advance(clock, window, 100);
            Assert.Equal(4, surface.EffectiveCornerRadius.TopLeft, 6);
            // The new rest target waits, while the old effect remains a valid completed result.
            controller.EndPress(); Advance(clock, window, 4);
            Assert.Equal(4, surface.EffectiveCornerRadius.TopLeft, 6);
            button.Resources.Remove(Sys.Motion.Spring.EffectsDefault);
            Advance(clock, window, 100);
            Assert.Equal(20, surface.EffectiveCornerRadius.TopLeft, 6);
            Assert.Null(MotionTestClockScope.Error(button));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ShapeSpec_RebindsAfterReparentingAndUsesTheNewOwnersCurrentTarget()
    {
        using var clock = new MotionTestClockScope();
        var button = CreateButton();
        var first = new StackPanel { Children = { button } }; var second = new StackPanel();
        first.Resources[Sys.Motion.Spring.EffectsDefault] = new SpringSpec(1, 800);
        var nextSpec = new SpringSpec(1, 400);
        second.Resources[Sys.Motion.Spring.EffectsDefault] = nextSpec;
        var window = Show(new StackPanel { Children = { first, second } });
        try
        {
            var controller = MaterialInteraction.GetOrCreate(button);
            controller.StartPress(new Point(20, 20)); Advance(clock, window, 3);
            first.Children.Remove(button); second.Children.Add(button); Flush(window);
            var surface = Surface(button); Assert.Equal(nextSpec, surface.ShapeAnimation);
            controller.StartPress(new Point(20, 20)); Advance(clock, window, 100);
            Assert.Equal(4, surface.EffectiveCornerRadius.TopLeft, 6);
            controller.EndPress(); Advance(clock, window, 100);
            Assert.Equal(20, surface.EffectiveCornerRadius.TopLeft, 6);
            Assert.Null(MotionTestClockScope.Error(button));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExplicitShapeSpec_RemainsAuthoritativeAndNullRestoresThemeInheritance()
    {
        using var clock = new MotionTestClockScope();
        var button = CreateButton();
        button.Resources[Sys.Motion.Spring.EffectsDefault] = MissingSpring();
        var window = Show(button);
        try
        {
            var surface = Surface(button);
            var controller = MaterialInteraction.GetOrCreate(button);
            ButtonSurfaceState.SetShapeSpec(surface, new SpringSpec(1, 400));
            controller.StartPress(new Point(20, 20)); Advance(clock, window, 100);
            Assert.Equal(4, surface.EffectiveCornerRadius.TopLeft, 6);
            ButtonSurfaceState.SetShapeSpec(surface, default(SpringSpec));
            controller.EndPress(); Advance(clock, window, 4);
            Assert.Equal(4, surface.EffectiveCornerRadius.TopLeft, 6);
            button.Resources.Remove(Sys.Motion.Spring.EffectsDefault); Advance(clock, window, 4);
            // An explicit invalid value is not replaced with the ambient theme spring.
            Assert.Equal(4, surface.EffectiveCornerRadius.TopLeft, 6);
            ButtonSurfaceState.SetShapeSpec(surface, null); Advance(clock, window, 100);
            Assert.Equal(20, surface.EffectiveCornerRadius.TopLeft, 6);
            Assert.Null(MotionTestClockScope.Error(button));
        }
        finally { window.Close(); }
    }

    private static Button CreateButton()
    {
        var button = new Button { Width = 80, Height = 48, CornerRadius = new CornerRadius(20),
            Content = new Border { Width = 16, Height = 16 } };
        MaterialButton.SetPressedCornerRadius(button, new CornerRadius(4));
        return button;
    }
    private static MaterialSurface Surface(Control owner) => owner.GetVisualDescendants().OfType<MaterialSurface>().Single();
    private static Window Show(Control control)
    {
        var window = new MotionTestWindow { Width = 320, Height = 240 };
        window.Styles.Add(new MaterialTheme()); window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    {
        for (var frame = 0; frame < frames; frame++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
