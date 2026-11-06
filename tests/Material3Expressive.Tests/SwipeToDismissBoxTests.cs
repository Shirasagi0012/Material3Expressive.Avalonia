using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class SwipeToDismissBoxTests
{
    [AvaloniaTheory]
    [InlineData(SwipeToDismissBoxValue.StartToEnd, 5)]
    [InlineData(SwipeToDismissBoxValue.StartToEnd, 15)]
    [InlineData(SwipeToDismissBoxValue.EndToStart, 5)]
    [InlineData(SwipeToDismissBoxValue.EndToStart, 15)]
    public void RemovingActiveTarget_CompletesAtFallbackWithoutDismissal(SwipeToDismissBoxValue direction, int frames)
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var window = DialogTests.Show(box);
        var dismissals = new List<SwipeToDismissBoxValue>();
        box.Dismissed += (_, e) => dismissals.Add(e.Direction);
        try
        {
            var operation = box.State.DismissAsync(direction);
            Advance(clock, window, frames);
            Assert.NotEqual(0, box.State.RequireOffset());
            if (direction == SwipeToDismissBoxValue.StartToEnd) box.EnableDismissFromStartToEnd = false;
            else box.EnableDismissFromEndToStart = false;
            window.UpdateLayout();
            Advance(clock, window, 25);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Equal(0, box.State.RequireOffset());
            Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.CurrentValue);
            Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.SettledValue);
            Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.TargetValue);
            Assert.Empty(dismissals);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);

            // The removed direction can subsequently be enabled and used normally.
            box.EnableDismissFromStartToEnd = box.EnableDismissFromEndToStart = true;
            window.UpdateLayout();
            var next = box.State.DismissAsync(direction);
            Advance(clock, window, 25);
            Assert.True(next.IsCompletedSuccessfully);
            Assert.Equal(direction, box.State.SettledValue);
            Assert.Equal([direction], dismissals);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void DisablingAnAncestorCancelsActiveDragAndReleasesCapture()
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var parent = new Border { Child = box }; var window = DialogTests.Show(parent);
        try
        {
            var origin = box.TranslatePoint(new Point(20, 20), window)!.Value;
            window.MouseDown(origin, MouseButton.Left); window.MouseMove(origin + new Vector(80, 0));
            Assert.InRange(box.State.CurrentOffset, 79, 81);
            parent.IsEnabled = false; Advance(clock, window, 25);
            Assert.Equal(0, box.State.CurrentOffset);
            window.MouseUp(origin + new Vector(80, 0), MouseButton.Left);
            parent.IsEnabled = true;
            window.MouseDown(origin, MouseButton.Left); window.MouseMove(origin + new Vector(170, 0));
            window.MouseUp(origin + new Vector(170, 0), MouseButton.Left); Advance(clock, window, 25);
            Assert.Equal(SwipeToDismissBoxValue.StartToEnd, box.State.SettledValue);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void DismissedHandlerCanRemoveTheItemWithoutLeavingAnimationWork()
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var window = DialogTests.Show(box); var count = 0;
        box.Dismissed += (_, e) =>
        {
            Assert.Equal(SwipeToDismissBoxValue.StartToEnd, e.Direction);
            count++; window.Content = null;
        };
        try
        {
            var task = box.State.DismissAsync(SwipeToDismissBoxValue.StartToEnd);
            Advance(clock, window, 25);
            Assert.True(task.IsCompletedSuccessfully); Assert.Equal(1, count); Assert.Null(window.Content);
            Assert.Equal(0, clock.PendingFrameCount); Assert.Null(window.MotionRoot.Diagnostics.LastError);
            Advance(clock, window, 5); Assert.Equal(1, count);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BackgroundSlotOwnsItsLogicalChildAndReleasesReplacedContent()
    {
        using var clock = new MotionTestClockScope();
        var context = new object(); var background = new Border();
        var box = Create(); box.DataContext = context; box.BackgroundContent = background;
        Assert.Same(box, background.GetLogicalParent()); Assert.Same(context, background.DataContext);
        var window = DialogTests.Show(box);
        try
        {
            Assert.Same(box, background.GetLogicalParent());
            var replacement = new Border(); box.BackgroundContent = replacement; window.UpdateLayout();
            Assert.Null(background.GetLogicalParent()); Assert.Null(background.DataContext);
            Assert.Same(box, replacement.GetLogicalParent()); Assert.Same(context, replacement.DataContext);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void UnmeasuredState_PreservesInitialValueAndRequiresLayoutForOffset()
    {
        var state = new SwipeToDismissBoxState(SwipeToDismissBoxValue.EndToStart);
        Assert.Equal(SwipeToDismissBoxValue.EndToStart, state.CurrentValue);
        Assert.Equal(SwipeToDismissBoxValue.EndToStart, state.SettledValue);
        Assert.Equal(SwipeToDismissBoxValue.Settled, state.DismissDirection);
        Assert.Equal(1, state.Progress);
        Assert.Throws<InvalidOperationException>(() => state.RequireOffset());
        state.SnapToAsync(SwipeToDismissBoxValue.StartToEnd).GetAwaiter().GetResult();
        Assert.Equal(SwipeToDismissBoxValue.StartToEnd, state.CurrentValue);
        Assert.True(double.IsNaN(state.CurrentOffset));
        Assert.NotSame(new SwipeToDismissBox().State, new SwipeToDismissBox().State);
    }

    [AvaloniaTheory]
    [InlineData(false, SwipeToDismissBoxValue.Settled, 0)]
    [InlineData(false, SwipeToDismissBoxValue.StartToEnd, 240)]
    [InlineData(false, SwipeToDismissBoxValue.EndToStart, -240)]
    [InlineData(true, SwipeToDismissBoxValue.StartToEnd, -240)]
    [InlineData(true, SwipeToDismissBoxValue.EndToStart, 240)]
    public void Layout_MovesOnlyForegroundAndMirrorsReadingDirection(bool rtl, SwipeToDismissBoxValue initial, double physicalOffset)
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); box.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        box.State = new(initial);
        var window = DialogTests.Show(box);
        try
        {
            var foreground = (Control)box.Content!;
            var background = (Control)box.BackgroundContent!;
            Assert.Equal(physicalOffset, foreground.TranslatePoint(default, window)!.Value.X - background.TranslatePoint(default, window)!.Value.X);
            Assert.Equal(new Size(240, 64), background.Bounds.Size);
            Assert.False(box.ClipToBounds);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BackgroundDoesNotDetermineNaturalSize_AndTemplatesCreateBothSlots()
    {
        using var clock = new MotionTestClockScope();
        var box = new SwipeToDismissBox
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Content = "foreground", ContentTemplate = new FuncDataTemplate<string>((_, _) => new Border { Width = 80, Height = 36 }),
            BackgroundContent = "background", BackgroundContentTemplate = new FuncDataTemplate<string>((_, _) => new Border { MinWidth = 600, MinHeight = 200 })
        };
        var window = DialogTests.Show(box);
        try { Assert.Equal(new Size(80, 36), box.Bounds.Size); }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, 32)]
    [InlineData(true, -31.75)]
    public void HalfPhysicalPixelOffsetsReverseBeforeRounding(bool rtl, double expected)
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); box.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var window = DialogTests.Show(box); window.SetRenderScaling(4); window.UpdateLayout();
        try
        {
            box.State.BeginDrag(); box.State.DragBy(127.5f); window.UpdateLayout();
            var foreground = (Control)box.Content!; var background = (Control)box.BackgroundContent!;
            Assert.Equal(31.875, box.State.CurrentOffset);
            Assert.Equal(expected, foreground.TranslatePoint(default, window)!.Value.X - background.TranslatePoint(default, window)!.Value.X);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(70, 0, SwipeToDismissBoxValue.Settled)]
    [InlineData(130, 0, SwipeToDismissBoxValue.StartToEnd)]
    [InlineData(55, 50, SwipeToDismissBoxValue.Settled)]
    [InlineData(56, 50, SwipeToDismissBoxValue.StartToEnd)]
    [InlineData(10, 125, SwipeToDismissBoxValue.StartToEnd)]
    [InlineData(-55, -50, SwipeToDismissBoxValue.Settled)]
    [InlineData(-56, -50, SwipeToDismissBoxValue.EndToStart)]
    [InlineData(-10, -125, SwipeToDismissBoxValue.EndToStart)]
    public void Release_UsesZeroVelocityNearestAnchorOrDirectionalThreshold(double distance, float velocity, SwipeToDismissBoxValue expected)
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var window = DialogTests.Show(box);
        try
        {
            box.State.BeginDrag(); box.State.DragBy((float)distance);
            Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.SettledValue);
            var operation = box.State.EndDrag(velocity);
            Advance(clock, window, 25);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Equal(expected, box.State.SettledValue);
            Assert.Equal(expected, box.State.CurrentValue);
            Assert.Equal(expected, box.State.TargetValue);
            Assert.Equal(1, box.State.Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CurrentAnchorCanChangeDuringDrag_WithoutReportingDismissal()
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var count = 0; box.Dismissed += (_, _) => count++;
        var window = DialogTests.Show(box);
        try
        {
            box.State.BeginDrag(); box.State.DragBy(150);
            Assert.Equal(SwipeToDismissBoxValue.StartToEnd, box.State.CurrentValue);
            Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.SettledValue);
            Assert.Equal(.625, box.State.Progress);
            Advance(clock, window, 2); Assert.Equal(0, count);
            box.State.DragBy(-120);
            Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.CurrentValue);
            Assert.Equal(1, box.State.Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ReversalCancelsPreviousAnimation_AndNotifiesOnlyCompletedDismissal()
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var directions = new List<SwipeToDismissBoxValue>();
        box.Dismissed += (_, e) => directions.Add(e.Direction);
        var window = DialogTests.Show(box);
        try
        {
            var first = box.State.DismissAsync(SwipeToDismissBoxValue.StartToEnd);
            Advance(clock, window, 7);
            Assert.InRange(box.State.CurrentOffset, 1, 239);
            var reverse = box.State.DismissAsync(SwipeToDismissBoxValue.EndToStart);
            Advance(clock, window, 25);
            Assert.True(first.IsCompletedSuccessfully); Assert.True(reverse.IsCompletedSuccessfully);
            Assert.Equal(-240, box.State.CurrentOffset);
            Assert.Equal([SwipeToDismissBoxValue.EndToStart], directions);
            var reset = box.State.ResetAsync(); Advance(clock, window, 25);
            Assert.True(reset.IsCompletedSuccessfully); Assert.Equal(0, box.State.CurrentOffset);
            Assert.Single(directions);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ResizeDuringAnimation_RestartsTowardUpdatedWidth()
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var window = DialogTests.Show(box);
        try
        {
            var task = box.State.DismissAsync(SwipeToDismissBoxValue.StartToEnd);
            Advance(clock, window, 5);
            box.Width = 320; window.UpdateLayout();
            Advance(clock, window, 25);
            Assert.True(task.IsCompletedSuccessfully); Assert.Equal(320, box.State.CurrentOffset);
            box.EnableDismissFromStartToEnd = false; window.UpdateLayout();
            Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.SettledValue);
            Assert.Equal(0, box.State.CurrentOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DirectionFlagsLimitDragging_ButDoNotVetoProgrammaticState()
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); box.EnableDismissFromStartToEnd = false;
        var window = DialogTests.Show(box);
        try
        {
            box.State.BeginDrag(); box.State.DragBy(500); Assert.Equal(0, box.State.CurrentOffset);
            box.State.DragBy(-500); Assert.Equal(-240, box.State.CurrentOffset);
            box.State.CancelDrag();
            box.State.SnapToAsync(SwipeToDismissBoxValue.StartToEnd).GetAwaiter().GetResult();
            Assert.Equal(SwipeToDismissBoxValue.StartToEnd, box.State.SettledValue);
            Assert.Equal(-240, box.State.CurrentOffset); // Missing anchors leave offset untouched.
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CustomThresholdReceivesDip_AndIsAppliedAtNonzeroVelocity()
    {
        using var clock = new MotionTestClockScope();
        var width = 0d;
        var box = Create(); box.State = new(SwipeToDismissBoxValue.Settled, total => { width = total; return total * .75; });
        var window = DialogTests.Show(box);
        try
        {
            box.State.BeginDrag(); box.State.DragBy(150);
            var task = box.State.EndDrag(50); Advance(clock, window, 25);
            Assert.True(task.IsCompletedSuccessfully); Assert.Equal(240, width);
            Assert.Equal(SwipeToDismissBoxValue.Settled, box.State.SettledValue);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void StateReplacementDetachAndCancellationEndOldOperations()
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var window = DialogTests.Show(box);
        try
        {
            var old = box.State;
            var first = old.DismissAsync(SwipeToDismissBoxValue.StartToEnd); Advance(clock, window, 3);
            box.State = new(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.True(first.IsCompletedSuccessfully); Assert.Equal(0, box.State.CurrentOffset);
            using var cancellation = new CancellationTokenSource();
            var second = box.State.DismissAsync(SwipeToDismissBoxValue.EndToStart, cancellation.Token);
            Advance(clock, window, 3); cancellation.Cancel(); Dispatcher.UIThread.RunJobs();
            Assert.True(second.IsCompletedSuccessfully);
            var third = box.State.ResetAsync(); Advance(clock, window, 2);
            window.Content = null; Dispatcher.UIThread.RunJobs(); Assert.True(third.IsCompletedSuccessfully);
            window.Content = box; window.UpdateLayout();
            var reset = box.State.ResetAsync(); Advance(clock, window, 25);
            Assert.True(reset.IsCompletedSuccessfully); Assert.Equal(0, box.State.CurrentOffset);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealMouseDrag_RespectsGesturesEnabled(bool enabled)
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); box.GesturesEnabled = enabled;
        var window = DialogTests.Show(box);
        try
        {
            var origin = box.TranslatePoint(new Point(20, 20), window)!.Value;
            window.MouseDown(origin, MouseButton.Left);
            window.MouseMove(origin + new Vector(160, 0));
            Assert.Equal(enabled ? 159.875 : 0, box.State.CurrentOffset);
            window.MouseUp(origin + new Vector(160, 0), MouseButton.Left);
            Advance(clock, window, 25);
            Assert.Equal(enabled ? SwipeToDismissBoxValue.StartToEnd : SwipeToDismissBoxValue.Settled, box.State.SettledValue);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PendingDismissalIsCancelledOnStateReplacementAndDetach()
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var count = 0;
        box.Dismissed += (_, _) => count++;
        var window = DialogTests.Show(box);
        try
        {
            box.State.SnapToAsync(SwipeToDismissBoxValue.StartToEnd).GetAwaiter().GetResult();
            box.State = new(); window.UpdateLayout(); Advance(clock, window, 2);
            Assert.Equal(0, count);
            box.State.SnapToAsync(SwipeToDismissBoxValue.EndToStart).GetAwaiter().GetResult();
            window.Content = null; Advance(clock, window, 2);
            Assert.Equal(0, count);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ChildButtonClicksNormally_AndLosesClickWhenParentStartsDragging()
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var clicks = 0;
        var button = new Button { Content = "Open", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        button.Click += (_, _) => clicks++;
        box.Content = button;
        var window = DialogTests.Show(box);
        try
        {
            var origin = button.TranslatePoint(new Point(30, 20), window)!.Value;
            window.MouseDown(origin, MouseButton.Left); window.MouseUp(origin, MouseButton.Left);
            Assert.Equal(1, clicks);
            window.MouseDown(origin, MouseButton.Left); window.MouseMove(origin + new Vector(170, 0));
            window.MouseUp(origin + new Vector(170, 0), MouseButton.Left); Advance(clock, window, 25);
            Assert.Equal(1, clicks);
            Assert.Equal(SwipeToDismissBoxValue.StartToEnd, box.State.SettledValue);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DescendantConsumedMotionDoesNotStartParentDrag()
    {
        using var clock = new MotionTestClockScope();
        var box = Create(); var child = (Control)box.Content!;
        child.PointerMoved += (_, e) => e.Handled = true;
        var window = DialogTests.Show(box);
        try
        {
            var origin = child.TranslatePoint(new Point(20, 20), window)!.Value;
            window.MouseDown(origin, MouseButton.Left); window.MouseMove(origin + new Vector(170, 0));
            window.MouseUp(origin + new Vector(170, 0), MouseButton.Left); Advance(clock, window, 25);
            Assert.Equal(0, box.State.CurrentOffset);
        }
        finally { window.Close(); }
    }

    private static SwipeToDismissBox Create() => new()
    {
        Width = 240, Height = 64,
        Content = new Border { Background = Brushes.Blue },
        BackgroundContent = new Border { Background = Brushes.Red }
    };
    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    {
        for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    }
}
