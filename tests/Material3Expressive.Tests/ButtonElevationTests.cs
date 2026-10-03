using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class ButtonElevationTests
{
    private static MaterialSurface Surface(Button button) =>
        button.GetVisualDescendants().OfType<MaterialSurface>().First();

    [AvaloniaFact]
    public void SharedXamlConfiguration_KeepsPlaybackAndLifetimePerButton()
    {
        using var clock = new MotionTestClockScope();
        var configured = Assert.IsType<Button>(AvaloniaXamlLoader.Load(
            new Uri("avares://Material3Expressive.Tests/Fixtures/ButtonElevation.axaml")));
        var values = Assert.IsType<ButtonElevation>(MaterialButton.GetElevation(configured));
        Assert.Equal(new ButtonElevation { Default = 1, Pressed = 2, Focused = 4, Hovered = 6 }, values);
        var firstWindow = ButtonThemeFixture.ShowWindow(out var first);
        var secondWindow = ButtonThemeFixture.ShowWindow(out var second);
        MaterialButton.SetElevation(first, values);
        MaterialButton.SetElevation(second, values);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        MaterialInteraction.GetOrCreate(first).Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        Assert.InRange(Surface(first).ShadowElevation, 1.01, 5.99);
        Assert.Equal(1, Surface(second).ShadowElevation);
        MaterialInteraction.GetOrCreate(second).Set(InteractionState.Focused, true);
        firstWindow.Close();
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(4, Surface(second).ShadowElevation);
        Assert.Same(values, MaterialButton.GetElevation(first));
        Assert.Same(values, MaterialButton.GetElevation(second));
        Assert.Null(MotionTestClockScope.Error(second));
        secondWindow.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void LocalTokens_RetargetFromCurrentHeight_AndFollowThemeChanges()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, "MaterialElevatedButtonTheme");
        button.Resources[MaterialTokens.Sys.Elevation.Level2] = 7d;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(7, MaterialButton.GetElevation(button)!.Hovered);
        MaterialInteraction.GetOrCreate(button).Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        var current = Surface(button).ShadowElevation;
        Assert.Equal(1 + 6 * ComposeEasings.FastOutSlowIn.Ease(0.5), current, 6);

        button.Resources[MaterialTokens.Comp.ElevatedButton.HoveredContainerElevation] = 9d;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(current, Surface(button).ShadowElevation, 6);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(9, Surface(button).ShadowElevation);

        button.Resources.Remove(MaterialTokens.Comp.ElevatedButton.HoveredContainerElevation);
        button.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary
        {
            [MaterialTokens.Comp.ElevatedButton.HoveredContainerElevation] = 11d
        };
        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(11, MaterialButton.GetElevation(button)!.Hovered);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(11, Surface(button).ShadowElevation);
        Assert.Null(MotionTestClockScope.Error(button));
        window.Close();
    }

    [AvaloniaFact]
    public void RemovingConfiguration_RestoresSurfaceValue_AndCancelsOldPlayback()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, "MaterialElevatedButtonTheme");
        var surface = Surface(button);
        surface.ShadowElevation = 8;
        MaterialInteraction.GetOrCreate(button).Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        Assert.InRange(surface.ShadowElevation, 1.01, 2.99);
        MaterialButton.SetElevation(button, null);
        Assert.Equal(8, surface.ShadowElevation);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(8, surface.ShadowElevation);
        Assert.Equal(0, clock.PendingFrameCount);
        button.ClearValue(MaterialButton.ElevationProperty);
        Assert.Equal(3, surface.ShadowElevation); // reattach at the current interaction target
        Assert.Null(MotionTestClockScope.Error(button));
        window.Close();
    }

    [AvaloniaFact]
    public void SwitchingToOutlinedTheme_RemovesElevationPolicy()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, "MaterialElevatedButtonTheme");
        MaterialInteraction.GetOrCreate(button).Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        button.Theme = Assert.IsType<ControlTheme>(ButtonThemeFixture.LoadButtonStyles().Resources["MaterialOutlinedButtonTheme"]);
        window.UpdateLayout();
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Null(MaterialButton.GetElevation(button));
        Assert.Equal(0, Surface(button).ShadowElevation);
        Assert.Null(MotionTestClockScope.Error(button));
        window.Close();
    }

    [AvaloniaFact]
    public void ReplacingTemplate_UnbindsOldSurface_AndAnimatesNewSurface()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, "MaterialElevatedButtonTheme");
        var controller = MaterialInteraction.GetOrCreate(button);
        controller.Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
        var oldSurface = Surface(button);
        button.Template = new FuncControlTemplate<Button>((_, _) => new MaterialSurface { ShadowElevation = 8 });
        button.ApplyTemplate();
        window.UpdateLayout();
        var replacement = Surface(button);
        Assert.NotSame(oldSurface, replacement);
        controller.Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(0, oldSurface.ShadowElevation);
        Assert.Equal(3, replacement.ShadowElevation);
        window.Close();
        Assert.Equal(8, replacement.ShadowElevation);
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void EqualInteractionHeights_UseComposeTargetBasedOutgoingPolicy_AndIgnoreDrag()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        MaterialButton.SetElevation(button, new ButtonElevation
        {
            Default = 1, Pressed = 3, Focused = 5, Hovered = 3
        });
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        var controller = MaterialInteraction.GetOrCreate(button);
        controller.Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        controller.Set(InteractionState.Dragged, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
        Assert.Equal(3, Surface(button).ShadowElevation);
        controller.Set(InteractionState.Hovered, false);
        // Button.kt matches the previous target to pressed before hover when their heights agree.
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(75));
        Assert.Equal(2, Surface(button).ShadowElevation, 6);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(75));
        Assert.Equal(1, Surface(button).ShadowElevation);
        window.Close();
    }
}
