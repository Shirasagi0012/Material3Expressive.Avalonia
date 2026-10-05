using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class InputMotionReadinessTests
{
    private static TokenRef MissingSpring() => new(new MaterialTokenKey<SpringSpec>("test.input.missing-spring"));

    [AvaloniaFact]
    public void SelectionMotion_WaitsForConfiguration_AndRestoresAfterReparenting()
    {
        var check = new CheckBox(); var radio = new RadioButton(); var toggle = new ToggleSwitch();
        var first = new StackPanel { Children = { check, radio, toggle } }; var second = new StackPanel();
        first.Resources[Comp.CheckBox.ShapeAnimation] = MissingSpring();
        first.Resources[Comp.CheckBox.ColorAnimation] = MissingSpring();
        first.Resources[Comp.RadioButton.DotAnimation] = MissingSpring();
        first.Resources[Comp.RadioButton.ColorAnimation] = MissingSpring();
        first.Resources[Comp.Switch.ThumbAnimation] = MissingSpring();
        using var clock = new MotionTestClockScope(); var window = Show(new StackPanel { Children = { first, second } });
        try
        {
            check.IsChecked = radio.IsChecked = toggle.IsChecked = true; Advance(clock, window, 4);
            Assert.Null(check.GetVisualDescendants().OfType<CheckBoxIndicator>().Single().ShapeAnimation);
            Assert.Null(radio.GetVisualDescendants().OfType<RadioButtonIndicator>().Single().DotAnimation);
            Assert.Null(toggle.GetVisualDescendants().OfType<SwitchThumbLayout>().Single().ThumbAnimation);
            Assert.Null(MotionTestClockScope.Error(check));
            foreach (var control in new Control[] { check, radio, toggle }) { first.Children.Remove(control); second.Children.Add(control); }
            Advance(clock, window, 100);
            Assert.Equal(1, check.GetVisualDescendants().OfType<CheckBoxIndicator>().Single().DrawFraction);
            Assert.Equal(6, radio.GetVisualDescendants().OfType<RadioButtonIndicator>().Single().DotRadius);
            Assert.Equal(24, toggle.GetVisualDescendants().OfType<SwitchThumbLayout>().Single().PaintedSize);
            Assert.Null(MotionTestClockScope.Error(check));
            // A later temporary resource failure also waits rather than constructing stiffness=0.
            second.Resources[Comp.CheckBox.ShapeAnimation] = MissingSpring();
            second.Resources[Comp.RadioButton.DotAnimation] = MissingSpring();
            second.Resources[Comp.Switch.ThumbAnimation] = MissingSpring();
            check.IsChecked = radio.IsChecked = toggle.IsChecked = false; Advance(clock, window, 4);
            second.Resources.Clear(); Advance(clock, window, 100);
            Assert.Equal(0, check.GetVisualDescendants().OfType<CheckBoxIndicator>().Single().DrawFraction);
            Assert.Equal(0, radio.GetVisualDescendants().OfType<RadioButtonIndicator>().Single().DotRadius);
            Assert.Equal(16, toggle.GetVisualDescendants().OfType<SwitchThumbLayout>().Single().PaintedSize);
            Assert.Null(MotionTestClockScope.Error(check));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TooltipMissingMotion_PreservesSharedStateAndCurrentAppearanceUntilRestored()
    {
        var state = new TooltipState(initialIsVisible: true);
        var tooltip = new TooltipBox { State = state, Content = new Border { Width = 20, Height = 20 },
            Tooltip = new PlainTooltip { Content = new Border { Width = 20, Height = 10 } } };
        tooltip.Resources[Comp.TooltipBox.ScaleAnimation] = MissingSpring();
        using var clock = new MotionTestClockScope(); var window = DialogTests.Show(new Grid { Children = { tooltip } });
        try
        {
            Advance(clock, window, 4); Assert.Null(tooltip.ScaleAnimation); Assert.Same(state, tooltip.State);
            tooltip.Resources.Remove(Comp.TooltipBox.ScaleAnimation); Advance(clock, window, 100);
            Assert.Equal(1, tooltip.AppearanceAlpha); Assert.True(state.IsVisible);
            state.Dismiss(); Advance(clock, window, 3);
            tooltip.Resources[Comp.TooltipBox.ScaleAnimation] = MissingSpring(); Flush(window);
            var heldAlpha = tooltip.AppearanceAlpha; var heldScale = tooltip.AppearanceScale;
            Advance(clock, window, 10);
            Assert.Equal(heldAlpha, tooltip.AppearanceAlpha); Assert.Equal(heldScale, tooltip.AppearanceScale);
            Assert.True(state.IsVisible); Assert.True(tooltip.IsPresented);
            tooltip.Resources.Remove(Comp.TooltipBox.ScaleAnimation); Flush(window);
            Assert.Equal(heldAlpha, tooltip.AppearanceAlpha); Assert.Equal(heldScale, tooltip.AppearanceScale);
            Advance(clock, window, 100);
            Assert.False(state.IsVisible); Assert.False(tooltip.IsPresented); Assert.Same(state, tooltip.State);
            Assert.Null(MotionTestClockScope.Error(tooltip));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SnackbarHost_WaitsForMotionBeforePresentingCurrentRequest()
    {
        var state = new SnackbarHostState();
        var host = new SnackbarHost { State = state,
            SnackbarTemplate = new FuncDataTemplate<object>((_, _) => new Border { Width = 40, Height = 20 }) };
        host.Resources[Comp.Snackbar.ScaleAnimation] = MissingSpring();
        host.Resources[Comp.Snackbar.OpacityAnimation] = MissingSpring();
        var request = state.ShowSnackbarAsync("configuration test", duration: SnackbarDuration.Indefinite);
        using var clock = new MotionTestClockScope(); var window = Show(host);
        try
        {
            Advance(clock, window, 4); Assert.NotNull(state.CurrentSnackbarData); Assert.False(request.IsCompleted);
            Assert.Empty(host.GetVisualDescendants().OfType<SnackbarPresenter>());
            host.Resources.Clear(); Advance(clock, window, 100);
            var presenter = Assert.Single(host.GetVisualDescendants().OfType<SnackbarPresenter>());
            Assert.Equal(1, presenter.Opacity);
            state.CurrentSnackbarData!.Dismiss(); Advance(clock, window, 100);
            Assert.True(request.IsCompletedSuccessfully); Assert.Empty(host.GetVisualDescendants().OfType<SnackbarPresenter>());
            Assert.Null(MotionTestClockScope.Error(host));
        }
        finally { state.CurrentSnackbarData?.Dismiss(); window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void WavyRendering_WaitsForRequiredWavelengthAndResumesOnResourceRecovery(bool circular)
    {
        WavyProgressIndicator indicator = circular ? new CircularWavyProgressIndicator() : new LinearWavyProgressIndicator();
        indicator.Progress = .5; indicator.Amplitude = 1; indicator.WaveSpeed = 0;
        var key = circular ? Comp.CircularProgressIndicator.ActiveWaveWavelength : Comp.LinearProgressIndicator.ActiveWaveWavelength;
        indicator.Resources[key] = new TokenRef(new MaterialTokenKey<double>("test.input.missing-wavelength"));
        using var clock = new MotionTestClockScope(); var window = Show(indicator);
        try
        {
            using (window.CaptureRenderedFrame()) { }
            Assert.Null(indicator.DefaultWavelength); Assert.Equal(.5, indicator.Progress);
            indicator.Resources.Remove(key); Advance(clock, window, 2);
            using (window.CaptureRenderedFrame()) { }
            Assert.NotNull(indicator.DefaultWavelength);
            indicator.Resources[key] = new TokenRef(new MaterialTokenKey<double>("test.input.missing-again")); Flush(window);
            using (window.CaptureRenderedFrame()) { }
            Assert.Null(indicator.DefaultWavelength);
            indicator.Resources.Remove(key); Advance(clock, window, 2);
            using (window.CaptureRenderedFrame()) { }
            Assert.NotNull(indicator.DefaultWavelength); Assert.Equal(.5, indicator.Progress);
            Assert.Null(MotionTestClockScope.Error(indicator));
        }
        finally { window.Close(); }
    }

    private static Window Show(Control content)
    {
        var window = new MotionTestWindow { Width = 500, Height = 400 };
        window.Styles.Add(new MaterialTheme()); window.Content = new StackPanel { Children = { content } };
        window.Show(); Flush(window); return window;
    }
    private static void Advance(MotionTestClockScope clock, Window window, int count)
    { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
