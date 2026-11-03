using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class MenuInteractionSourceTests
{
    [AvaloniaTheory]
    [InlineData("baseline")]
    [InlineData("expressive")]
    [InlineData("checkable")]
    [InlineData("selectable")]
    public void TemplateReplacementDoesNotReplayAnOldPress(string kind)
    {
        using var clock = new MotionTestClockScope();
        var control = Create(kind); var source = new MaterialInteractionSource(); control.InteractionSource = source;
        var window = DialogTests.Show(control);
        if (kind == "expressive")
        {
            control.Theme = (Avalonia.Styling.ControlTheme)window.FindResource("MaterialExpressiveDropdownMenuItemTheme")!;
            window.UpdateLayout();
        }
        try
        {
            var controller = MaterialInteraction.GetOrCreate(control);
            var press = new MaterialInteractionEvent.Press(new Point(24, 24)); source.Emit(press); Advance(clock, 3);
            Assert.Equal(1, controller.RippleCount);
            var template = control.Template; control.Template = null; window.UpdateLayout();
            control.Template = template; window.UpdateLayout(); Advance(clock, 3);
            Assert.Equal(0, controller.RippleCount);
            source.Emit(new MaterialInteractionEvent.Release(press)); Advance(clock, 3);
            source.Emit(new MaterialInteractionEvent.Press(new Point(24, 24))); Advance(clock, 3);
            Assert.Equal(1, controller.RippleCount);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void GroupHoverPairsByIdentityAndSourceReplacementRetainsRememberedState()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var group = new DropdownMenuGroup { InteractionSource = source, Width = 180,
            Items = { new Border { Height = 48 } } };
        var window = DialogTests.Show(group);
        try
        {
            var surface = group.GetVisualDescendants().OfType<MaterialSurface>().First();
            var first = new MaterialInteractionEvent.HoverEnter(); var second = new MaterialInteractionEvent.HoverEnter();
            source.Emit(first); source.Emit(second); Advance(clock, 60);
            source.Emit(new MaterialInteractionEvent.HoverExit(first)); Advance(clock, 60);
            Assert.Equal(new CornerRadius(16), surface.EffectiveCornerRadius);
            source.Emit(new MaterialInteractionEvent.HoverExit(new())); Advance(clock, 60);
            Assert.Equal(new CornerRadius(16), surface.EffectiveCornerRadius);
            var replacement = new MaterialInteractionSource();
            group.InteractionSource = replacement; Advance(clock, 60);
            source.Emit(new MaterialInteractionEvent.HoverExit(second)); Advance(clock, 60);
            Assert.Equal(new CornerRadius(16), surface.EffectiveCornerRadius);
            // Every event publishes the new collector's list, even a non-hover event.
            replacement.Emit(new MaterialInteractionEvent.DragStart()); Advance(clock, 60);
            Assert.Equal(new CornerRadius(8), surface.EffectiveCornerRadius);
            replacement.Emit(first); Advance(clock, 3);
            window.Content = null; Dispatcher.UIThread.RunJobs();
            replacement.Emit(second);
            Assert.Equal(0, clock.PendingFrameCount);
            window.Content = group; window.UpdateLayout(); Advance(clock, 60);
            surface = group.GetVisualDescendants().OfType<MaterialSurface>().First();
            Assert.Equal(new CornerRadius(16), surface.EffectiveCornerRadius);
            replacement.Emit(new MaterialInteractionEvent.HoverExit(second)); Advance(clock, 60);
            Assert.Equal(new CornerRadius(16), surface.EffectiveCornerRadius);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void GroupNativeHoverEmitsExitToOldSourceOnReplacementAndDetach()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var group = new DropdownMenuGroup { InteractionSource = source, Width = 180, Items = { new Border { Height = 48 } } };
        var window = DialogTests.Show(group);
        try
        {
            var center = group.TranslatePoint(new Rect(group.Bounds.Size).Center, window)!.Value;
            window.MouseMove(center); Advance(clock, 2);
            var enter = Assert.Single(events.OfType<MaterialInteractionEvent.HoverEnter>());
            group.InteractionSource = new();
            Assert.Same(enter, Assert.Single(events.OfType<MaterialInteractionEvent.HoverExit>()).Start);
            events.Clear(); group.InteractionSource.Interaction += events.Add;
            window.MouseMove(new Point(500, 280)); window.MouseMove(center); Advance(clock, 2);
            enter = Assert.Single(events.OfType<MaterialInteractionEvent.HoverEnter>());
            window.Content = null; Dispatcher.UIThread.RunJobs();
            Assert.Same(enter, Assert.Single(events.OfType<MaterialInteractionEvent.HoverExit>()).Start);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("baseline")]
    [InlineData("expressive")]
    [InlineData("checkable")]
    [InlineData("selectable")]
    public void ExternalInteractionsPairByIdentity_WithoutChangingSelection(string kind)
    {
        using var clock = new MotionTestClockScope();
        var control = Create(kind); var source = new MaterialInteractionSource(); SetSource(control, source);
        var clicks = 0; control.Click += (_, _) => clicks++;
        var window = DialogTests.Show(control);
        if (kind == "expressive")
        {
            control.Theme = (Avalonia.Styling.ControlTheme)window.FindResource("MaterialExpressiveDropdownMenuItemTheme")!;
            window.UpdateLayout();
        }
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
            Assert.False(control.Selected); Assert.Equal(0, clicks);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData("baseline")]
    [InlineData("expressive")]
    [InlineData("checkable")]
    [InlineData("selectable")]
    public void ReplacementAndRemountObserveFutureEventsOnly(string kind)
    {
        using var clock = new MotionTestClockScope();
        var control = Create(kind); var first = new MaterialInteractionSource(); SetSource(control, first);
        var window = DialogTests.Show(control);
        if (kind == "expressive")
        {
            control.Theme = (Avalonia.Styling.ControlTheme)window.FindResource("MaterialExpressiveDropdownMenuItemTheme")!;
            window.UpdateLayout();
        }
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
    [InlineData("baseline")]
    [InlineData("expressive")]
    [InlineData("checkable")]
    [InlineData("selectable")]
    public void NativeInputEmitsPairedInteractionsWithoutImplicitSelection(string kind)
    {
        using var clock = new MotionTestClockScope();
        var control = Create(kind); var source = new MaterialInteractionSource(); SetSource(control, source);
        var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var clicks = 0; control.Click += (_, _) => clicks++;
        bool? requested = null;
        if (control is CheckableDropdownMenuItem checkable) checkable.CheckedChangeRequested += (_, e) => requested = e.Value;
        var window = DialogTests.Show(control);
        if (kind == "expressive")
        {
            control.Theme = (Avalonia.Styling.ControlTheme)window.FindResource("MaterialExpressiveDropdownMenuItemTheme")!;
            window.UpdateLayout();
        }
        try
        {
            var center = control.TranslatePoint(new Rect(control.Bounds.Size).Center, window)!.Value;
            window.MouseMove(center); window.MouseDown(center, MouseButton.Left); Advance(clock, 3);
            var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
            window.MouseUp(center, MouseButton.Left); Advance(clock, 3);
            Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.Release>()).Start);
            Assert.False(control.Selected); Assert.Equal(1, clicks);
            Assert.Equal(kind == "checkable" ? true : (bool?)null, requested);
            events.Clear(); window.MouseDown(center, MouseButton.Left); Advance(clock, 2);
            press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
            window.Content = null; Dispatcher.UIThread.RunJobs();
            Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
        }
        finally { window.Close(); }
    }

    private static DropdownMenuItem Create(string kind) => kind switch
    {
        "expressive" => new DropdownMenuItem { Shapes = MenuDefaults.ItemShape(0, 1) },
        "checkable" => new CheckableDropdownMenuItem(),
        "selectable" => new SelectableDropdownMenuItem(),
        _ => new DropdownMenuItem()
    };
    private static void SetSource(DropdownMenuItem control, MaterialInteractionSource? source) => control.InteractionSource = source;
    private static void Advance(MotionTestClockScope clock, int frames)
    {
        for (var i = 0; i < frames; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
    }
}
