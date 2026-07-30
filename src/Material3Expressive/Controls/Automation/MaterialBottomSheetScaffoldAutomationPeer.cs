// Upstream behavior source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/
// BottomSheetScaffold.kt:230-448 and SheetDefaults.kt:90-260.
// Compose exposes expand/collapse semantics on the standard sheet drag handle. Avalonia exposes
// the equivalent desktop ExpandCollapse pattern on the scaffold while retaining the three-state
// Hidden / PartiallyExpanded / Expanded model.

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialBottomSheetScaffoldAutomationPeer :
    ContentControlAutomationPeer,
    IExpandCollapseProvider
{
    private BottomSheetState _observedState;
    private ExpandCollapseState _lastAutomationState;

    public MaterialBottomSheetScaffoldAutomationPeer(MaterialBottomSheetScaffold owner)
        : base(owner)
    {
        _observedState = owner.EffectiveState;
        _lastAutomationState = ToAutomationState(_observedState.CurrentValue);
        _observedState.PropertyChanged += StatePropertyChanged;
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialBottomSheetScaffold Owner =>
        (MaterialBottomSheetScaffold)base.Owner;

    public ExpandCollapseState ExpandCollapseState =>
        _lastAutomationState;

    public bool ShowsMenu => false;

    public void Expand()
    {
        EnsureEnabled();
        _observedState.Expand();
    }

    public void Collapse()
    {
        EnsureEnabled();
        _observedState.PartiallyExpand();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Pane;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != MaterialBottomSheetScaffold.StateProperty)
            return;

        var oldAutomationState = ToAutomationState(_observedState.CurrentValue);
        _observedState.PropertyChanged -= StatePropertyChanged;
        _observedState = Owner.EffectiveState;
        _observedState.PropertyChanged += StatePropertyChanged;
        var newAutomationState = ToAutomationState(_observedState.CurrentValue);
        _lastAutomationState = newAutomationState;
        if (oldAutomationState != newAutomationState)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldAutomationState,
                newAutomationState);
        }
    }

    private void StatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not BottomSheetState state ||
            e.PropertyName != nameof(BottomSheetState.CurrentValue))
            return;

        var newState = ToAutomationState(state.CurrentValue);
        var oldState = _lastAutomationState;
        if (oldState == newState)
            return;

        _lastAutomationState = newState;
        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            oldState,
            newState);
    }

    private static ExpandCollapseState ToAutomationState(BottomSheetValue value) =>
        value switch
        {
            BottomSheetValue.Expanded => ExpandCollapseState.Expanded,
            BottomSheetValue.PartiallyExpanded => ExpandCollapseState.PartiallyExpanded,
            _ => ExpandCollapseState.Collapsed
        };
}
