using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class InputFeedbackConfigurationTests
{
    [AvaloniaFact]
    public void SelectionMotion_UsesThemeProperties_AndKeepsSystemReferencesLive()
    {
        var check = new CheckBox(); var radio = new RadioButton(); var toggle = new ToggleSwitch();
        var content = new StackPanel { Children = { check, radio, toggle } };
        using var clock = new MotionTestClockScope(); var window = Show(content);
        try
        {
            var checkPart = check.GetVisualDescendants().OfType<CheckBoxIndicator>().Single();
            var radioPart = radio.GetVisualDescendants().OfType<RadioButtonIndicator>().Single();
            var thumb = toggle.GetVisualDescendants().OfType<SwitchThumbLayout>().Single();
            var spatial = new SpringSpec(.7, 321);
            content.Resources[Sys.Motion.Spring.SpatialFast] = spatial; Flush(window);
            Assert.Equal(spatial, radioPart.DotAnimation); Assert.Equal(spatial, thumb.ThumbAnimation);
            var shape = new SpringSpec(.8, 432);
            check.Resources[Comp.CheckBox.ShapeAnimation] = shape; Flush(window); Assert.Equal(shape, checkPart.ShapeAnimation);
            var effects = new SpringSpec(1, 654);
            content.Resources[Sys.Motion.Spring.EffectsDefault] = effects; Flush(window);
            Assert.Equal(effects, checkPart.ColorAnimation); Assert.Equal(effects, radioPart.ColorAnimation);
            var local = new SpringSpec(1, 777); radioPart.DotAnimation = local;
            content.Resources[Sys.Motion.Spring.SpatialFast] = new SpringSpec(.6, 222); Flush(window);
            Assert.Equal(local, radioPart.DotAnimation);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void FeedbackMotion_UsesThemeProperties_WithoutOwningRequestState()
    {
        var tooltip = new TooltipBox(); var snackbar = new SnackbarHost();
        var content = new StackPanel { Children = { tooltip, snackbar } };
        using var clock = new MotionTestClockScope(); var window = Show(content);
        try
        {
            var state = tooltip.State; var hostState = snackbar.State;
            var spec = new SpringSpec(.8, 456);
            content.Resources[Sys.Motion.Spring.SpatialFast] = spec; Flush(window);
            Assert.Equal(spec, tooltip.ScaleAnimation); Assert.Equal(spec, snackbar.ScaleAnimation);
            spec = new SpringSpec(1, 567);
            content.Resources[Sys.Motion.Spring.EffectsFast] = spec; Flush(window);
            Assert.Equal(spec, tooltip.OpacityAnimation); Assert.Equal(spec, snackbar.OpacityAnimation);
            Assert.Same(state, tooltip.State); Assert.Same(hostState, snackbar.State);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SnackbarHeightToken_UpdatesLayoutWithoutUnrelatedInvalidation()
    {
        var snackbar = new Snackbar { Content = new Border { Width = 20, Height = 8 } };
        using var clock = new MotionTestClockScope(); var window = Show(snackbar);
        try
        {
            var layout = snackbar.GetVisualDescendants().OfType<SnackbarLayout>().Single();
            Assert.Equal(48, layout.SingleLineContainerHeight); Assert.Equal(48, layout.Bounds.Height);
            snackbar.Resources[Comp.Snackbar.SingleLineContainerHeight] = 72d; Flush(window);
            Assert.Equal(72, layout.SingleLineContainerHeight); Assert.Equal(72, layout.Bounds.Height);
            snackbar.Resources.Remove(Comp.Snackbar.SingleLineContainerHeight); Flush(window);
            Assert.Equal(48, layout.SingleLineContainerHeight); Assert.Equal(48, layout.Bounds.Height);
        }
        finally { window.Close(); }
    }

    private static Window Show(Control content)
    {
        var window = new MotionTestWindow { Width = 500, Height = 400 };
        window.Styles.Add(new MaterialTheme()); window.Content = new StackPanel { Children = { content } };
        window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
