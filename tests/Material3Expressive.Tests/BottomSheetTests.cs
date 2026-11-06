using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Input.Raw;
using Material3Expressive.Foundation.Rendering;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class BottomSheetTests
{
    [AvaloniaFact]
    public void StateValidatesEnabledValuesAndUnmeasuredRequests()
    {
        Assert.Throws<ArgumentException>(() => new SheetState(SheetValue.Hidden, [SheetValue.Hidden]));
        Assert.Throws<ArgumentException>(() => new SheetState(SheetValue.Hidden, [SheetValue.Expanded]));
        var state = new SheetState(SheetValue.Expanded, [SheetValue.Expanded]);
        Assert.Throws<InvalidOperationException>(() => { _ = state.HideAsync(); });
        Assert.Throws<InvalidOperationException>(() => { _ = state.PartialExpandAsync(); });
        Assert.Throws<InvalidOperationException>(() => state.RequireOffset());
        Assert.NotSame(new BottomSheet().State, new BottomSheet().State);
        var hidden = new SheetState(); hidden.ShowAsync().GetAwaiter().GetResult();
        Assert.Equal(SheetValue.Expanded, hidden.CurrentValue); Assert.True(hidden.IsVisible);
    }

    [AvaloniaTheory]
    [InlineData(SheetValue.Hidden, 400, 600)]
    [InlineData(SheetValue.PartiallyExpanded, 400, 300)]
    [InlineData(SheetValue.Expanded, 400, 152)]
    [InlineData(SheetValue.PartiallyExpanded, 100, 452)]
    [InlineData(SheetValue.Expanded, 100, 452)]
    public void AnchorsIncludeHandleAndMergeForShortContent(SheetValue initial, double height, double offset)
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(initial, height); var window = DialogTests.Show(sheet);
        try
        {
            Assert.Equal(offset, sheet.State.RequireOffset());
            Assert.True(sheet.State.HasExpandedState); Assert.True(sheet.State.HasPartiallyExpandedState);
            var surface = sheet.GetVisualDescendants().OfType<MaterialSurface>().First();
            Assert.Equal(height + 48, surface.Bounds.Height);
            Assert.Equal(640, surface.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ConvergedPartialPromotesWhenContentGrows()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.PartiallyExpanded, 100); var window = DialogTests.Show(sheet);
        try
        {
            ((Control)sheet.Content!).Height = 400; window.UpdateLayout();
            Assert.Equal(SheetValue.Expanded, sheet.State.CurrentValue); Assert.Equal(152, sheet.State.CurrentOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ShowHideReversalRetainsPositionAndCancelsOldMutation()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(); var window = DialogTests.Show(sheet);
        try
        {
            var show = sheet.State.ShowAsync(); Advance(clock, window, 4);
            var offset = sheet.State.CurrentOffset; Assert.InRange(offset, 300, 599);
            Assert.Equal(SheetValue.Hidden, sheet.State.CurrentValue);
            var hide = sheet.State.HideAsync(); Assert.Equal(offset, sheet.State.CurrentOffset);
            Advance(clock, window, 100);
            Assert.True(show.IsCompletedSuccessfully); Assert.True(hide.IsCompletedSuccessfully);
            Assert.Equal(600, sheet.State.CurrentOffset); Assert.False(sheet.State.IsVisible);
            Assert.False(sheet.State.IsAnimationRunning); Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void VetoPreventsProgrammaticChangeAndSettlesDraggedOffsetBack()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.PartiallyExpanded);
        sheet.State = new SheetState(SheetValue.PartiallyExpanded, confirmValueChange: value => value != SheetValue.Hidden);
        var window = DialogTests.Show(sheet);
        try
        {
            Assert.True(sheet.State.HideAsync().IsCompletedSuccessfully); Assert.Equal(300, sheet.State.CurrentOffset);
            sheet.State.BeginDrag(); sheet.State.DragBy(240); var settle = sheet.State.EndDrag(0); Advance(clock, window, 100);
            Assert.True(settle.IsCompletedSuccessfully); Assert.Equal(300, sheet.State.CurrentOffset);
            Assert.Equal(SheetValue.PartiallyExpanded, sheet.State.CurrentValue);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NestedScrollConsumesOnlyUserDeltasAndRespectsAnchorBounds()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.PartiallyExpanded); var window = DialogTests.Show(sheet);
        try
        {
            Assert.Equal(0, sheet.OnPreScroll(20)); Assert.Equal(0, sheet.OnPreScroll(-20, false));
            Assert.Equal(-148, sheet.OnPreScroll(-500)); Assert.Equal(152, sheet.State.CurrentOffset);
            Assert.Equal(448, sheet.OnPostScroll(800)); Assert.Equal(600, sheet.State.CurrentOffset);
            sheet.GesturesEnabled = false; Assert.Equal(0, sheet.OnPostScroll(-20));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RemovingSheetCancelsPendingAnimationAndReleasesStateForRemount()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(); var window = DialogTests.Show(sheet);
        try
        {
            var task = sheet.State.ExpandAsync(); Advance(clock, window, 3);
            window.Content = null; Dispatcher.UIThread.RunJobs(); Assert.True(task.IsCompletedSuccessfully);
            Assert.Equal(0, clock.PendingFrameCount);
            window.Content = sheet; window.UpdateLayout();
            var next = sheet.State.ShowAsync(); Advance(clock, window, 100);
            Assert.True(next.IsCompletedSuccessfully); Assert.Equal(300, sheet.State.CurrentOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MouseDragDismissesOnceAndDisabledGesturesKeepTheHandleClickable()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.PartiallyExpanded); var window = DialogTests.Show(sheet); var dismissals = 0;
        sheet.DismissRequested += (_, _) => dismissals++;
        try
        {
            var origin = sheet.TranslatePoint(new Point(300, 370), window)!.Value;
            window.MouseDown(origin, MouseButton.Left); window.MouseMove(origin + new Vector(0, 220));
            Assert.InRange(sheet.State.CurrentOffset, 519, 521);
            window.MouseUp(origin + new Vector(0, 220), MouseButton.Left); Advance(clock, window, 100);
            Assert.Equal(SheetValue.Hidden, sheet.State.CurrentValue); Assert.Equal(1, dismissals);
            _ = sheet.State.ShowAsync(); Advance(clock, window, 100); sheet.GesturesEnabled = false;
            var handle = sheet.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_HandleButton");
            handle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); Advance(clock, window, 100);
            Assert.Equal(SheetValue.Expanded, sheet.State.CurrentValue);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void EscapeCollapsesThenDismissesAndBackSwitchDisablesIt()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.Expanded); var window = DialogTests.Show(sheet); var count = 0;
        sheet.DismissRequested += (_, _) => count++;
        try
        {
            sheet.BackHandlerEnabled = false;
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Advance(clock, window, 50);
            Assert.Equal(SheetValue.Expanded, sheet.State.CurrentValue);
            sheet.BackHandlerEnabled = true;
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Advance(clock, window, 50);
            Assert.Equal(SheetValue.PartiallyExpanded, sheet.State.CurrentValue); Assert.Equal(0, count);
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Advance(clock, window, 50);
            Assert.Equal(SheetValue.Hidden, sheet.State.CurrentValue); Assert.Equal(1, count);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DismissCallbackMayRemoveSheetWithoutLeavingWork()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.Expanded); var window = DialogTests.Show(sheet); var count = 0;
        sheet.DismissRequested += (_, _) => { count++; window.Content = null; };
        try
        {
            sheet.Dismiss(); Advance(clock, window, 50);
            Assert.Equal(1, count); Assert.Null(window.Content); Assert.Equal(0, clock.PendingFrameCount);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExplicitCancellationPreservesOffsetAndReleasesAnimationTarget()
    {
        using var clock = new MotionTestClockScope(); using var cancellation = new CancellationTokenSource();
        var sheet = Create(); var window = DialogTests.Show(sheet);
        try
        {
            var task = sheet.State.ExpandAsync(cancellation.Token); Advance(clock, window, 4);
            var offset = sheet.State.CurrentOffset; cancellation.Cancel(); Dispatcher.UIThread.RunJobs();
            Assert.True(task.IsCompletedSuccessfully); Assert.Equal(offset, sheet.State.CurrentOffset); Assert.False(sheet.State.IsAnimationRunning);
            Advance(clock, window, 4); Assert.Equal(offset, sheet.State.CurrentOffset);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void InsetsExcludeAncestorConsumptionAndPropagateRemainingPadding(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.Expanded); sheet.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        sheet.WindowInsets = new Thickness(13, 7, 5, 11);
        var parent = new Border { Child = sheet, FlowDirection = sheet.FlowDirection };
        MaterialWindowInsets.SetConsumePadding(parent, new Thickness(8, 3, 2, 6));
        var window = DialogTests.Show(parent);
        try
        {
            var surface = sheet.GetVisualDescendants().OfType<MaterialSurface>().First();
            var padding = (Border)surface.Child!;
            Assert.Equal(rtl ? new Thickness(3, 4, 5, 5) : new Thickness(5, 4, 3, 5), padding.Padding);
            var content = (Control)sheet.Content!;
            Assert.Equal(rtl ? new Thickness(5, 7, 13, 11) : new Thickness(13, 7, 5, 11), content.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void HiddenAnchorIsPresentEvenWhenProgrammaticHideIsDisabled()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.Expanded); sheet.State = new(SheetValue.Expanded, [SheetValue.Expanded]);
        var window = DialogTests.Show(sheet);
        try
        {
            Assert.False(sheet.State.HasPartiallyExpandedState);
            // BottomSheetImpl always registers Hidden, unlike the standard scaffold variant.
            sheet.State.BeginDrag(); sheet.State.DragBy(1000);
            Assert.Equal(600, sheet.State.CurrentOffset);
            var settle = sheet.State.EndDrag(0); Advance(clock, window, 50);
            Assert.True(settle.IsCompletedSuccessfully); Assert.Equal(SheetValue.Hidden, sheet.State.CurrentValue);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ZeroHeightSheetReconcilesExpandedToHidden()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.Expanded, 0); sheet.DragHandle = null;
        var window = DialogTests.Show(sheet);
        try
        {
            Assert.False(sheet.State.HasExpandedState); Assert.Equal(SheetValue.Hidden, sheet.State.CurrentValue);
            Assert.Equal(600, sheet.State.CurrentOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("zh-CN", "拖动手柄")]
    [InlineData("zh-TW", "拖曳控點")]
    [InlineData("id-ID", "Handel geser")]
    [InlineData("en-AU", "Drag handle")]
    public void SemanticsUseFixedUpstreamLocaleMapping(string locale, string expected)
    {
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(locale);
            Assert.Equal(expected, BottomSheetStrings.Get("BottomSheetDragHandleDescription"));
        }
        finally { System.Globalization.CultureInfo.CurrentUICulture = previous; }
    }

    private static BottomSheet Create(SheetValue initial = SheetValue.Hidden, double height = 400) => new()
    { Width = 800, Height = 600, State = new(initial), Content = new Border { Height = height, Background = Brushes.CornflowerBlue } };
    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    { for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); } }
}
