using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class SheetAutomationTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void NamedActionsTrackStateAndDismissWithoutCycling(bool scaffold)
    {
        using var clock = new MotionTestClockScope();
        var state = new SheetState(SheetValue.PartiallyExpanded);
        var content = new Border { Height = 400 };
        Control root = scaffold ? new BottomSheetScaffold { State = new(state, new()), SheetContent = content }
            : new BottomSheet { State = state, Content = content };
        var window = DialogTests.Show(root);
        void Frames(int count = 60) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        try
        {
            var sheet = root as BottomSheet ?? root.GetVisualDescendants().OfType<BottomSheet>().Single();
            var handle = sheet.GetVisualDescendants().OfType<BottomSheetHandle>().Single();
            var peer = ControlAutomationPeer.CreatePeerForElement(handle);
            AutomationPeer Action(string name) => peer.GetChildren().Single(p => p.GetAutomationId() == "BottomSheet." + name);
            var dismissals = 0; sheet.DismissRequested += (_, _) => dismissals++;
            var dismiss = Action("Hidden"); var expand = Action("Expanded");
            Assert.Same(peer, dismiss.GetParent()); Assert.False(dismiss.IsKeyboardFocusable());
            Assert.DoesNotContain(peer.GetChildren(), p => p.GetAutomationId() == "BottomSheet.PartiallyExpanded");
            ((IInvokeProvider)expand).Invoke(); Frames(); Assert.Equal(SheetValue.Expanded, state.CurrentValue);
            Assert.DoesNotContain(peer.GetChildren(), p => p.GetAutomationId() == "BottomSheet.Expanded");
            ((IInvokeProvider)expand).Invoke(); Assert.Equal(SheetValue.Expanded, state.TargetValue);
            ((IInvokeProvider)Action("PartiallyExpanded")).Invoke(); Frames(); Assert.Equal(SheetValue.PartiallyExpanded, state.CurrentValue);
            sheet.GesturesEnabled = false; Assert.Empty(peer.GetChildren()); Assert.False(dismiss.IsEnabled());
            ((IInvokeProvider)dismiss).Invoke(); Frames(); Assert.Equal(SheetValue.PartiallyExpanded, state.CurrentValue);
            sheet.GesturesEnabled = true; ((IInvokeProvider)Action("Hidden")).Invoke(); Frames();
            Assert.Equal(SheetValue.Hidden, state.CurrentValue); Assert.Equal(scaffold ? 0 : 1, dismissals);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void StandardSheetWithoutHiddenStateOmitsDismissAndVetoKeepsState()
    {
        using var clock = new MotionTestClockScope();
        var state = new SheetState(SheetValue.PartiallyExpanded, [SheetValue.PartiallyExpanded, SheetValue.Expanded], _ => false);
        var scaffold = new BottomSheetScaffold { State = new(state, new()), SheetContent = new Border { Height = 400 } };
        var window = DialogTests.Show(scaffold);
        try
        {
            var handle = scaffold.GetVisualDescendants().OfType<BottomSheetHandle>().Single();
            var peer = ControlAutomationPeer.CreatePeerForElement(handle);
            var action = Assert.Single(peer.GetChildren()); Assert.Equal("BottomSheet.Expanded", action.GetAutomationId());
            ((IInvokeProvider)action).Invoke(); Assert.Equal(SheetValue.PartiallyExpanded, state.CurrentValue);
            Assert.False(state.IsAnimationRunning);
            scaffold.SheetDragHandle = null; Assert.Empty(peer.GetChildren());
        }
        finally { window.Close(); }
    }
}
