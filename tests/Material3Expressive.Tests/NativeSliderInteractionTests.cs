using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class NativeSliderInteractionTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void TrackClickPublishesPairedPressAndShowsLabel(bool cancel)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var log = new List<MaterialInteractionEvent>(); source.Interaction += log.Add;
        var slider = new Slider { Width = 280, Value = 20 }; SliderAppearance.SetInteractionSource(slider, source);
        var label = new TooltipLabel { Content = slider, InteractionSource = source, Label = new PlainTooltip { Content = "value" } };
        var window = DialogTests.Show(label);
        try
        {
            var point = slider.TranslatePoint(new(220, slider.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left); Flush(window);
            Assert.True(label.IsPresented);
            // Native Slider may transfer a track press directly into Thumb dragging.
            // Both paths must start real feedback, not just pass an empty pairing loop.
            Assert.Contains(log, e => e is MaterialInteractionEvent.Press or MaterialInteractionEvent.DragStart);
            if (cancel) slider.IsEnabled = false;
            window.MouseUp(point, MouseButton.Left); window.MouseMove(new(1, 1)); Flush(window);
            Assert.False(label.IsPresented);
            foreach (var start in log.OfType<MaterialInteractionEvent.Press>())
                Assert.Single(log, e => e is MaterialInteractionEvent.Release r && r.Start == start || e is MaterialInteractionEvent.CancelPress c && c.Start == start);
            foreach (var start in log.OfType<MaterialInteractionEvent.DragStart>())
                Assert.Single(log, e => e is MaterialInteractionEvent.DragStop s && s.Start == start || e is MaterialInteractionEvent.DragCancel c && c.Start == start);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void ExternalInteractionsDriveThumbAndLabelWithoutChangingValue(bool vertical)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var slider = new Slider { Width = vertical ? 48 : 280, Height = vertical ? 280 : 48, Value = 40,
            Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal };
        SliderAppearance.SetInteractionSource(slider, source);
        var label = new TooltipLabel { Content = slider, InteractionSource = source, Label = new PlainTooltip { Content = "volume" } };
        var window = DialogTests.Show(label);
        try
        {
            var thumb = Assert.Single(slider.GetVisualDescendants().OfType<SliderThumb>());
            var size = slider.Bounds.Size;
            var press = new MaterialInteractionEvent.Press(default); var drag = new MaterialInteractionEvent.DragStart();
            source.Emit(press); source.Emit(drag); Flush(window);
            Assert.Equal(InteractionState.Pressed | InteractionState.Dragged, thumb.Interaction); Assert.True(label.IsPresented);
            source.Emit(new MaterialInteractionEvent.Release(press)); Flush(window);
            Assert.Equal(InteractionState.Dragged, thumb.Interaction); Assert.True(label.IsPresented);
            source.Emit(new MaterialInteractionEvent.DragCancel(drag)); Flush(window);
            Assert.Equal(InteractionState.None, thumb.Interaction); Assert.False(label.IsPresented);
            Assert.Equal(40, slider.Value); Assert.Equal(size, slider.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExternalFocusUsesSharedRingAndTrackGapWhileNativeFocusStaysUnchanged()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var slider = new Slider { Width = 280, Value = 40 }; SliderAppearance.SetInteractionSource(slider, source);
        MaterialInteraction.SetFocusFeedback(slider, FocusFeedback.InsetRing);
        var window = DialogTests.Show(slider);
        try
        {
            var slot = Assert.Single(slider.GetVisualDescendants().OfType<SliderThumbPresenter>());
            var focus = new MaterialInteractionEvent.Focus(); source.Emit(focus);
            for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
            Assert.Equal(4, slot.FocusPadding); Assert.True(slider.GetValue(SliderAppearance.IsSourceFocusedProperty));
            Assert.InRange(slot.Ring.Progress, .99f, 1.01f); Assert.False(slider.IsFocused);
            source.Emit(new MaterialInteractionEvent.Unfocus(focus));
            for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
            Assert.Equal(0, slot.FocusPadding); Assert.Equal(0, slot.Ring.Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("release")] [InlineData("replace")] [InlineData("disable")] [InlineData("detach")]
    public void NativeDragPublishesOnePairedLifetime(string ending)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var log = new List<MaterialInteractionEvent>(); source.Interaction += log.Add;
        var slider = new Slider { Width = 280, Value = 40 }; SliderAppearance.SetInteractionSource(slider, source);
        var window = DialogTests.Show(slider);
        try
        {
            var thumb = Assert.Single(slider.GetVisualDescendants().OfType<Thumb>());
            var point = thumb.TranslatePoint(new(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.MouseMove(point + new Vector(25, 0)); Flush(window);
            var drag = Assert.Single(log.OfType<MaterialInteractionEvent.DragStart>());
            var next = new MaterialInteractionSource(); var nextLog = new List<MaterialInteractionEvent>(); next.Interaction += nextLog.Add;
            if (ending == "replace") SliderAppearance.SetInteractionSource(slider, next);
            if (ending == "disable") slider.IsEnabled = false;
            if (ending == "detach") window.Content = null;
            window.MouseUp(point + new Vector(25, 0), MouseButton.Left); Flush(window);
            var end = Assert.Single(log, e => e is MaterialInteractionEvent.DragStop or MaterialInteractionEvent.DragCancel);
            if (ending == "release") Assert.Same(drag, Assert.IsType<MaterialInteractionEvent.DragStop>(end).Start);
            else Assert.Same(drag, Assert.IsType<MaterialInteractionEvent.DragCancel>(end).Start);
            Assert.Empty(nextLog.OfType<MaterialInteractionEvent.DragStop>());
            Assert.Empty(nextLog.OfType<MaterialInteractionEvent.DragCancel>());
            foreach (var press in log.OfType<MaterialInteractionEvent.Press>())
                Assert.Single(log, e => e is MaterialInteractionEvent.Release r && r.Start == press || e is MaterialInteractionEvent.CancelPress c && c.Start == press);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ReplacingFocusedSourceEndsNativeFocusOnTheOldSourceAndDoesNotReplayIt()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var log = new List<MaterialInteractionEvent>(); source.Interaction += log.Add;
        var slider = new Slider { Width = 280 }; SliderAppearance.SetInteractionSource(slider, source);
        var window = DialogTests.Show(slider);
        try
        {
            slider.Focus(NavigationMethod.Tab); Flush(window);
            var focus = Assert.Single(log.OfType<MaterialInteractionEvent.Focus>());
            var next = new MaterialInteractionSource(); var nextLog = new List<MaterialInteractionEvent>(); next.Interaction += nextLog.Add;
            SliderAppearance.SetInteractionSource(slider, next); Flush(window);
            Assert.Same(focus, Assert.Single(log.OfType<MaterialInteractionEvent.Unfocus>()).Start); Assert.Empty(nextLog);
            Assert.True(slider.IsFocused); Assert.False(slider.GetValue(SliderAppearance.IsSourceFocusedProperty));
            window.FocusManager!.Focus(null); slider.Focus(NavigationMethod.Tab); Flush(window);
            Assert.Single(nextLog.OfType<MaterialInteractionEvent.Focus>());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DefaultThumbPublishesOnlyOneHoverAndTemplateReplacementEndsIt()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var log = new List<MaterialInteractionEvent>(); source.Interaction += log.Add;
        var slider = new Slider { Width = 280, Value = 40 }; SliderAppearance.SetInteractionSource(slider, source);
        var window = DialogTests.Show(slider);
        try
        {
            var thumb = Assert.Single(slider.GetVisualDescendants().OfType<Thumb>());
            var point = thumb.TranslatePoint(new(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point); Flush(window);
            var hover = Assert.Single(log.OfType<MaterialInteractionEvent.HoverEnter>());
            slider.Template = null; Flush(window);
            Assert.Same(hover, Assert.Single(log.OfType<MaterialInteractionEvent.HoverExit>()).Start);
            source.Emit(new MaterialInteractionEvent.Focus()); Flush(window);
            Assert.Equal(0, clock.PendingFrameCount);
        }
        finally { window.Close(); }
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
