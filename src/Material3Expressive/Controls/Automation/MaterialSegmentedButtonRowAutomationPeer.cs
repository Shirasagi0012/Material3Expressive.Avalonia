// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SegmentedButton.kt:
// SingleChoiceSegmentedButtonRow installs selectableGroup; MultiChoiceSegmentedButtonRow keeps
// independent toggle semantics. This peer exposes the corresponding desktop selection container.

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialSegmentedButtonRowAutomationPeer :
    ControlAutomationPeer,
    ISelectionProvider
{
    public MaterialSegmentedButtonRowAutomationPeer(MaterialSegmentedButtonRow owner)
        : base(owner)
    {
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialSegmentedButtonRow Owner => (MaterialSegmentedButtonRow)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.List;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        return Owner.VisibleItems
            .Select(ControlAutomationPeer.CreatePeerForElement)
            .Where(static peer => peer is not null)
            .Cast<AutomationPeer>()
            .ToArray();
    }

    protected override object? GetProviderCore(Type providerType)
    {
        if (providerType == typeof(ISelectionProvider))
            return Owner.SelectionMode == SegmentedButtonSelectionMode.Single ? this : null;

        return base.GetProviderCore(providerType);
    }

    bool ISelectionProvider.CanSelectMultiple => false;

    bool ISelectionProvider.IsSelectionRequired => false;

    IReadOnlyList<AutomationPeer> ISelectionProvider.GetSelection() =>
        Owner.VisibleItems
            .Where(static item => item.IsChecked == true)
            .Select(ControlAutomationPeer.CreatePeerForElement)
            .Where(static peer => peer is not null)
            .Cast<AutomationPeer>()
            .ToArray();

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialSegmentedButtonRow.SelectionModeProperty)
            RaisePropertyChangedEvent(
                SelectionPatternIdentifiers.CanSelectMultipleProperty,
                e.GetOldValue<SegmentedButtonSelectionMode>() == SegmentedButtonSelectionMode.Multiple,
                e.GetNewValue<SegmentedButtonSelectionMode>() == SegmentedButtonSelectionMode.Multiple);

        if (e.Property == MaterialSegmentedButtonRow.SelectionModeProperty)
            RaisePropertyChangedEvent(
                SelectionPatternIdentifiers.SelectionProperty,
                null,
                null);
    }
}
