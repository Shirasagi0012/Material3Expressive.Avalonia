using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class FabInteractionSourceTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConfigurationSnapPublishesAfterTheEffectButNewInteractionsUseItImmediately(bool extended)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var fab = Create(extended, source);
        var window = DialogTests.Show(new Grid { Children = { fab } });
        var surface = fab.GetVisualDescendants().OfType<MaterialSurface>().Single();
        clock.SendFrame(TimeSpan.Zero); Flush(window);
        var press = new MaterialInteractionEvent.Press(new(24, 20));
        source.Emit(press);
        for (var ms = 16; ms <= 64; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
        fab.Elevation = new(2, 20, 6, 9);
        clock.SendFrame(TimeSpan.FromMilliseconds(80)); Flush(window);
        // The 80ms composition reads the old running value before updateElevation snaps.
        Assert.InRange(surface.ShadowElevation, 2, 4);
        // A following interaction sees the internal snap immediately, even though its
        // display has not yet been committed. Deferring the snap itself would fail here.
        source.Emit(new MaterialInteractionEvent.CancelPress(press));
        clock.SendFrame(TimeSpan.FromMilliseconds(96)); Flush(window);
        Assert.Equal(20, surface.ShadowElevation);
        Advance(clock, window);
        Assert.Equal(2, surface.ShadowElevation);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void PendingConfigurationCoalescesAndDetachRestoresUnderlyingElevation()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var fab = Create(false, source);
        var root = new Grid { Children = { fab } }; var window = DialogTests.Show(root);
        var surface = fab.GetVisualDescendants().OfType<MaterialSurface>().Single();
        surface.ShadowElevation = 13;
        source.Emit(new MaterialInteractionEvent.Press(new(24, 20))); Advance(clock, window);
        Assert.Equal(4, surface.ShadowElevation);
        fab.Elevation = new(2, 30, 6, 9);
        fab.Elevation = new(2, 7, 6, 9);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        Assert.Equal(4, surface.ShadowElevation);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        Assert.Equal(7, surface.ShadowElevation);
        fab.Elevation = new(2, 40, 6, 9);
        root.Children.Remove(fab); Flush(window);
        Assert.Equal(13, surface.ShadowElevation);
        root.Children.Add(fab); Flush(window); Advance(clock, window);
        Assert.Equal(2, surface.ShadowElevation);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ChangingUnusedElevation_DoesNotFinishHover_AndNextPressUsesNewConfiguration()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var fab = Create(false, source);
        var window = DialogTests.Show(new Grid { Children = { fab } });
        var surface = fab.GetVisualDescendants().OfType<MaterialSurface>().Single();
        clock.SendFrame(TimeSpan.Zero); Flush(window);
        source.Emit(new MaterialInteractionEvent.HoverEnter());
        foreach (var time in new[] { 16, 32, 48, 64 }) { clock.SendFrame(TimeSpan.FromMilliseconds(time)); Flush(window); }
        var before = surface.ShadowElevation;
        fab.Elevation = new(2, 40, 60, 9);
        clock.SendFrame(TimeSpan.FromMilliseconds(80)); Flush(window);
        Assert.InRange(surface.ShadowElevation, before, 8.999);
        source.Emit(new MaterialInteractionEvent.Press(new(24, 20))); Advance(clock, window);
        Assert.Equal(40, surface.ShadowElevation);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void FocusedSourceReplacement_EndsOldFocusWithoutReplayingIt(bool ordinaryButton)
    {
        using var clock = new MotionTestClockScope();
        var old = new MaterialInteractionSource(); var next = new MaterialInteractionSource();
        var before = new List<MaterialInteractionEvent>(); var after = new List<MaterialInteractionEvent>();
        old.Interaction += before.Add; next.Interaction += after.Add;
        Button button = ordinaryButton ? new Button() : Create(false, old);
        void SetSource(MaterialInteractionSource value)
        {
            if (button is FloatingActionButton fab) fab.InteractionSource = value;
            else MaterialButton.SetInteractionSource(button, value);
        }
        SetSource(old);
        var other = new Button(); var window = DialogTests.Show(new StackPanel { Children = { button, other } });
        button.Focus(NavigationMethod.Tab); Flush(window);
        var focus = Assert.Single(before.OfType<MaterialInteractionEvent.Focus>());
        SetSource(next); Flush(window);
        Assert.True(button.IsFocused);
        if (button is FloatingActionButton)
        {
            Assert.Empty(before.OfType<MaterialInteractionEvent.Unfocus>());
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        }
        Assert.Same(focus, Assert.Single(before.OfType<MaterialInteractionEvent.Unfocus>()).Start);
        Assert.Empty(after);
        if (button is FloatingActionButton)
        {
            Advance(clock, window);
            Assert.Equal(2, button.GetVisualDescendants().OfType<MaterialSurface>().Single().ShadowElevation);
        }
        other.Focus(NavigationMethod.Tab); button.Focus(NavigationMethod.Tab); Flush(window);
        Assert.Single(after.OfType<MaterialInteractionEvent.Focus>());
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void HoverElevation_StartsOnFirstAnimationFrame_AndPressInterruptsCurrentValue()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var fab = Create(false, source);
        var window = DialogTests.Show(new Grid { Children = { fab } });
        var surface = fab.GetVisualDescendants().OfType<MaterialSurface>().Single();
        clock.SendFrame(TimeSpan.Zero); Flush(window);
        source.Emit(new MaterialInteractionEvent.HoverEnter());
        foreach (var time in new[] { 16, 32, 48, 64 }) { clock.SendFrame(TimeSpan.FromMilliseconds(time)); Flush(window); }
        // Fixed Core layer probe: 2 -> 9 DIP, 120ms FastOutSlowIn, 48ms elapsed at 64ms.
        Assert.InRange(surface.ShadowElevation, 6.2951, 6.2953);
        var interrupted = surface.ShadowElevation;
        source.Emit(new MaterialInteractionEvent.Press(new(24, 20)));
        Assert.Equal(interrupted, surface.ShadowElevation);
        clock.SendFrame(TimeSpan.FromMilliseconds(80)); Flush(window);
        // Running Animatable reuses its last frame time on interruption (Core: 6.1805644).
        Assert.InRange(surface.ShadowElevation, 6.1805, 6.1807);
        Advance(clock, window); Assert.Equal(4, surface.ShadowElevation);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverlappingEvents_PreserveIdentityElevationAndActivationOwnership(bool extended)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var fab = Create(extended, source);
        var window = DialogTests.Show(new Grid { Children = { fab } });
        var surface = fab.GetVisualDescendants().OfType<MaterialSurface>().Single();
        var controller = MaterialInteraction.GetOrCreate(fab); var clicks = 0; fab.Click += (_, _) => clicks++;
        var hover = new MaterialInteractionEvent.HoverEnter(); source.Emit(hover); Advance(clock, window);
        Assert.Equal(9, surface.ShadowElevation); Assert.Equal(2, surface.TonalElevation);
        var first = new MaterialInteractionEvent.Press(new(10, 12)); var second = new MaterialInteractionEvent.Press(new(20, 16));
        source.Emit(first); source.Emit(second); Assert.Equal(2, controller.RippleCount); Advance(clock, window);
        // A newer ripple finishes the previous wave; the elevation collector retains both presses.
        Assert.Equal(4, surface.ShadowElevation); Assert.Equal(1, controller.RippleCount); Assert.False(fab.IsPressed);
        source.Emit(new MaterialInteractionEvent.Release(first)); Advance(clock, window);
        Assert.Equal(4, surface.ShadowElevation); Assert.Equal(1, controller.RippleCount);
        source.Emit(new MaterialInteractionEvent.CancelPress(second)); Advance(clock, window);
        Assert.Equal(9, surface.ShadowElevation);
        source.Emit(new MaterialInteractionEvent.HoverExit(hover)); Advance(clock, window);
        Assert.Equal(2, surface.ShadowElevation); Assert.Equal(0, clicks);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Replacement_RecreatesElevationAndDoesNotReplayOldEvents(bool extended)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var fab = Create(extended, source);
        var root = new Grid { Children = { fab } }; var window = DialogTests.Show(root);
        var surface = fab.GetVisualDescendants().OfType<MaterialSurface>().Single();
        var hover = new MaterialInteractionEvent.HoverEnter(); source.Emit(hover); Advance(clock, window);
        Assert.Equal(9, surface.ShadowElevation);
        var next = new MaterialInteractionSource(); fab.InteractionSource = next; Flush(window);
        Assert.Equal(9, surface.ShadowElevation);
        next.Emit(new MaterialInteractionEvent.Press(new(16, 16)));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        Assert.Equal(2, surface.ShadowElevation); Assert.Equal(0, MaterialInteraction.GetOrCreate(fab).StateLayerOpacity);
        source.Emit(new MaterialInteractionEvent.Press(new(16, 16))); Advance(clock, window);
        Assert.Equal(2, surface.ShadowElevation);
        next.Emit(new MaterialInteractionEvent.Focus()); Advance(clock, window); Assert.Equal(6, surface.ShadowElevation);
        root.Children.Remove(fab); Flush(window);
        next.Emit(new MaterialInteractionEvent.Press(new(16, 16)));
        root.Children.Add(fab); Flush(window); Advance(clock, window);
        Assert.Equal(2, surface.ShadowElevation); Assert.Equal(0, MaterialInteraction.GetOrCreate(fab).RippleCount);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void NativeInput_PairsOldSourceCancellationAndResumesOnTheNewSource()
    {
        using var clock = new MotionTestClockScope();
        var old = new MaterialInteractionSource(); var next = new MaterialInteractionSource();
        var before = new List<MaterialInteractionEvent>(); var after = new List<MaterialInteractionEvent>();
        old.Interaction += before.Add; next.Interaction += after.Add;
        var fab = Create(false, old); var window = DialogTests.Show(new Grid { Children = { fab } });
        var pointer = fab.TranslatePoint(new Rect(fab.Bounds.Size).Center, window)!.Value;
        window.MouseMove(pointer); window.MouseDown(pointer, MouseButton.Left); Flush(window);
        var press = Assert.Single(before.OfType<MaterialInteractionEvent.Press>());
        fab.InteractionSource = next; Flush(window);
        Assert.Empty(before.OfType<MaterialInteractionEvent.CancelPress>());
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        Assert.Same(press, Assert.Single(before.OfType<MaterialInteractionEvent.CancelPress>()).Start);
        window.MouseUp(pointer, MouseButton.Left); Flush(window);
        Assert.Empty(after.OfType<MaterialInteractionEvent.Release>());
        window.MouseDown(pointer, MouseButton.Left); window.MouseUp(pointer, MouseButton.Left); Flush(window);
        Assert.Same(Assert.Single(after.OfType<MaterialInteractionEvent.Press>()), Assert.Single(after.OfType<MaterialInteractionEvent.Release>()).Start);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ReplacementCoalescesAndCanBeCanceledBeforeCommitOrDetach()
    {
        using var clock = new MotionTestClockScope();
        var first = new MaterialInteractionSource(); var skipped = new MaterialInteractionSource();
        var latest = new MaterialInteractionSource(); var fab = Create(false, first);
        var root = new Grid { Children = { fab } }; var window = DialogTests.Show(root);
        var surface = fab.GetVisualDescendants().OfType<MaterialSurface>().Single();
        first.Emit(new MaterialInteractionEvent.HoverEnter()); Advance(clock, window);
        fab.InteractionSource = skipped;
        fab.InteractionSource = first;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        Assert.Equal(9, surface.ShadowElevation);
        fab.InteractionSource = skipped;
        fab.InteractionSource = latest;
        skipped.Emit(new MaterialInteractionEvent.Focus());
        latest.Emit(new MaterialInteractionEvent.Focus());
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        Assert.Equal(2, surface.ShadowElevation);
        skipped.Emit(new MaterialInteractionEvent.HoverEnter()); Advance(clock, window);
        Assert.Equal(2, surface.ShadowElevation);
        latest.Emit(new MaterialInteractionEvent.Focus()); Advance(clock, window);
        Assert.Equal(6, surface.ShadowElevation);
        fab.InteractionSource = first;
        root.Children.Remove(fab); Flush(window);
        Advance(clock, window);
        Assert.Equal(0, clock.PendingFrameCount);
        root.Children.Add(fab); Flush(window); Advance(clock, window);
        Assert.Equal(2, surface.ShadowElevation);
        first.Emit(new MaterialInteractionEvent.Press(new(16, 16))); Advance(clock, window);
        Assert.Equal(4, surface.ShadowElevation);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    static FloatingActionButton Create(bool extended, MaterialInteractionSource source)
    {
        FloatingActionButton fab = extended ? new ExtendedFloatingActionButton { Content = new Border { Width = 48, Height = 16 } } : new();
        fab.Icon = new Border { Width = 24, Height = 24 }; fab.InteractionSource = source;
        fab.Elevation = new(2, 4, 6, 9); return fab;
    }
    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    static void Advance(MotionTestClockScope clock, Window window)
    { for (var i = 0; i < 40; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
}
