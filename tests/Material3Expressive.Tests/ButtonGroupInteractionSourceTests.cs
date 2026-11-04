using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class ButtonGroupInteractionSourceTests
{
    [AvaloniaFact]
    public void RapidSourceChangesCommitOnlyTheLastSourceAndDoNotReplayEvents()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var skipped = new MaterialInteractionSource(); var final = new MaterialInteractionSource();
        var (group, item) = Create(source); var window = DialogTests.Show(group);
        try
        {
            source.Emit(new MaterialInteractionEvent.Press(default)); Advance(clock, window, 3);
            item.InteractionSource = skipped; skipped.Emit(new MaterialInteractionEvent.Press(default));
            item.InteractionSource = final; final.Emit(new MaterialInteractionEvent.Press(default));
            Advance(clock, window, 1); var committed = item.PressProgress;
            skipped.Emit(new MaterialInteractionEvent.Press(default)); Advance(clock, window, 10);
            Assert.Equal(committed, item.PressProgress);
            final.Emit(new MaterialInteractionEvent.Press(default)); Advance(clock, window, 50);
            Assert.Equal(1, item.PressProgress); Assert.Null(MotionTestClockScope.Error(item));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void DetachingBeforeSourceCommitCancelsThePendingCollector()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var replacement = new MaterialInteractionSource();
        var (group, item) = Create(source); var window = DialogTests.Show(group);
        try
        {
            source.Emit(new MaterialInteractionEvent.Press(default)); Advance(clock, window, 3);
            item.InteractionSource = replacement; window.Content = null; Flush(window);
            Assert.Equal(0, clock.PendingFrameCount);
            replacement.Emit(new MaterialInteractionEvent.Press(default));
            window.Content = group; Flush(window); Advance(clock, window, 5);
            Assert.Equal(0, item.PressProgress);
            replacement.Emit(new MaterialInteractionEvent.Press(default)); Advance(clock, window, 50);
            Assert.Equal(1, item.PressProgress); Assert.Null(MotionTestClockScope.Error(item));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiplePressesReleaseOnlyAfterTheLastMatchingEnd(bool repeatSamePress)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var (group, item) = Create(source);
        var window = DialogTests.Show(group);
        try
        {
            var first = new MaterialInteractionEvent.Press(default);
            // Upstream keeps an ordered list, including duplicate event objects. A matching
            // release removes only one entry; replacing the list with a set loses a press.
            var second = repeatSamePress ? first : new MaterialInteractionEvent.Press(default);
            source.Emit(first); source.Emit(second);
            Advance(clock, window, 50);
            Assert.Equal(1, item.PressProgress);
            Assert.Equal(120, item.Bounds.Width);
            source.Emit(new MaterialInteractionEvent.Release(first));
            source.Emit(new MaterialInteractionEvent.CancelPress(new(default)));
            Advance(clock, window, 50);
            Assert.Equal(1, item.PressProgress);
            source.Emit(new MaterialInteractionEvent.CancelPress(second));
            Advance(clock, window, 60);
            Assert.Equal(0, item.PressProgress);
            Assert.Equal(80, item.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void QuickReleaseWaitsForExpansionAndRepressInterruptsReturn()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var (group, item) = Create(source);
        var window = DialogTests.Show(group);
        try
        {
            var press = new MaterialInteractionEvent.Press(default);
            source.Emit(press); source.Emit(new MaterialInteractionEvent.Release(press));
            Advance(clock, window, 4);
            Assert.InRange(item.PressProgress, .01f, .75f);
            var peak = item.PressProgress;
            for (var i = 0; i < 60; i++)
            {
                Advance(clock, window, 1);
                if (item.PressProgress < peak) break;
                peak = item.PressProgress;
            }
            Assert.True(peak > .75f);
            Assert.True(item.PressProgress < peak);
            source.Emit(press);
            Advance(clock, window, 50);
            Assert.Equal(1, item.PressProgress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SourceReplacementKeepsProgressButDoesNotReplayOldPresses()
    {
        using var clock = new MotionTestClockScope();
        var old = new MaterialInteractionSource();
        var next = new MaterialInteractionSource();
        var (group, item) = Create(old);
        var window = DialogTests.Show(group);
        try
        {
            var press = new MaterialInteractionEvent.Press(default);
            old.Emit(press); Advance(clock, window, 5);
            var beforeCommit = item.PressProgress;
            next.Emit(press); // Starts before collection must not be replayed.
            item.InteractionSource = next;
            // The old animation samples the composition frame before EnlargeOnPressElement
            // replaces its collector. This is a frame boundary, not a fixed delay.
            Advance(clock, window, 1);
            var value = item.PressProgress;
            Assert.True(value > beforeCommit);
            old.Emit(new MaterialInteractionEvent.Release(press));
            Advance(clock, window, 20);
            Assert.Equal(value, item.PressProgress);
            next.Emit(new MaterialInteractionEvent.HoverEnter());
            // Even a first non-press event emits false upstream. The <= .75 branch waits 1 second.
            Advance(clock, window, 110);
            Assert.Equal(0, item.PressProgress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SharedNativeSourceDrivesFeedbackAndWidthWithoutActivatingFromExternalEvents()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var (group, item) = Create(source);
        var button = new Button { Content = "Run", HorizontalAlignment = HorizontalAlignment.Stretch };
        MaterialButton.SetInteractionSource(button, source);
        item.Content = button;
        var clicks = 0; button.Click += (_, _) => clicks++;
        var window = DialogTests.Show(group);
        try
        {
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left);
            Advance(clock, window, 50);
            Assert.Equal(1, item.PressProgress);
            button.IsEnabled = false; Advance(clock, window, 60);
            Assert.Equal(0, item.PressProgress);
            window.MouseUp(point, MouseButton.Left);
            source.Emit(new MaterialInteractionEvent.Press(default));
            Advance(clock, window, 50);
            Assert.Equal(1, item.PressProgress); // External source remains observable when content is disabled.
            Assert.Equal(0, clicks);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExplicitSourceWinsAndClearingItRestoresPressSource()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var (group, item) = Create(source);
        var button = new Button { Content = "Run", HorizontalAlignment = HorizontalAlignment.Stretch };
        item.Content = button; item.PressSource = button;
        var window = DialogTests.Show(group);
        try
        {
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left);
            Advance(clock, window, 20);
            Assert.Equal(0, item.PressProgress);
            item.InteractionSource = null;
            Advance(clock, window, 50);
            Assert.Equal(1, item.PressProgress);
            window.MouseUp(point, MouseButton.Left); Advance(clock, window, 60);
            Assert.Equal(0, item.PressProgress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DetachEndsCollectionAndReattachStartsFresh()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var (group, item) = Create(source);
        var window = DialogTests.Show(group);
        try
        {
            var press = new MaterialInteractionEvent.Press(default);
            source.Emit(press); Advance(clock, window, 5);
            window.Content = null; Flush(window);
            source.Emit(press); Advance(clock, window, 5);
            Assert.Equal(0, item.PressProgress);
            window.Content = group; Flush(window);
            Advance(clock, window, 5); Assert.Equal(0, item.PressProgress);
            source.Emit(press); Advance(clock, window, 50); Assert.Equal(1, item.PressProgress);
        }
        finally { window.Close(); }
    }

    private static (ButtonGroup Group, ButtonGroupCustomItem Item) Create(MaterialInteractionSource source)
    {
        var group = new ButtonGroup { ExpandedRatio = .5 };
        var items = Enumerable.Range(0, 3).Select(_ => new ButtonGroupCustomItem
        {
            Content = new Border { MinWidth = 80, Height = 40 },
            InteractionSource = new MaterialInteractionSource(), CompressionLimit = 24
        }).ToArray();
        items[1].InteractionSource = source;
        foreach (var item in items) group.Items.Add(item);
        return (group, items[1]);
    }

    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    {
        for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
