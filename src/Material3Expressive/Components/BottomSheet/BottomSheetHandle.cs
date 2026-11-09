using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

internal sealed class BottomSheetHandle : Button
{
    protected override AutomationPeer OnCreateAutomationPeer() => new HandlePeer(this);
    internal void RefreshAutomation() => (ControlAutomationPeer.FromElement(this) as HandlePeer)?.Refresh();

    private sealed class HandlePeer : ButtonAutomationPeer, IExpandCollapseProvider
    {
        private readonly BottomSheetHandle _handle;
        private readonly ActionPeer[] _actions;
        internal BottomSheet? Sheet => _handle.FindAncestorOfType<BottomSheet>();
        public HandlePeer(BottomSheetHandle owner) : base(owner)
        {
            _handle = owner;
            _actions = [new(this, owner, SheetValue.Hidden), new(this, owner, SheetValue.Expanded), new(this, owner, SheetValue.PartiallyExpanded)];
        }
        internal bool Available(SheetValue action)
        {
            if (Sheet is not { GesturesEnabled: true, IsEffectivelyEnabled: true, DragHandle: not null } sheet) return false;
            var state = sheet.State;
            if (sheet.StandardPeekHeight is not null && state.AnchorCount <= 1) return false;
            return action switch
            {
                SheetValue.Hidden => sheet.StandardPeekHeight is null || state.HiddenEnabled,
                SheetValue.Expanded => state.CurrentValue == SheetValue.PartiallyExpanded,
                _ => state.CurrentValue != SheetValue.PartiallyExpanded && (sheet.StandardPeekHeight is not null || state.HasPartiallyExpandedState)
            };
        }
        internal void Execute(SheetValue action)
        {
            if (!Available(action) || Sheet is not { } sheet) return;
            var state = sheet.State;
            // Scaffold's collapse/dismiss semantics call confirm, then launch the state operation
            // regardless of that first result. The state operation performs its own confirmation.
            if (action == SheetValue.Hidden)
            {
                if (sheet.StandardPeekHeight is null) sheet.Dismiss();
                else { _ = state.Confirm(action); _ = state.HideAsync(); }
            }
            else
            {
                var confirmed = state.Confirm(action);
                if (action == SheetValue.Expanded) { if (confirmed) _ = state.ExpandAsync(); }
                else if (confirmed || sheet.StandardPeekHeight is not null) _ = state.PartialExpandAsync();
            }
        }
        internal void Refresh()
        {
            InvalidateChildren();
            RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, null, ExpandCollapseState);
        }
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => _actions.Where(action => Available(action.Action)).Cast<AutomationPeer>().ToArray();
        public ExpandCollapseState ExpandCollapseState => Sheet?.State.CurrentValue == SheetValue.Expanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public bool ShowsMenu => false;
        public void Expand() => Execute(SheetValue.Expanded);
        public void Collapse() => Execute(SheetValue.PartiallyExpanded);
    }

    // The backend has Invoke and ExpandCollapse, but no named multi-action provider. These peers
    // expose the actual available actions without adding controls to layout or the tab sequence.
    private sealed class ActionPeer(HandlePeer parent, BottomSheetHandle owner, SheetValue action) : ControlAutomationPeer(owner), IInvokeProvider
    {
        internal SheetValue Action => action;
        public void Invoke() => parent.Execute(action);
        protected override AutomationPeer? GetParentCore() => parent;
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => [];
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
        protected override string GetClassNameCore() => "BottomSheetAction";
        protected override string? GetAutomationIdCore() => "BottomSheet." + action;
        protected override string? GetNameCore() => BottomSheetStrings.Get(action switch
        {
            SheetValue.Hidden => "BottomSheetDismissDescription",
            SheetValue.Expanded => "BottomSheetExpandDescription",
            _ => "BottomSheetPartialExpandDescription"
        });
        protected override bool IsEnabledCore() => parent.Available(action);
        protected override bool IsKeyboardFocusableCore() => false;
        protected override bool HasKeyboardFocusCore() => false;
        protected override void SetFocusCore() { }
    }
}
