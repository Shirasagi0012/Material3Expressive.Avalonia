using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

internal sealed class BottomSheetHandle : Button
{
    protected override AutomationPeer OnCreateAutomationPeer() => new HandlePeer(this);
    private sealed class HandlePeer(BottomSheetHandle owner) : ButtonAutomationPeer(owner), IExpandCollapseProvider
    {
        private BottomSheet? Sheet => owner.FindAncestorOfType<BottomSheet>();
        public ExpandCollapseState ExpandCollapseState => Sheet?.State.CurrentValue == SheetValue.Expanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public bool ShowsMenu => false;
        public void Expand()
        {
            if (Sheet is { GesturesEnabled: true } sheet && sheet.State.Confirm(SheetValue.Expanded)) _ = sheet.State.ExpandAsync();
        }
        public void Collapse()
        {
            if (Sheet is { GesturesEnabled: true } sheet && sheet.State.HasPartiallyExpandedState && sheet.State.Confirm(SheetValue.PartiallyExpanded)) _ = sheet.State.PartialExpandAsync();
        }
    }
}
