using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class ButtonInteractionSourceTests
{
    [AvaloniaTheory]
    [InlineData("Filled")]
    [InlineData("Tonal")]
    [InlineData("Elevated")]
    [InlineData("Outlined")]
    [InlineData("Text")]
    public void DisabledExternalPress_UsesDisabledContentTokenForRipple(string variant)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var button = new Button();
        MaterialButton.SetInteractionSource(button, source);
        var window = DialogTests.Show(new Grid { Children = { button } });
        button.Theme = (ControlTheme)window.FindResource($"Material{variant}ButtonTheme")!; Flush(window);
        var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        source.Emit(new MaterialInteractionEvent.Press(new(24, 20)));
        button.IsEnabled = false; Flush(window);
        Assert.Equal(button.Foreground, surface.StateLayerBrush);
        Assert.Equal(1, MaterialInteraction.GetOrCreate(button).RippleCount);
        button.Foreground = Avalonia.Media.Brushes.Red; Flush(window);
        Assert.NotEqual(button.Foreground, surface.StateLayerBrush);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverlappingExternalPresses_AffectShapeWithoutActivation(bool toggle)
    {
        using var clock = new MotionTestClockScope();
        Button button = toggle ? new ToggleButton { IsChecked = true } : new Button();
        MaterialButton.SetPressedCornerRadius(button, new(6));
        var source = new MaterialInteractionSource(); MaterialButton.SetInteractionSource(button, source);
        var window = DialogTests.Show(new Grid { Children = { button } });
        var clicks = 0; button.Click += (_, _) => clicks++;
        var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        var a = new MaterialInteractionEvent.Press(new(24, 20)); var b = new MaterialInteractionEvent.Press(new(24, 20));
        source.Emit(a); source.Emit(b); Advance(clock, window);
        Assert.Equal(new CornerRadius(6), surface.EffectiveCornerRadius);
        Assert.False(button.IsPressed); Assert.True(ButtonInteraction.GetIsPressed(button));
        source.Emit(new MaterialInteractionEvent.Release(a)); Advance(clock, window);
        Assert.Equal(new CornerRadius(6), surface.EffectiveCornerRadius);
        source.Emit(new MaterialInteractionEvent.CancelPress(new(default)));
        Assert.True(ButtonInteraction.GetIsPressed(button));
        source.Emit(new MaterialInteractionEvent.CancelPress(b)); Advance(clock, window);
        Assert.False(ButtonInteraction.GetIsPressed(button)); Assert.Equal(0, clicks);
        if (button is ToggleButton selected) Assert.True(selected.IsChecked);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void SourceReplacement_PreservesShapeAndElevationButRecreatesIndication()
    {
        using var clock = new MotionTestClockScope();
        var old = new MaterialInteractionSource(); var next = new MaterialInteractionSource();
        var button = new ToggleButton(); MaterialButton.SetInteractionSource(button, old);
        MaterialButton.SetElevation(button, new() { Default = 1, Pressed = 3, Hovered = 5 });
        var window = DialogTests.Show(new Grid { Children = { button } });
        var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        var press = new MaterialInteractionEvent.Press(new(24, 20));
        old.Emit(press); Advance(clock, window);
        Assert.Equal(3, surface.ShadowElevation); Assert.Equal(1, MaterialInteraction.GetOrCreate(button).RippleCount);
        MaterialButton.SetInteractionSource(button, next); Flush(window);
        Assert.True(ButtonInteraction.GetIsPressed(button));
        Assert.Equal(0, MaterialInteraction.GetOrCreate(button).RippleCount);
        old.Emit(new MaterialInteractionEvent.Release(press)); Advance(clock, window);
        Assert.True(ButtonInteraction.GetIsPressed(button)); Assert.Equal(3, surface.ShadowElevation);
        var hover = new MaterialInteractionEvent.HoverEnter(); next.Emit(hover); Advance(clock, window);
        Assert.False(ButtonInteraction.GetIsPressed(button)); Assert.Equal(5, surface.ShadowElevation);
        next.Emit(new MaterialInteractionEvent.HoverExit(hover)); Advance(clock, window);
        Assert.Equal(3, surface.ShadowElevation); // remembered old press still owns elevation
        next.Emit(new MaterialInteractionEvent.CancelPress(press)); Advance(clock, window);
        Assert.Equal(1, surface.ShadowElevation);
        window.Close(); Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeInput_IsPairedAndDisableCancelsOnlyOwnedPress(bool toggle)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        Button button = toggle ? new ToggleButton() : new Button(); MaterialButton.SetInteractionSource(button, source);
        var window = DialogTests.Show(new Grid { Children = { button } });
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center); window.MouseDown(center, MouseButton.Left); Flush(window);
        var native = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
        var external = new MaterialInteractionEvent.Press(new(24, 20)); source.Emit(external);
        button.IsEnabled = false; Flush(window);
        Assert.Same(native, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
        if (toggle) Assert.True(ButtonInteraction.GetIsPressed(button));
        window.MouseUp(center, MouseButton.Left); Assert.Empty(events.OfType<MaterialInteractionEvent.Release>());
        source.Emit(new MaterialInteractionEvent.CancelPress(external));
        button.IsEnabled = true; window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left); Flush(window);
        Assert.Same(events.OfType<MaterialInteractionEvent.Press>().Last(), Assert.Single(events.OfType<MaterialInteractionEvent.Release>()).Start);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void TemplateReplacementAndReattachment_DoNotReplayOldInteractions()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var button = new ToggleButton(); MaterialButton.SetInteractionSource(button, source);
        var root = new Grid { Children = { button } }; var window = DialogTests.Show(root);
        source.Emit(new MaterialInteractionEvent.Press(new(24, 20))); Advance(clock, window);
        button.Template = null; Flush(window);
        button.ClearValue(TemplatedControl.TemplateProperty); Flush(window);
        Assert.False(ButtonInteraction.GetIsPressed(button)); Assert.Equal(0, MaterialInteraction.GetOrCreate(button).RippleCount);
        source.Emit(new MaterialInteractionEvent.Press(new(24, 20))); Assert.True(ButtonInteraction.GetIsPressed(button));
        root.Children.Remove(button); Flush(window); Assert.False(ButtonInteraction.GetIsPressed(button));
        source.Emit(new MaterialInteractionEvent.Press(new(24, 20)));
        root.Children.Add(button); Flush(window); Assert.False(ButtonInteraction.GetIsPressed(button));
        root.Children.Clear(); Flush(window); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void SplitParts_HaveIndependentSourcesAndShareTooltipLabelInput()
    {
        using var clock = new MotionTestClockScope();
        var leading = new Button { Content = "Save" }; var trailing = new ToggleButton { Content = "+" };
        var source = new MaterialInteractionSource(); var other = new MaterialInteractionSource();
        MaterialButton.SetInteractionSource(leading, source); MaterialButton.SetInteractionSource(trailing, other);
        var split = new Material3Expressive.Components.SplitButton { LeadingButton = leading, TrailingButton = trailing };
        var label = new TooltipLabel { InteractionSource = source, Content = split, Label = new PlainTooltip { Content = "Save" } };
        var window = DialogTests.Show(new Grid { Children = { label } });
        leading.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonLeadingTheme")!;
        trailing.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonTrailingTheme")!; Flush(window);
        var center = leading.TranslatePoint(new Point(leading.Bounds.Width / 2, leading.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center); Flush(window); Assert.True(label.IsPresented);
        window.MouseDown(center, MouseButton.Left); Flush(window);
        Assert.True(ButtonInteraction.GetIsPressed(leading)); Assert.False(ButtonInteraction.GetIsPressed(trailing));
        other.Emit(new MaterialInteractionEvent.Press(new(20, 20))); Assert.True(ButtonInteraction.GetIsPressed(trailing));
        window.MouseUp(center, MouseButton.Left); Flush(window);
        Assert.False(ButtonInteraction.GetIsPressed(leading)); Assert.True(ButtonInteraction.GetIsPressed(trailing)); Assert.False(trailing.IsChecked);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    static void Advance(MotionTestClockScope clock, Window window)
    { for (var i = 0; i < 60; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
}
