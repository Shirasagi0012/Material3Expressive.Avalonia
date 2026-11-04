using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class BottomSheetScaffoldTests
{
    [AvaloniaTheory]
    [InlineData(56, 400, SheetValue.PartiallyExpanded, 544)]
    [InlineData(0, 400, SheetValue.PartiallyExpanded, 600)]
    [InlineData(56, 8, SheetValue.Expanded, 544)]
    public void PeekAnchorsAndLayerPlacement(double peek, double contentHeight, SheetValue expected, double offset)
    {
        using var clock = new MotionTestClockScope();
        var body = new Border { Background = Brushes.White }; var bar = new Border { Height = 64 };
        var snack = new Border { Width = 120, Height = 32 };
        var content = new Border { Height = contentHeight };
        var scaffold = new BottomSheetScaffold { Width = 800, Height = 600, SheetPeekHeight = peek, SheetContent = content, Content = body, TopBar = bar, SnackbarHost = snack };
        var window = DialogTests.Show(scaffold);
        try
        {
            var state = scaffold.State.BottomSheetState;
            Assert.Equal(expected, state.CurrentValue); Assert.Equal(offset, state.CurrentOffset);
            Assert.Equal(new Thickness(0, 0, 0, peek), scaffold.ContentPadding);
            Assert.Equal(64, body.TranslatePoint(default, scaffold)!.Value.Y);
            Assert.Equal(expected == SheetValue.PartiallyExpanded ? offset - 32 : 568, snack.TranslatePoint(default, scaffold)!.Value.Y);
            Assert.Equal(0, bar.TranslatePoint(default, scaffold)!.Value.Y);
            Assert.Equal(800, bar.Bounds.Width);
            Assert.Throws<InvalidOperationException>(() => { _ = state.HideAsync(); });
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DefaultSnackbarStateIsConnectedAndHandleCollapsesExpandedSheet()
    {
        using var clock = new MotionTestClockScope();
        var scaffold = new BottomSheetScaffold { Width = 800, Height = 600, SheetContent = new Border { Height = 400 } };
        var window = DialogTests.Show(scaffold);
        try
        {
            Assert.Same(scaffold.State.SnackbarHostState, ((SnackbarHost)scaffold.SnackbarHost!).State);
            var task = scaffold.State.BottomSheetState.ExpandAsync(); Advance(clock, window, 80);
            Assert.True(task.IsCompletedSuccessfully); Assert.Equal(SheetValue.Expanded, scaffold.State.BottomSheetState.CurrentValue);
            var handle = scaffold.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_HandleButton");
            handle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); Advance(clock, window, 80);
            Assert.Equal(SheetValue.PartiallyExpanded, scaffold.State.BottomSheetState.CurrentValue);
            var oldState = scaffold.State; scaffold.State = new(); window.UpdateLayout();
            Assert.Same(scaffold.State.SnackbarHostState, ((SnackbarHost)scaffold.SnackbarHost!).State);
            Assert.NotSame(oldState.BottomSheetState, scaffold.State.BottomSheetState);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void PeekChangeReconcilesDuringAnimationAndOldStateDetaches()
    {
        using var clock = new MotionTestClockScope();
        var scaffold = new BottomSheetScaffold { Width = 800, Height = 600, SheetContent = new Border { Height = 400 } };
        var window = DialogTests.Show(scaffold);
        try
        {
            _ = scaffold.State.BottomSheetState.ExpandAsync(); Advance(clock, window, 80);
            var task = scaffold.State.BottomSheetState.PartialExpandAsync(); Advance(clock, window, 3);
            scaffold.SheetPeekHeight = 100; window.UpdateLayout(); Advance(clock, window, 80);
            Assert.True(task.IsCompletedSuccessfully); Assert.Equal(500, scaffold.State.BottomSheetState.CurrentOffset);
            Assert.Equal(100, scaffold.ContentPadding.Bottom);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ScaffoldOwnsCustomSlotsAndReleasesReplacedHandle()
    {
        using var clock = new MotionTestClockScope();
        var handle = new Border { Width = 48, Height = 20 }; var content = new Border { Height = 300 };
        var scaffold = new BottomSheetScaffold { Width = 800, Height = 600, SheetContent = content, SheetDragHandle = handle, SheetDragHandleTemplate = null };
        var window = DialogTests.Show(scaffold);
        try
        {
            Assert.Same(scaffold, content.GetLogicalParent()); Assert.Same(scaffold, handle.GetLogicalParent());
            var replacement = new Border { Width = 60, Height = 24 }; scaffold.SheetDragHandle = replacement; window.UpdateLayout();
            Assert.Null(handle.GetLogicalParent()); Assert.Same(scaffold, replacement.GetLogicalParent());
            Assert.Equal(60, replacement.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RemovingTheAmbiguousHiddenAnchorPreservesOffsetUntilNextRequest()
    {
        using var clock = new MotionTestClockScope();
        var scaffold = new BottomSheetScaffold { Width = 800, Height = 600, SheetPeekHeight = 0, SheetContent = new Border { Height = 400 } };
        var window = DialogTests.Show(scaffold);
        try
        {
            var state = scaffold.State.BottomSheetState;
            _ = scaffold.OnPostFlingAsync(0); Advance(clock, window, 60);
            Assert.Equal(SheetValue.Hidden, state.CurrentValue);
            Assert.False(state.HasPartiallyExpandedState);
            scaffold.SheetPeekHeight = 56; window.UpdateLayout();
            Assert.Equal(600, state.CurrentOffset); Assert.Equal(SheetValue.Hidden, state.CurrentValue);
            Assert.Equal(SheetValue.Hidden, state.TargetValue);
            _ = state.ShowAsync(); Advance(clock, window, 60);
            Assert.Equal(544, state.CurrentOffset); Assert.Equal(SheetValue.PartiallyExpanded, state.CurrentValue);
        }
        finally { window.Close(); }
    }

    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    { for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); } }
}
