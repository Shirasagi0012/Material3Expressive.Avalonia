using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class ModalBottomSheetTests
{
    [AvaloniaFact]
    public async Task RebuiltOwnerScopeReconnectsPresentationAndScrim()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.PartiallyExpanded); sheet.IsOpen = true;
        var window = DialogTests.Show(sheet, 600);
        try
        {
            Advance(clock, window);
            await AnimationScope.Get(sheet).Diagnostics.EndAsync();
            _ = AnimationScope.Get(sheet); Advance(clock, window);
            Assert.True(sheet.IsPresented); Assert.Equal(1, sheet.ScrimAlpha);
            var hide = sheet.State.HideAsync(); Advance(clock, window);
            Assert.True(hide.IsCompletedSuccessfully); Assert.Equal(0, sheet.ScrimAlpha);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ScrimReversalCancelsOldRunBeforeStartingTheNewSpring()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(); var window = DialogTests.Show(sheet, 600);
        try
        {
            sheet.IsOpen = true; Advance(clock, window, 6);
            Assert.InRange(sheet.ScrimAlpha, .1f, .99f);
            _ = sheet.State.HideAsync();
            Advance(clock, window, 1);
            var atCancellation = sheet.ScrimAlpha;
            Advance(clock, window, 1);
            // AnimateAsState cancels the old job, resetting LastFrameTime and velocity.
            // The first frame of the new run therefore retains its starting value.
            Assert.Equal(atCancellation, sheet.ScrimAlpha);
            Advance(clock, window, 1);
            Assert.True(sheet.ScrimAlpha < atCancellation);
            Advance(clock, window); Assert.Equal(0, sheet.ScrimAlpha);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void OpenShowsAfterLayoutAndProgrammaticHideKeepsDeclarationPresented()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(); var window = DialogTests.Show(sheet, 600);
        try
        {
            Assert.Equal(default, sheet.DesiredSize); Assert.False(sheet.IsPresented);
            sheet.IsOpen = true; Advance(clock, window);
            Assert.True(sheet.IsPresented); Assert.Equal(SheetValue.PartiallyExpanded, sheet.State.CurrentValue);
            Assert.Equal(300, sheet.State.CurrentOffset); Assert.Equal(1, sheet.ScrimAlpha);
            Assert.Equal(0, sheet.PresentedSheet!.TonalElevation);
            var hide = sheet.State.HideAsync(); Advance(clock, window);
            Assert.True(hide.IsCompletedSuccessfully); Assert.True(sheet.IsOpen); Assert.True(sheet.IsPresented);
            Assert.Equal(SheetValue.Hidden, sheet.State.CurrentValue); Assert.Equal(0, sheet.ScrimAlpha);
            sheet.IsOpen = false; Assert.False(sheet.IsPresented);
            sheet.IsOpen = true; Advance(clock, window); Assert.True(sheet.State.IsVisible);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void RestoredExpandedStateIsNotCollapsedAndEscapeHasTwoStages()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.Expanded); sheet.IsOpen = true;
        var window = DialogTests.Show(sheet, 600); var dismissals = 0;
        sheet.DismissRequested += (_, _) => { dismissals++; sheet.IsOpen = false; };
        try
        {
            Advance(clock, window); Assert.Equal(SheetValue.Expanded, sheet.State.CurrentValue);
            Assert.IsType<ModalBottomSheetPresenter>(window.FocusManager!.GetFocusedElement());
            Assert.True(sheet.State.HasPartiallyExpandedState);
            Assert.False(sheet.PresentedSheet!.BackHandlerEnabled);
            Key(window);
            Assert.Equal(SheetValue.PartiallyExpanded, sheet.State.TargetValue);
            Advance(clock, window);
            Assert.Equal(SheetValue.PartiallyExpanded, sheet.State.CurrentValue); Assert.Equal(0, dismissals);
            Key(window); Advance(clock, window);
            Assert.Equal(SheetValue.Hidden, sheet.State.CurrentValue); Assert.Equal(1, dismissals); Assert.False(sheet.IsPresented);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void OutsideClickHonorsPolicyAndVeto(bool allow, bool veto)
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(); sheet.ShouldDismissOnClickOutside = allow;
        sheet.State = new(SheetValue.PartiallyExpanded, confirmValueChange: v => !veto || v != SheetValue.Hidden);
        var window = DialogTests.Show(sheet, 600); var dismissals = 0;
        sheet.DismissRequested += (_, _) => dismissals++;
        try
        {
            sheet.IsOpen = true; Advance(clock, window);
            window.MouseDown(new Point(10, 10), MouseButton.Left); window.MouseUp(new Point(10, 10), MouseButton.Left);
            Advance(clock, window);
            Assert.Equal(allow && !veto ? 1 : 0, dismissals);
            Assert.Equal(allow && !veto ? SheetValue.Hidden : SheetValue.PartiallyExpanded, sheet.State.CurrentValue);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DisabledBackAndVetoedBackDoNotDismissVisibleSheet()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(); sheet.State = new(SheetValue.PartiallyExpanded, confirmValueChange: v => v != SheetValue.Hidden);
        var window = DialogTests.Show(sheet, 600); var dismissals = 0; sheet.DismissRequested += (_, _) => dismissals++;
        try
        {
            sheet.IsOpen = true; Advance(clock, window);
            sheet.ShouldDismissOnBackPress = false; Key(window); Advance(clock, window);
            sheet.ShouldDismissOnBackPress = true; Key(window); Advance(clock, window);
            Assert.Equal(SheetValue.PartiallyExpanded, sheet.State.CurrentValue); Assert.Equal(0, dismissals);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TopInsetsArePreconsumedAndContentRetainsItsDataContext()
    {
        using var clock = new MotionTestClockScope();
        var sheet = Create(SheetValue.Expanded); sheet.DataContext = new object(); sheet.WindowInsets = new Thickness(13, 100, 5, 11);
        var window = DialogTests.Show(sheet, 600);
        try
        {
            sheet.IsOpen = true; Advance(clock, window);
            var surface = sheet.PresentedSheet!.GetVisualDescendants().OfType<MaterialSurface>().First();
            var padding = Assert.IsType<Border>(surface.Child);
            Assert.Equal(new Thickness(13, 0, 5, 11), padding.Padding);
            Assert.Same(sheet.DataContext, ((Control)sheet.Content!).DataContext);
            Assert.True(sheet.PresentedSheet!.GetValue(MaterialWindowInsets.ConsumedPaddingProperty).Top > 100);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ReplacingStateAndRemovingOwnerCancelsWorkAndRestoresFocus()
    {
        using var clock = new MotionTestClockScope();
        var before = new TextBox(); var sheet = Create();
        var root = new Grid { Children = { before, sheet } }; var window = DialogTests.Show(root, 600);
        try
        {
            before.Focus(); sheet.IsOpen = true; Advance(clock, window, 4);
            Assert.NotSame(before, window.FocusManager!.GetFocusedElement());
            var previous = sheet.State; sheet.State = new(SheetValue.Expanded); Advance(clock, window);
            Assert.False(previous.IsAnimationRunning); Assert.Equal(SheetValue.Expanded, sheet.State.CurrentValue);
            var hide = sheet.State.HideAsync(); Advance(clock, window, 2);
            root.Children.Remove(sheet); Dispatcher.UIThread.RunJobs();
            Assert.True(hide.IsCompletedSuccessfully); Assert.False(sheet.IsPresented); Assert.Same(before, window.FocusManager.GetFocusedElement());
            Advance(clock, window); Assert.Equal(0, clock.PendingFrameCount); Assert.Null(window.MotionRoot.Diagnostics.LastError);
            root.Children.Add(sheet); window.UpdateLayout(); Advance(clock, window);
            Assert.True(sheet.IsPresented);
        }
        finally { window.Close(); }
    }

    private static ModalBottomSheet Create(SheetValue value = SheetValue.Hidden) => new()
    { State = new(value), Content = new Border { Height = 400, Background = Brushes.CornflowerBlue } };
    private static void Key(Window window) => window.KeyPress(Avalonia.Input.Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
    private static void Advance(MotionTestClockScope clock, Window window, int frames = 100)
    { Dispatcher.UIThread.RunJobs(); for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); } }
}
