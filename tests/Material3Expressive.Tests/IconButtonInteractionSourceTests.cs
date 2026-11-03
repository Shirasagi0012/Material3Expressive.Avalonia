using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class IconButtonInteractionSourceTests
{
    [AvaloniaFact]
    public void ReplacingSourceDuringHover_DisposesOldStateLayerWithoutReplayingIt()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var next = new MaterialInteractionSource();
        var button = new IconButton { InteractionSource = source };
        var window = DialogTests.Show(new Grid { Children = { button } });
        var controller = MaterialInteraction.GetOrCreate(button);
        source.Emit(new MaterialInteractionEvent.HoverEnter());
        for (var i = 0; i < 3; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
        Assert.True(controller.StateLayerOpacity > 0);
        button.InteractionSource = next; Flush(window);
        Assert.Equal(0, controller.StateLayerOpacity);
        Advance(clock, window); Assert.Equal(0, controller.StateLayerOpacity);
        next.Emit(new MaterialInteractionEvent.HoverEnter()); Advance(clock, window);
        Assert.True(controller.StateLayerOpacity > 0);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaFact]
    public void EnablingMorphDuringHeldPress_DoesNotReplayTheOldPress()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var button = new IconButton { InteractionSource = source };
        var window = DialogTests.Show(new Grid { Children = { button } });
        var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        var first = new MaterialInteractionEvent.Press(new(24, 24));
        source.Emit(first); Advance(clock, window);
        var controller = MaterialInteraction.GetOrCreate(button);
        Assert.Equal(1, controller.RippleCount);
        MaterialButton.SetPressedCornerRadius(button, new(8)); Advance(clock, window);
        Assert.Equal(new CornerRadius(20), surface.EffectiveCornerRadius);
        Assert.Equal(0, controller.RippleCount);
        var second = new MaterialInteractionEvent.Press(new(24, 24));
        source.Emit(second); Advance(clock, window);
        Assert.Equal(new CornerRadius(8), surface.EffectiveCornerRadius);
        Assert.Equal(1, controller.RippleCount);
        source.Emit(new MaterialInteractionEvent.Release(first)); Advance(clock, window);
        Assert.Equal(new CornerRadius(8), surface.EffectiveCornerRadius);
        MaterialButton.SetPressedCornerRadius(button, null); Advance(clock, window);
        Assert.Equal(0, controller.RippleCount);
        MaterialButton.SetPressedCornerRadius(button, new(8)); Advance(clock, window);
        Assert.Equal(new CornerRadius(20), surface.EffectiveCornerRadius);
    }

    [AvaloniaFact]
    public void SourceReplacement_DuringMorph_PreservesTheRunningShape()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var button = new IconButton { InteractionSource = source };
        MaterialButton.SetPressedCornerRadius(button, new(8));
        var window = DialogTests.Show(new Grid { Children = { button } });
        var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        source.Emit(new MaterialInteractionEvent.Press(new(24, 24)));
        for (var i = 0; i < 4; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
        var before = surface.EffectiveCornerRadius;
        Assert.InRange(before.TopLeft, 8.01, 19.99);
        button.InteractionSource = new(); Flush(window);
        Assert.Equal(before, surface.EffectiveCornerRadius);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        Assert.InRange(surface.EffectiveCornerRadius.TopLeft, 8.001, before.TopLeft);
        Advance(clock, window);
        Assert.Equal(new CornerRadius(8), surface.EffectiveCornerRadius);
        Assert.Equal(0, MaterialInteraction.GetOrCreate(button).RippleCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExternalPresses_KeepShapeUntilLastIdentityEnds_WithoutActivating(bool toggle)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        IconButton button = toggle ? new IconToggleButton { CheckedCornerRadius = new(12), IsChecked = true } : new IconButton();
        button.InteractionSource = source;
        MaterialButton.SetPressedCornerRadius(button, new CornerRadius(8));
        var window = DialogTests.Show(new Grid { Children = { button } });
        var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        int clicks = 0; button.Click += (_, _) => clicks++;
        var first = new MaterialInteractionEvent.Press(new Point(24, 24));
        var second = new MaterialInteractionEvent.Press(new Point(24, 24));
        source.Emit(first); source.Emit(second); Advance(clock, window);
        Assert.Equal(new CornerRadius(8), surface.EffectiveCornerRadius);
        Assert.False(button.IsPressed); Assert.True(button.SourceIsPressed);
        source.Emit(new MaterialInteractionEvent.Release(first)); Advance(clock, window);
        Assert.Equal(new CornerRadius(8), surface.EffectiveCornerRadius);
        source.Emit(new MaterialInteractionEvent.CancelPress(new(default))); Assert.True(button.SourceIsPressed);
        source.Emit(new MaterialInteractionEvent.CancelPress(second)); Advance(clock, window);
        Assert.False(button.SourceIsPressed);
        Assert.Equal(new CornerRadius(toggle ? 12 : 20), surface.EffectiveCornerRadius);
        Assert.Equal(0, clicks);
        if (button is IconToggleButton selected) Assert.True(selected.IsChecked);
    }

    [AvaloniaFact]
    public void SourceReplacement_KeepsRememberedPressUntilNextEvent_ButDoesNotReplayRipple()
    {
        using var clock = new MotionTestClockScope();
        var first = new MaterialInteractionSource(); var second = new MaterialInteractionSource();
        var button = new IconButton { InteractionSource = first };
        MaterialButton.SetPressedCornerRadius(button, new(8));
        var window = DialogTests.Show(new Grid { Children = { button } });
        var controller = MaterialInteraction.GetOrCreate(button);
        first.Emit(new MaterialInteractionEvent.Press(new Point(24, 24))); Advance(clock, window);
        Assert.True(button.SourceIsPressed); Assert.Equal(1, controller.RippleCount);
        button.InteractionSource = second; Flush(window);
        Assert.True(button.SourceIsPressed); Assert.Equal(0, controller.RippleCount);
        first.Emit(new MaterialInteractionEvent.HoverEnter()); Assert.True(button.SourceIsPressed);
        second.Emit(new MaterialInteractionEvent.HoverEnter()); Assert.False(button.SourceIsPressed);
        second.Emit(new MaterialInteractionEvent.Press(new Point(24, 24))); Assert.True(button.SourceIsPressed);
        button.InteractionSource = null; Flush(window); Assert.True(button.SourceIsPressed);
        var center = button.TranslatePoint(new Point(24, 24), window)!.Value;
        window.MouseMove(center); Flush(window); Assert.False(button.SourceIsPressed);
    }

    [AvaloniaFact]
    public void NativeInput_PublishesPairedEvents_AndDisableCancelsOwnedPress()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add;
        var button = new IconButton { InteractionSource = source };
        var window = DialogTests.Show(new Grid { Children = { button } });
        var center = button.TranslatePoint(new Point(24, 24), window)!.Value;
        window.MouseMove(center); window.MouseDown(center, MouseButton.Left); Flush(window);
        var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
        Assert.True(button.SourceIsPressed);
        button.IsEnabled = false; Flush(window);
        Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
        Assert.False(button.SourceIsPressed);
        window.MouseUp(center, MouseButton.Left); Flush(window);
        Assert.Empty(events.OfType<MaterialInteractionEvent.Release>());
        button.IsEnabled = true;
        window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left); Flush(window);
        Assert.Same(events.OfType<MaterialInteractionEvent.Press>().Last(), Assert.Single(events.OfType<MaterialInteractionEvent.Release>()).Start);
    }

    [AvaloniaFact]
    public void DetachAndReattach_StopOldCollector_AndDoNotReplayExternalPress()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var button = new IconButton { InteractionSource = source };
        var root = new Grid { Children = { button } }; var window = DialogTests.Show(root);
        source.Emit(new MaterialInteractionEvent.Press(new Point(24, 24))); Assert.True(button.SourceIsPressed);
        root.Children.Remove(button); Flush(window);
        source.Emit(new MaterialInteractionEvent.Press(new Point(24, 24))); Assert.False(button.SourceIsPressed);
        root.Children.Add(button); Flush(window); Assert.False(button.SourceIsPressed);
        Assert.Equal(0, MaterialInteraction.GetOrCreate(button).RippleCount);
        source.Emit(new MaterialInteractionEvent.Press(new Point(24, 24))); Assert.True(button.SourceIsPressed);
        root.Children.Clear(); Flush(window);
        Assert.Equal(0, clock.PendingFrameCount); Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaFact]
    public void LabelAndIconButton_ShareNativeHoverAndPress_AndIndependentSourceDisposal()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var button = new IconButton { InteractionSource = source };
        var label = new TooltipLabel { InteractionSource = source, Content = button,
            Label = new PlainTooltip { Content = "Save" } };
        var window = DialogTests.Show(new Grid { Children = { label } });
        var center = button.TranslatePoint(new Point(24, 24), window)!.Value;
        window.MouseMove(center); Flush(window); Assert.True(label.IsPresented);
        window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left);
        window.MouseMove(new Point(500, 280)); Flush(window); Assert.False(label.IsPresented);
        Assert.False(button.SourceIsPressed);
    }

    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    static void Advance(MotionTestClockScope clock, Window window)
    { for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
}
