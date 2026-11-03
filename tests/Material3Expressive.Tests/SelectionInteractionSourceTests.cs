using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class SelectionInteractionSourceTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExternalInteractionsPairByIdentity_WithoutChangingSelection(bool radio)
    {
        using var clock = new MotionTestClockScope();
        var control = Create(radio); var source = new MaterialInteractionSource(); SetSource(control, source);
        var clicks = 0; control.Click += (_, _) => clicks++;
        var window = DialogTests.Show(control);
        try
        {
            var controller = MaterialInteraction.GetOrCreate(control);
            var first = new MaterialInteractionEvent.Press(new Point(24, 24));
            var second = new MaterialInteractionEvent.Press(new Point(24, 24));
            source.Emit(first); source.Emit(second); Advance(clock, 3);
            Assert.Equal(2, controller.RippleCount);
            source.Emit(new MaterialInteractionEvent.Release(new(new Point(24, 24))));
            Assert.True(controller.Interaction.HasFlag(InteractionState.Pressed));
            source.Emit(new MaterialInteractionEvent.Release(first));
            Assert.True(controller.Interaction.HasFlag(InteractionState.Pressed));
            source.Emit(new MaterialInteractionEvent.CancelPress(second));
            Assert.False(controller.Interaction.HasFlag(InteractionState.Pressed));
            Advance(clock, 50); Assert.Equal(0, controller.RippleCount);
            Assert.False(control.IsChecked); Assert.Equal(0, clicks);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacementAndRemountObserveFutureEventsOnly(bool radio)
    {
        using var clock = new MotionTestClockScope();
        var control = Create(radio); var first = new MaterialInteractionSource(); SetSource(control, first);
        var window = DialogTests.Show(control);
        try
        {
            var controller = MaterialInteraction.GetOrCreate(control);
            first.Emit(new MaterialInteractionEvent.HoverEnter()); Advance(clock, 12);
            Assert.True(controller.StateLayerOpacity > 0);
            var replacement = new MaterialInteractionSource();
            replacement.Emit(new MaterialInteractionEvent.Press(new Point(24, 24)));
            SetSource(control, replacement); Advance(clock, 2);
            Assert.Equal(0, controller.RippleCount); Assert.Equal(InteractionState.None, controller.Interaction);
            first.Emit(new MaterialInteractionEvent.DragStart());
            Assert.Equal(InteractionState.None, controller.Interaction);
            replacement.Emit(new MaterialInteractionEvent.Press(new Point(24, 24))); Advance(clock, 2);
            Assert.Equal(1, controller.RippleCount);
            window.Content = null; Dispatcher.UIThread.RunJobs();
            replacement.Emit(new MaterialInteractionEvent.Press(new Point(24, 24)));
            Assert.Equal(0, clock.PendingFrameCount);
            window.Content = control; window.UpdateLayout(); Advance(clock, 2);
            Assert.Equal(0, controller.RippleCount);
            replacement.Emit(new MaterialInteractionEvent.Press(new Point(24, 24))); Advance(clock, 2);
            Assert.Equal(1, controller.RippleCount);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeInputEmitsPairedInteractionsAndStillSelects(bool radio)
    {
        using var clock = new MotionTestClockScope();
        var control = Create(radio); var source = new MaterialInteractionSource(); SetSource(control, source);
        var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var window = DialogTests.Show(control);
        try
        {
            var center = control.TranslatePoint(new Rect(control.Bounds.Size).Center, window)!.Value;
            window.MouseMove(center); window.MouseDown(center, MouseButton.Left); Advance(clock, 3);
            var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
            window.MouseUp(center, MouseButton.Left); Advance(clock, 3);
            Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.Release>()).Start);
            Assert.True(control.IsChecked);
            events.Clear(); window.MouseDown(center, MouseButton.Left); Advance(clock, 2);
            press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
            window.Content = null; Dispatcher.UIThread.RunJobs();
            Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
        }
        finally { window.Close(); }
    }

    private static ToggleButton Create(bool radio) => radio ? new RadioButton() : new CheckBox();
    private static void SetSource(ToggleButton control, MaterialInteractionSource? source)
    {
        if (control is RadioButton radio) RadioButtonAppearance.SetInteractionSource(radio, source);
        else CheckBoxAppearance.SetInteractionSource((CheckBox)control, source);
    }
    private static void Advance(MotionTestClockScope clock, int frames)
    {
        for (var i = 0; i < frames; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
    }
}
